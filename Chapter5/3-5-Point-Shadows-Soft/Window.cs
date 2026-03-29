using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace LearnOpenTK
{
    public class Window : GameWindow
    {
        private bool shadows = true;
        private bool shadowsPressed = true;
        
        private int depthMapFbo;
        private int depthCubeMap;
        
        private float nearPlane = 1f;
        private float farPlane = 25f;
        
        private Vector3 LightPos = new(0f);
        private Texture floorTexture;
        
        private float[] planeVertices = {
            // positions            // normals         // texcoords
            25.0f, -0.5f,  25.0f,  0.0f, 1.0f, 0.0f,  1.0f,  0.0f,
            -25.0f, -0.5f,  25.0f,  0.0f, 1.0f, 0.0f,   0.0f,  0.0f,
            -25.0f, -0.5f, -25.0f,  0.0f, 1.0f, 0.0f,   0.0f, 1.0f,

            25.0f, -0.5f,  25.0f,  0.0f, 1.0f, 0.0f,  1.0f,  0.0f,
            -25.0f, -0.5f, -25.0f,  0.0f, 1.0f, 0.0f,   0.0f, 1.0f,
            25.0f, -0.5f, -25.0f,  0.0f, 1.0f, 0.0f, 1.0f, 1.0f
        };
        

        private int planeVao;
        private int planeVbo;
        
        private Shader _shader;
        private Shader _depthShader;
        
        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;

        private const int ShadowWidth = 1024;
        private const int ShadowHeight = 1024;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            
            // plane VAO
            planeVao = GL.GenVertexArray();
            planeVbo = GL.GenBuffer();
            GL.BindVertexArray(planeVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, planeVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * planeVertices.Length, planeVertices, BufferUsageHint.StaticDraw);
            
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
            
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            GL.BindVertexArray(0);

            // load texture
            floorTexture = Texture.LoadFromFile("Resources/wood.png", TextureWrapMode.ClampToEdge);
            
            // configure depth map FBO
            depthMapFbo = GL.GenFramebuffer();
            depthCubeMap = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, depthCubeMap);
            for (int i = 0; i < 6; i++)
            {
                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.DepthComponent, ShadowWidth, ShadowHeight, 0, PixelFormat.DepthComponent, PixelType.Float, 0);
            }
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            // attach depth texture fbo to fbo depth buffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFbo);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthCubeMap, 0 );
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // shader configuration
            _shader = new Shader("Shaders/point_shadows.vert", "Shaders/point_shadows.frag");
            _depthShader = new Shader("Shaders/point_shadows_depth.vert", "Shaders/point_shadows_depth.frag", "Shaders/point_shadows_depth.geo");
            
            _shader.Use();
            _shader.SetInt("diffuseTexture", 0);
            _shader.SetInt("depthMap", 1);
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
        }   

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            GL.ClearColor(0.1f,0.1f,0.1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // move light position over time
            LightPos.Z = (float)(MathHelper.Sin((float)GLFW.GetTime() * 0.5) * 3.0);
            
            // 0. create depth cubemap transformation matrices
            var shadowProj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(90.0f),
                1.0f,
                nearPlane,
                farPlane
            );
            var shadowTransforms = new List<Matrix4>();
            
            shadowTransforms.Add(Matrix4.LookAt(LightPos, LightPos + new Vector3(1f, 0f, 0f), new Vector3(0f, -1f, 0f)) * shadowProj);
            shadowTransforms.Add(Matrix4.LookAt(LightPos, LightPos + new Vector3(-1f, 0f, 0f), new Vector3(0f, -1f, 0f)) * shadowProj);
            shadowTransforms.Add(Matrix4.LookAt(LightPos, LightPos + new Vector3(0, 1f, 0f), new Vector3(0f, 0f, 1f)) * shadowProj);
            shadowTransforms.Add(Matrix4.LookAt(LightPos, LightPos + new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, -1f)) * shadowProj);
            shadowTransforms.Add(Matrix4.LookAt(LightPos, LightPos + new Vector3(0f, 0f, 1f), new Vector3(0f, -1f, 0f)) * shadowProj);
            shadowTransforms.Add(Matrix4.LookAt(LightPos, LightPos + new Vector3(0f, 0f, -1f), new Vector3(0f, -1f, 0f)) * shadowProj);
            
            // 1. Render Depth of scene to texture (from light perspective)
            GL.Viewport(0, 0, ShadowWidth, ShadowHeight);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFbo);
            
            GL.Clear(ClearBufferMask.DepthBufferBit);
            _depthShader.Use();
            for (int i = 0; i < 6; ++i)
            {
                _depthShader.SetMatrix4($"shadowMatrices[{i}]", shadowTransforms[i]);
            }
            _depthShader.SetFloat("far_plane", farPlane);
            _depthShader.SetVector3("lightPos", LightPos);
            RenderScene(_depthShader, false);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // 2. Render Scene as normal
            GL.Viewport(0, 0, 800, 600);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            _shader.Use();
            _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _shader.SetMatrix4("view", _camera.GetViewMatrix());
            
            _shader.SetVector3("viewPos", _camera.Position);
            _shader.SetVector3("lightPos", LightPos);
            _shader.SetBool("shadows", shadows); // Switch by B button
            _shader.SetFloat("far_plane", farPlane);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, floorTexture.Handle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.TextureCubeMap, depthCubeMap);
            RenderScene(_shader, true);
            
            SwapBuffers();
        }

        private void RenderScene(Shader shader, bool hasReverseNormals) // hasReverseNormals param added to skip setting uniform that does not exist, preventing exception without changes in Shader class
        {
            // Room Cube
            var model = Matrix4.Identity;
            model = Matrix4.CreateScale(new Vector3(5f)) * model;
            shader.SetMatrix4("model", model);
            GL.Disable(EnableCap.CullFace); // We want to render the inside of a cube
            if (hasReverseNormals)
            {
                shader.SetInt("reverse_normals", 1); // Invert normals so that lighting inside works as expected
            }
            RenderCube();
            if (hasReverseNormals)
            {
                shader.SetInt("reverse_normals", 0);
            }
            GL.Enable(EnableCap.CullFace);

            // Cubes
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(4f, -3.5f, 1f)) * model;
            model = Matrix4.CreateScale(new Vector3(0.75f)) * model;
            shader.SetMatrix4("model", model);
            RenderCube();
            
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(-3f, -1f, 0)) * model;
            model = Matrix4.CreateScale(new Vector3(0.5f)) * model;
            shader.SetMatrix4("model", model);
            RenderCube();
            
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(-1.5f, 1f, 1.5f)) * model;
            model = Matrix4.CreateScale(new Vector3(0.5f)) * model;
            shader.SetMatrix4("model", model);
            RenderCube();
            
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(-1.5f, 2f, -3f)) * model;
            model = Matrix4.CreateFromAxisAngle(
                Vector3.Normalize(new Vector3(1.0f, 0.0f, 1.0f)),
                MathHelper.DegreesToRadians(60.0f)
            ) * model;
            model = Matrix4.CreateScale(new Vector3(0.75f)) * model;
            shader.SetMatrix4("model", model);
            RenderCube();
        }

        private int cubeVao = 0;
        private int cubeVbo = 0;
        
        private void RenderCube()
        {
            if (cubeVao == 0)
            {
                float[] vertices =
                {
                    // back face
                    -1.0f, -1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f, // bottom-left
                    1.0f, 1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f, // top-right
                    1.0f, -1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, // bottom-right         
                    1.0f, 1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f, // top-right
                    -1.0f, -1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f, // bottom-left
                    -1.0f, 1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f, // top-left
                    // front face
                    -1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, // bottom-left
                    1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f, // bottom-right
                    1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f, // top-right
                    1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f, // top-right
                    -1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, // top-left
                    -1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, // bottom-left
                    // left face
                    -1.0f, 1.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f, // top-right
                    -1.0f, 1.0f, -1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f, // top-left
                    -1.0f, -1.0f, -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, // bottom-left
                    -1.0f, -1.0f, -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, // bottom-left
                    -1.0f, -1.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, // bottom-right
                    -1.0f, 1.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f, // top-right
                    // right face
                    1.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, // top-left
                    1.0f, -1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, // bottom-right
                    1.0f, 1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, // top-right         
                    1.0f, -1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, // bottom-right
                    1.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, // top-left
                    1.0f, -1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, // bottom-left     
                    // bottom face
                    -1.0f, -1.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, // top-right
                    1.0f, -1.0f, -1.0f, 0.0f, -1.0f, 0.0f, 1.0f, 1.0f, // top-left
                    1.0f, -1.0f, 1.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f, // bottom-left
                    1.0f, -1.0f, 1.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f, // bottom-left
                    -1.0f, -1.0f, 1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, // bottom-right
                    -1.0f, -1.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, // top-right
                    // top face
                    -1.0f, 1.0f, -1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, // top-left
                    1.0f, 1.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, // bottom-right
                    1.0f, 1.0f, -1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, // top-right     
                    1.0f, 1.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, // bottom-right
                    -1.0f, 1.0f, -1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, // top-left
                    -1.0f, 1.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f // bottom-left        
                };
                
                cubeVao = GL.GenVertexArray();
                cubeVbo = GL.GenBuffer();
                
                // fill buffer
                GL.BindBuffer(BufferTarget.ArrayBuffer, cubeVbo);
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertices.Length, vertices, BufferUsageHint.StaticDraw);
                
                // link vertex attributes
                GL.BindVertexArray(cubeVao);
                
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
            
                GL.EnableVertexAttribArray(2);
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
                
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);
            }
            
            // Render the cube
            GL.BindVertexArray(cubeVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            GL.BindVertexArray(0);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (!IsFocused)
            {
                return;
            }

            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            const float cameraSpeed = 1.5f;
            const float sensitivity = 0.2f;
            
            if (input.IsKeyDown(Keys.B) && !shadowsPressed) 
            {
                shadows = !shadows;
                shadowsPressed = true;
            }
            if (input.IsKeyReleased(Keys.B)) 
            {
                shadowsPressed = false;
            }

            if (input.IsKeyDown(Keys.W))
            {
                _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; // Forward
            }
            if (input.IsKeyDown(Keys.S))
            {
                _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Backwards
            }
            if (input.IsKeyDown(Keys.A))
            {
                _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Left
            }
            if (input.IsKeyDown(Keys.D))
            {
                _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Right
            }
            if (input.IsKeyDown(Keys.Space))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Up
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Down
            }

            var mouse = MouseState;

            if (_firstMove)
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastPos.X;
                var deltaY = mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(mouse.X, mouse.Y);

                _camera.Yaw += deltaX * sensitivity;
                _camera.Pitch -= deltaY * sensitivity;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _camera.Fov -= e.OffsetY;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
        }
    }
}
