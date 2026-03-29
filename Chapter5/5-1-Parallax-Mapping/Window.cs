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
        private float heightScale = 0.1f;

        private float nearPlane = 1f;
        private float farPlane = 25f;
        
        private Vector3 LightPos = new(0.5f, 1f, 0.3f);
        
        private Texture diffuseMap;
        private Texture normalMap;
        private Texture depthMap;
        
        private Shader _shader;
        
        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;


        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);

            // load texture
            diffuseMap = Texture.LoadFromFile("Resources/bricks2.jpg");
            normalMap = Texture.LoadFromFile("Resources/bricks2_normal.jpg");
            depthMap = Texture.LoadFromFile("Resources/bricks2_disp.jpg");
            
            // shader configuration
            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            
            _shader.Use();
            _shader.SetInt("diffuseMap", 0);
            _shader.SetInt("normalMap", 1);
            _shader.SetInt("depthMap", 2);
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
        }   

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            GL.ClearColor(0.1f,0.1f,0.1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            _shader.Use();
            _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _shader.SetMatrix4("view", _camera.GetViewMatrix());
            
            // render parallax-mapped quad
            var model = Matrix4.Identity;
            model = Matrix4.CreateFromAxisAngle(
                Vector3.Normalize(new Vector3(1.0f, 0.0f, 1.0f)),
                MathHelper.DegreesToRadians((float)GLFW.GetTime() * -10.0f)
            ) * model;
            _shader.SetMatrix4("model", model);
            _shader.SetVector3("viewPos", _camera.Position);
            _shader.SetVector3("lightPos", LightPos);
            _shader.SetFloat("heightScale", heightScale); //adjust with Q and E 
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, diffuseMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, normalMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, depthMap.Handle);
            RenderQuad();
            
            // Render light source (simply re-renders a smaller plane at the light's position for debugging/visualization)
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(LightPos) * model;
            model = Matrix4.CreateScale(new Vector3(0.1f)) * model;
            _shader.SetMatrix4("model", model);
            RenderQuad();
            
            SwapBuffers();
        }

        private int quadVao = 0;
        private int quadVbo = 0;
        private void RenderQuad()
        {
            if (quadVao == 0)
            {
                // positions
                var pos1 = new Vector3(-1f, 1f, 0f);
                var pos2 = new Vector3(-1f, -1f, 0f);
                var pos3 = new Vector3(1f, -1f, 0f);
                var pos4 = new Vector3(1f, 1f, 0f);
                
                // texture coordinates
                var uv1 = new Vector2(0f, 1f);
                var uv2 = new Vector2(0f, 0f);
                var uv3 = new Vector2(1f, 0f);
                var uv4 = new Vector2(1f, 1f);
                
                // normal vector
                var nm = new Vector3(0, 0, 1f);
                
                // calculate tangent/bitangent vectors of both triangles
                var tangent1 = Vector3.Zero;
                var bitangent1 = Vector3.Zero;
                
                var tangent2 = Vector3.Zero;
                var bitangent2 = Vector3.Zero;
                
                // triangle1
                var edge1 = pos2 - pos1;
                var edge2 = pos3 - pos1;
                var deltaUV1 = uv2 - uv1;
                var deltaUV2 = uv3 - uv1;

                var f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);
                
                tangent1.X = f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X);
                tangent1.Y = f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y);
                tangent1.Z = f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z);

                bitangent1.X = f * (-deltaUV2.X * edge1.X + deltaUV1.X * edge2.X);
                bitangent1.Y = f * (-deltaUV2.X * edge1.Y + deltaUV1.X * edge2.Y);
                bitangent1.Z = f * (-deltaUV2.X * edge1.Z + deltaUV1.X * edge2.Z);
                // triangle 2
                edge1 = pos3 - pos1;
                edge2 = pos4 - pos1;
                deltaUV1 = uv3 - uv1;
                deltaUV2 = uv4 - uv1;

                f = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);

                tangent2.X = f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X);
                tangent2.Y = f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y);
                tangent2.Z = f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z);


                bitangent2.X = f * (-deltaUV2.X * edge1.X + deltaUV1.X * edge2.X);
                bitangent2.Y = f * (-deltaUV2.X * edge1.Y + deltaUV1.X * edge2.Y);
                bitangent2.Z = f * (-deltaUV2.X * edge1.Z + deltaUV1.X * edge2.Z);
                
                float[] quadVertices = {
                    // positions            // normal         // texcoords  // tangent                          // bitangent
                    pos1.X, pos1.Y, pos1.Z, nm.X, nm.Y, nm.Z, uv1.X, uv1.Y, tangent1.X, tangent1.Y, tangent1.Z, bitangent1.X, bitangent1.Y, bitangent1.Z,
                    pos2.X, pos2.Y, pos2.Z, nm.X, nm.Y, nm.Z, uv2.X, uv2.Y, tangent1.X, tangent1.Y, tangent1.Z, bitangent1.X, bitangent1.Y, bitangent1.Z,
                    pos3.X, pos3.Y, pos3.Z, nm.X, nm.Y, nm.Z, uv3.X, uv3.Y, tangent1.X, tangent1.Y, tangent1.Z, bitangent1.X, bitangent1.Y, bitangent1.Z,

                    pos1.X, pos1.Y, pos1.Z, nm.X, nm.Y, nm.Z, uv1.X, uv1.Y, tangent2.X, tangent2.Y, tangent2.Z, bitangent2.X, bitangent2.Y, bitangent2.Z,
                    pos3.X, pos3.Y, pos3.Z, nm.X, nm.Y, nm.Z, uv3.X, uv3.Y, tangent2.X, tangent2.Y, tangent2.Z, bitangent2.X, bitangent2.Y, bitangent2.Z,
                    pos4.X, pos4.Y, pos4.Z, nm.X, nm.Y, nm.Z, uv4.X, uv4.Y, tangent2.X, tangent2.Y, tangent2.Z, bitangent2.X, bitangent2.Y, bitangent2.Z
                };
                
                quadVao = GL.GenVertexArray();
                quadVbo = GL.GenBuffer();
                GL.BindVertexArray(quadVao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo);
                GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);
                
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 0);
                
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 3 * sizeof(float));
                
                GL.EnableVertexAttribArray(2);
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 14 * sizeof(float), 6 * sizeof(float));
                
                GL.EnableVertexAttribArray(3);
                GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 8 * sizeof(float));
                
                GL.EnableVertexAttribArray(4);
                GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, 14 * sizeof(float), 11 * sizeof(float));
            }
            GL.BindVertexArray(quadVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
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
            
            if (input.IsKeyDown(Keys.Q)) 
            {
                if (heightScale > 0.0f) 
                    heightScale -= 0.0005f;
                else 
                    heightScale = 0.0f;
            }
            
            if (input.IsKeyDown(Keys.E)) 
            {
                if (heightScale < 1.0f) 
                    heightScale += 0.0005f;
                else 
                    heightScale = 1.0f;
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
