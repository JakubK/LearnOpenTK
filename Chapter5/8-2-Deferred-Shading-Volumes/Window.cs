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
        private int rboDepth;

        private List<Vector3> ObjectPositions = new();
        private List<Vector3> LightPositions = new();
        private List<Vector3> LightColors = new();
        
        
        private Shader shaderGeometryPass;
        private Shader shaderLightingPass;
        private Shader shaderLightBox;

        private int gBuffer;
        private int gPosition;
        private int gNormal;
        private int gAlbedoSpec;
        

        private Model backpack;
        
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

            // load model
            backpack = new Model("Resources/backpack.obj");
            
            ObjectPositions.Add(new Vector3(-3, -0.5f, -3));
            ObjectPositions.Add(new Vector3(0, -0.5f, -3));
            ObjectPositions.Add(new Vector3(3, -0.5f, -3));
            ObjectPositions.Add(new Vector3(-3, -0.5f, 0));
            ObjectPositions.Add(new Vector3(0, -0.5f, 0));
            ObjectPositions.Add(new Vector3(3, -0.5f, 0));
            ObjectPositions.Add(new Vector3(-3, -0.5f, 3));
            ObjectPositions.Add(new Vector3(0, -0.5f, 3));
            ObjectPositions.Add(new Vector3(3, -0.5f, 3));
            
            // Configure g-buffer framebuffer
            gBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, gBuffer);
            // position color buffer
            gPosition = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, gPosition);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, 800, 600, 0, PixelFormat.Rgba, PixelType.Float, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,  TextureTarget.Texture2D, gPosition, 0);
            
            // normal color buffer
            gNormal = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, gNormal);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, 800, 600, 0, PixelFormat.Rgba, PixelType.Float, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1,  TextureTarget.Texture2D, gNormal, 0);
            // color + specular color buffer
            gAlbedoSpec = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, gAlbedoSpec);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, 800, 600, 0, PixelFormat.Rgba, PixelType.UnsignedByte, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2,  TextureTarget.Texture2D, gAlbedoSpec, 0);
    
            // tell OpenGL which color attachments we'll use (of this framebuffer) for rendering 
            DrawBuffersEnum[] attachments = { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2 };
            GL.DrawBuffers(attachments.Length, attachments);
            // create and attach depth buffer (renderbuffer)
            rboDepth = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboDepth);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, 800, 600);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, rboDepth);
            // finally check if framebuffer is complete
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer not complete!");
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // lighting info
            const int NumberOfLights = 32;
            var rand = new Random();
            for (int i = 0; i < NumberOfLights; i++)
            {
                // calculate slightly random offsets
                var xPos = rand.Next() % 100 / 100.0f * 6.0f - 3.0f;
                var yPos = rand.Next() % 100 / 100.0f * 6.0f - 4.0f;
                var zPos = rand.Next() % 100 / 100.0f * 6.0f - 3.0f;
                LightPositions.Add(new Vector3(xPos, yPos, zPos));
                // also calculate random color
                var rColor = rand.Next() % 100 / 200.0f + 0.5f; // between 0.5 and 1.0
                var gColor = rand.Next() % 100 / 200.0f + 0.5f; // between 0.5 and 1.0
                var bColor = rand.Next() % 100 / 200.0f + 0.5f; // between 0.5 and 1.0
                LightColors.Add(new Vector3(rColor, gColor, bColor));
            }
            
            // load shaders
            shaderGeometryPass = new Shader("Shaders/g_buffer.vert", "Shaders/g_buffer.frag");
            shaderLightingPass = new Shader("Shaders/deferred_shading.vert", "Shaders/deferred_shading.frag");
            shaderLightBox = new Shader("Shaders/deferred_light_box.vert", "Shaders/deferred_light_box.frag");
            
            shaderLightingPass.Use();
            shaderLightingPass.SetInt("gPosition", 0);
            shaderLightingPass.SetInt("gNormal", 1);
            shaderLightingPass.SetInt("gAlbedoSpec", 2);
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);
            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            GL.ClearColor(0.1f,0.1f,0.1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            

            // 1. geometry pass: render scene's geometry/color data into gbuffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, gBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var projections = _camera.GetProjectionMatrix();
            var views = _camera.GetViewMatrix();
            var model = Matrix4.Identity;

            shaderGeometryPass.Use();
            shaderGeometryPass.SetMatrix4("projection", projections);
            shaderGeometryPass.SetMatrix4("view", views);
            for (int i = 0; i < ObjectPositions.Count; i++)
            {
                model = Matrix4.Identity;
                model = Matrix4.CreateTranslation(ObjectPositions[i]) * model;
                model = Matrix4.CreateScale(0.5f) * model;
                shaderGeometryPass.SetMatrix4("model", model);
                backpack.Draw(shaderGeometryPass);
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // 2. lighting pass: calculate lighting by iterating over a screen filled quad pixel-by-pixel using the gbuffer's content.
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            shaderLightingPass.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, gPosition);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, gNormal);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, gAlbedoSpec);
            // send light relevant uniforms
            for (int i = 0; i < LightPositions.Count; i++)
            {
                shaderLightingPass.SetVector3("lights[" + i + "].Position", LightPositions[i]);
                shaderLightingPass.SetVector3("lights[" + i + "].Color", LightColors[i]);
                // update attenuation parameters and calculate radius
                const float constant = 1.0f; // note that we don't send this to the shader, we assume it is always 1.0 (in our case)
                const float linear = 0.7f;
                const float quadratic = 1.8f;
                shaderLightingPass.SetFloat("lights[" + i + "].Linear", linear);
                shaderLightingPass.SetFloat("lights[" + i + "].Quadratic", quadratic);
                // then calculate radius of light volume/sphere
                var maxBrightness = Math.Max(Math.Max(LightColors[i].X, LightColors[i].Y), LightColors[i].Z);
                var sqrt = MathHelper.Sqrt(linear * linear - 4 * quadratic * (constant - (256.0f / 5.0f) * maxBrightness));
                var radius = (float)(-linear + sqrt) / (2.0f * quadratic);
                shaderLightingPass.SetFloat("lights[" + i + "].Radius", radius);

            }
            shaderLightingPass.SetVector3("viewPos", _camera.Position);
            RenderQuad();

            // 2.5. copy content of geometry's depth buffer to default framebuffer's depth buffer
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, gBuffer);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0); // write to default framebuffer
            // blit to default framebuffer. Note that this may or may not work as the internal formats of both the FBO and default framebuffer have to match.
            // the internal formats are implementation defined. This works on all of my systems, but if it doesn't on yours you'll likely have to write to the 		
            // depth buffer in another shader stage (or somehow see to match the default framebuffer's internal format with the FBO's internal format).
            GL.BlitFramebuffer(0, 0, Size.X, Size.Y, 0, 0, Size.X, Size.Y, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // 3. render lights on top of scene
            shaderLightBox.Use();
            shaderLightBox.SetMatrix4("projection", projections);
            shaderLightBox.SetMatrix4("view", views);
            for (int i = 0; i < LightPositions.Count; i++)
            {
                model = Matrix4.Identity;
                model = Matrix4.CreateTranslation(LightPositions[i]) * model;
                model = Matrix4.CreateScale(0.125f) * model;
                shaderLightBox.SetMatrix4("model", model);
                shaderLightBox.SetVector3("lightColor", LightColors[i]);
                RenderCube();
            }

            SwapBuffers();
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

        private int quadVao = 0;
        private int quadVbo = 0;
        
        private void RenderQuad()
        {
            if (quadVao == 0)
            {
                float[] quadVertices = {
                    // positions        // texture Coords
                    -1.0f,  1.0f, 0.0f, 0.0f, 1.0f,
                    -1.0f, -1.0f, 0.0f, 0.0f, 0.0f,
                    1.0f,  1.0f, 0.0f, 1.0f, 1.0f,
                    1.0f, -1.0f, 0.0f, 1.0f, 0.0f,
                };
                // setup plane VAO
                quadVao = GL.GenVertexArray();
                quadVbo = GL.GenBuffer();
                GL.BindVertexArray(quadVao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo);
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * quadVertices.Length, quadVertices, BufferUsageHint.StaticDraw);
                
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (3 * sizeof(float)));
            }
            
            // Render the cube
            GL.BindVertexArray(quadVao);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
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
