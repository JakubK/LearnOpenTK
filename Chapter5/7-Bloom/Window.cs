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
        private int[] pingpongFbo = new[] { 0, 0 };
        private int[] pingpongColorbuffers = new[] { 0, 0 };
        
        private int[] colorBuffers = new []{ 0, 0 };
        
        private int rboDepth;
        private int colorBuffer;
        
        private List<Vector3> LightPositions;
        private List<Vector3> LightColors;
        
        private int hdrFbo;
        
        private bool bloom = true;
        private bool bloomKeyPressed;
        private float exposure = 1.0f;
        
        private float nearPlane = 1f;
        private float farPlane = 25f;
        
        private Texture woodTexture;
        private Texture containerTexture;
        
        private Shader _shader;
        private Shader _shaderLight;
        private Shader _shaderBlur;
        private Shader _shaderBloomFinal;
        
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
            woodTexture = Texture.LoadFromFile("Resources/wood.png", TextureWrapMode.Repeat, true);
            containerTexture = Texture.LoadFromFile("Resources/container2.png", TextureWrapMode.Repeat, true);
            
            hdrFbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, hdrFbo);
            GL.GenTextures(colorBuffers.Length, colorBuffers);
            
            // Create 2 floating point buffers (1 for normal rendering, other for brightness threshold values)
            for (int i = 0; i < colorBuffers.Length; i++)
            {
                // attach texture to framebuffer
                GL.BindTexture(TextureTarget.Texture2D, colorBuffers[i]);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, 800, 600, 0, PixelFormat.Rgba, PixelType.Float, 0);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge); // we clamp to the edge as the blur filter would otherwise sample repeated texture values!
                
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture2D, colorBuffers[i], 0);
            }
            
            // create and attach depth buffer (renderbuffer)
            rboDepth = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboDepth);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, 800, 600);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, rboDepth);

            // tell OpenGL which color attachments we'll use (of this framebuffer) for rendering 
            DrawBuffersEnum[] attachments = { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 };
            GL.DrawBuffers(attachments.Length, attachments);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer not complete!");
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            GL.GenFramebuffers(2, pingpongFbo);
            GL.GenTextures(2, pingpongColorbuffers);

            for (int i = 0; i < pingpongColorbuffers.Length; i++)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, pingpongFbo[i]);
                GL.BindTexture(TextureTarget.Texture2D, pingpongColorbuffers[i]);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, 800, 600, 0, PixelFormat.Rgba, PixelType.Float, 0);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,  (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,  (int)TextureWrapMode.ClampToEdge);
                
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, pingpongColorbuffers[i], 0);
                
                // also check if framebuffers are complete (no need for depth buffer)
                if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                {
                    Console.WriteLine("Framebuffer not complete!");
                }
            }
            
            
            // Lighting info
            LightPositions = new List<Vector3>();
            LightPositions.Add(new  Vector3(0, 0.5f, 1.5f));
            LightPositions.Add(new  Vector3(-4f, 0.5f, -3f));
            LightPositions.Add(new  Vector3(3f, 0.5f, 4f));
            LightPositions.Add(new  Vector3(-0.8f, 2.4f, -1f));

            LightColors = new List<Vector3>();
            LightColors.Add(new  Vector3(5, 5, 5));
            LightColors.Add(new  Vector3(10, 0, 0));
            LightColors.Add(new  Vector3(0, 0, 15f));
            LightColors.Add(new  Vector3(0, 5f, 0));
            
            // shader configuration
            _shader = new Shader("Shaders/bloom.vs", "Shaders/bloom.fs");
            _shaderLight = new Shader("Shaders/bloom.vs", "Shaders/light_box.fs");
            _shaderBlur = new Shader("Shaders/blur.vs", "Shaders/blur.fs");
            _shaderBloomFinal = new Shader("Shaders/bloom_final.vs", "Shaders/bloom_final.fs");
            
            _shader.Use();
            _shader.SetInt("diffuseTexture", 0);
            
            _shaderBlur.Use();
            _shaderBlur.SetInt("image", 0);
            
            _shaderBloomFinal.Use();
            _shaderBloomFinal.SetInt("scene", 0);
            _shaderBloomFinal.SetInt("bloomBlur", 1);
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            GL.ClearColor(0.1f,0.1f,0.1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            // 1. render scene into floating point framebuffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, hdrFbo);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _shader.Use();
            _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _shader.SetMatrix4("view", _camera.GetViewMatrix());
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, woodTexture.Handle);
            
            // Set lighting uniforms
            for (int i = 0; i < LightPositions.Count; i++)
            {
                _shader.SetVector3($"lights[{i}].Position", LightPositions[i]);
                _shader.SetVector3($"lights[{i}].Color", LightColors[i]);
            }
            
            // create one large cube that acts as the floor
            var model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new  Vector3(0, -1.0f, 0)) * model;
            model = Matrix4.CreateScale(new Vector3(12.5f, 0.5f, 12.5f)) * model;
            _shader.SetMatrix4("model", model);
            RenderCube();
            
            // then create multiple cubes as the scenery
            GL.BindTexture(TextureTarget.Texture2D, containerTexture.Handle);
            
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new  Vector3(0, 1.5f, 0)) * model;
            model =  Matrix4.CreateScale(new Vector3(0.5f)) * model;
            _shader.SetMatrix4("model", model);
            RenderCube();
            
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new  Vector3(2, 0, 1)) * model;
            model =  Matrix4.CreateScale(new Vector3(0.5f)) * model;
            _shader.SetMatrix4("model", model);
            RenderCube();

            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new  Vector3(-1, -1, 2)) * model;
            model = Matrix4.CreateFromAxisAngle(new Vector3(1, 0, 1), MathHelper.DegreesToRadians(60f)) * model;
            _shader.SetMatrix4("model", model);
            RenderCube();
            
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new  Vector3(0, 2.7f, 4)) * model;
            model = Matrix4.CreateFromAxisAngle(new Vector3(1, 0, 1), MathHelper.DegreesToRadians(23)) * model;
            model =  Matrix4.CreateScale(new Vector3(1.25f)) * model;
            _shader.SetMatrix4("model", model);
            RenderCube();

            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new  Vector3(-2, 1, -3)) * model;
            model = Matrix4.CreateFromAxisAngle(new Vector3(1, 0, 1), MathHelper.DegreesToRadians(124f)) * model;
            _shader.SetMatrix4("model", model);
            RenderCube();

            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new  Vector3(-3, 0, 0)) * model;
            model =  Matrix4.CreateScale(new Vector3(0.5f)) * model;
            _shader.SetMatrix4("model", model);
            RenderCube();
            
            // finally show all the light sources as bright cubes
            _shaderLight.Use();
            _shaderLight.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _shaderLight.SetMatrix4("view", _camera.GetViewMatrix());

            for (int i = 0; i < LightPositions.Count; i++)
            {
                model = Matrix4.Identity;
                model = Matrix4.CreateTranslation(LightPositions[i]) * model;
                model =  Matrix4.CreateScale(new Vector3(0.25f)) * model;
                
                _shaderLight.SetMatrix4("model", model);
                _shaderLight.SetVector3("lightColor", LightColors[i]);
                RenderCube();
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            var horizontal = 1;
            var firstIteration = true;
            var amount = 10;
            _shaderBlur.Use();

            for (int i = 0; i < amount; i++)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, pingpongFbo[horizontal]);
                _shaderBlur.SetInt("horizontal", horizontal);
                
                GL.BindTexture(TextureTarget.Texture2D, firstIteration ? colorBuffers[1] : pingpongColorbuffers[1 - horizontal]); // bind texture of other framebuffer (or scene if first iteration)
                RenderQuad();
                horizontal = 1 - horizontal;
                if (firstIteration)
                {
                    firstIteration = false;
                }
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // 3. now render floating point color buffer to 2D quad and tonemap HDR colors to default framebuffer's (clamped) color range
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _shaderBloomFinal.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, colorBuffers[0]);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, pingpongColorbuffers[1 - horizontal]);
            _shaderBloomFinal.SetBool("bloom", bloom);
            _shaderBloomFinal.SetFloat("exposure", exposure);
            RenderQuad();
            
            
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

            if (input.IsKeyDown(Keys.B))
            {
                bloom = !bloom;
                bloomKeyPressed = true;
            }

            if (input.IsKeyReleased(Keys.B))
            {
                bloomKeyPressed = false;
            }
            
            if (input.IsKeyDown(Keys.Q)) 
            {
                if (exposure > 0.0f)
                    exposure -= 0.001f;
                else
                    exposure = 0.0f;
            }
            
            if (input.IsKeyDown(Keys.E)) 
            {
                exposure += 0.001f;
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
