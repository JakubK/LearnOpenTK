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

        private List<Vector3> ssaoKernel = new();
        private List<Vector3> ssaoNoice = new();

        private Vector3 LightPosition = new(2, 4, -2);
        private Vector3 LightColor = new(0.2f, 0.2f, 0.7f);
        
        
        private Shader shaderGeometryPass;
        private Shader shaderLightingPass;
        private Shader shaderSsao;
        private Shader shaderSsaoBlur;

        private int noiseTexture;

        private int gBuffer;
        private int gPosition;
        private int gNormal;
        private int gAlbedoSpec;
        
        private int ssaoFbo;
        private int ssaoBlurFbo;
        private int ssaoColorBuffer;
        private int ssaoColorBufferBlur;

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

            // also create framebuffer to hold SSAO processing stage
            ssaoFbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ssaoFbo);

            // SSAO color buffer
            ssaoColorBuffer = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, ssaoColorBuffer);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, Size.X, Size.Y, 0, PixelFormat.Red, PixelType.Float, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ssaoColorBuffer, 0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("SSAO Framebuffer not complete!");
            }

            // and blur stage
            ssaoBlurFbo = GL.GenFramebuffer();
            ssaoColorBufferBlur = GL.GenTexture();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ssaoBlurFbo);
            GL.BindTexture(TextureTarget.Texture2D, ssaoColorBufferBlur);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, Size.X, Size.Y, 0, PixelFormat.Red, PixelType.Float, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ssaoColorBufferBlur, 0);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("SSAO Blur Framebuffer not complete!");
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // generate sample kernel
            for (int i = 0;i < 64; i++)
            {
                var sample = new Vector3(
                    (float)(new Random().NextDouble() * 2.0 - 1.0), 
                    (float)(new Random().NextDouble() * 2.0 - 1.0), 
                    (float)(new Random().NextDouble())
                );
                sample = Vector3.Normalize(sample);
                sample *= (float)(new Random().NextDouble());
                var scale = i / 64.0f;
                scale = MathHelper.Lerp(0.1f, 1.0f, scale * scale);
                sample *= scale;
                ssaoKernel.Add(sample);
            }
            
            // generate noise texture
            noiseTexture = GL.GenTexture();
            for (int i = 0;i < 16; i++)
            {
                var noise = new Vector3(
                    (float)(new Random().NextDouble() * 2.0 - 1.0), 
                    (float)(new Random().NextDouble() * 2.0 - 1.0), 
                    0.0f
                ); // rotate around z-axis (in tangent space)
                ssaoNoice.Add(noise);
            } 

            GL.BindTexture(TextureTarget.Texture2D, noiseTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32f, 4, 4, 0, PixelFormat.Rgb, PixelType.Float, ssaoNoice.ToArray());
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            // load shaders
            shaderGeometryPass = new Shader("Shaders/ssao_geometry.vs", "Shaders/ssao_geometry.fs");
            shaderLightingPass = new Shader("Shaders/ssao.vs", "Shaders/ssao_lighting.fs");
            shaderSsao = new Shader("Shaders/ssao.vs", "Shaders/ssao.fs");
            shaderSsaoBlur = new Shader("Shaders/ssao.vs", "Shaders/ssao_blur.fs");
            
            shaderLightingPass.Use();
            shaderLightingPass.SetInt("gPosition", 0);
            shaderLightingPass.SetInt("gNormal", 1);
            shaderLightingPass.SetInt("gAlbedo", 2);
            shaderLightingPass.SetInt("ssao", 3);

            shaderSsao.Use();
            shaderSsao.SetInt("gPosition", 0);
            shaderSsao.SetInt("gNormal", 1);
            shaderSsao.SetInt("texNoise", 2);

            shaderSsaoBlur.Use();
            shaderSsaoBlur.SetInt("ssaoInput", 0);
            
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

            var projection = _camera.GetProjectionMatrix();
            var view = _camera.GetViewMatrix();

            shaderGeometryPass.Use();
            shaderGeometryPass.SetMatrix4("projection", projection);
            shaderGeometryPass.SetMatrix4("view", view);

            // room cube
            var model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new (0,7,0)) * model;
            model = Matrix4.CreateScale(7.5f) * model;
            shaderGeometryPass.SetMatrix4("model", model);
            shaderGeometryPass.SetInt("invertedNormals", 1); // invert normals as we're inside the cube
            RenderCube();
            shaderGeometryPass.SetInt("invertedNormals", 0);
            // backpack model on the floor
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new (0,0.5f,0)) * model;
            model = Matrix4.CreateFromAxisAngle(new Vector3(1.0f, 0.3f, 0.5f), MathHelper.DegreesToRadians(-90f)) * model;
            model = Matrix4.CreateScale(1f) * model;
            shaderGeometryPass.SetMatrix4("model", model);
            backpack.Draw(shaderGeometryPass);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);


            // 2. generate SSAO texture
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ssaoFbo);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            shaderSsao.Use();
            // send kernel + rotation
            for (int i = 0; i < 64; i++)            {
                shaderSsao.SetVector3("samples[" + i + "]", ssaoKernel[i]);
            }
            shaderSsao.SetMatrix4("projection", projection);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, gPosition);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, gNormal);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, noiseTexture);
            RenderQuad();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // 3. blur SSAO texture to remove noise
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ssaoBlurFbo);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            shaderSsaoBlur.Use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, ssaoColorBuffer);
            RenderQuad();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // 4. lighting pass: traditional deferred Blinn-Phong lighting with added screen-space ambient occlusion
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            shaderLightingPass.Use();
            // send light relevant uniforms
            var lightPosView = Vector3.TransformPosition(LightPosition, view);
            shaderLightingPass.SetVector3("light.Position", lightPosView);
            shaderLightingPass.SetVector3("light.Color", LightColor);
            // Update attenuation parameters and calculate radius
            const float linear = 0.09f;
            const float quadratic = 0.032f;
            shaderLightingPass.SetFloat("light.Linear", linear);
            shaderLightingPass.SetFloat("light.Quadratic", quadratic);
            // send gbuffer textures
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, gPosition);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, gNormal);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, gAlbedoSpec);
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, ssaoColorBufferBlur);
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
