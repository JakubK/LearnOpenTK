using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using Camera = LearnOpenTK.Common.Camera;

namespace LearnOpenTK
{
    public class Window : GameWindow
    {
        private Shader solidShader;
        private Shader transparentShader;
        private Shader compositeShader;
        private Shader screenShader;

        private int revealTexture;
        private int accumTexture;
        private int opaqueTexture;

        private float[] zeroFillerVec = new[] { 0f, 0f, 0f, 0f };
        private float[] oneFillerVec = new[] { 1f, 1f, 1f, 1f };
        
        private Camera _camera;

        private bool _firstMove = true;

        private int quadVao;
        private int quadVbo;

        private Matrix4 redModelMat;
        private Matrix4 greenModelMat;
        private Matrix4 blueModelMat;

        private int opaqueFbo;
        private int transparentFbo;
        
        private Vector2 _lastPos;
        
        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings) {}

        protected override void OnLoad()
        {
            base.OnLoad();
            _camera = new Camera(new Vector3(0,0,5), Size.X / (float)Size.Y);

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.Blend);
            
            float[] quadVertices = {
                // positions		// uv
                -1.0f, -1.0f, 0.0f,	0.0f, 0.0f,
                1.0f, -1.0f, 0.0f, 1.0f, 0.0f,
                1.0f,  1.0f, 0.0f, 1.0f, 1.0f,

                1.0f,  1.0f, 0.0f, 1.0f, 1.0f,
                -1.0f,  1.0f, 0.0f, 0.0f, 1.0f,
                -1.0f, -1.0f, 0.0f, 0.0f, 0.0f
            };
            
            quadVao = GL.GenVertexArray();
            quadVbo = GL.GenBuffer();
            
            GL.BindVertexArray(quadVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * quadVertices.Length, quadVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (3 * sizeof(float)));
            GL.BindVertexArray(0);
            

            // set up framebuffers and their texture attachments
            opaqueFbo = GL.GenFramebuffer();
            transparentFbo = GL.GenFramebuffer();

            // set up attachments for opaque framebuffer
            opaqueTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, opaqueTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, 800, 600, 0, PixelFormat.Rgba, PixelType.HalfFloat, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            var depthTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, 800, 600, 0, PixelFormat.DepthComponent, PixelType.Float, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, opaqueFbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, opaqueTexture, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTexture, 0);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Opaque Framebuffer not complete");
            }
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // set up attachments for transparent framebuffer
            accumTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, accumTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, 800, 600, 0, PixelFormat.Rgba, PixelType.HalfFloat, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            revealTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, revealTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, 800, 600, 0, PixelFormat.Red, PixelType.UnsignedByte, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, transparentFbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, accumTexture, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, revealTexture, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTexture, 0); // opaque framebuffer's depth texture
            
            DrawBuffersEnum[] transparentDrawBuffers = { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 };
            GL.DrawBuffers(2, transparentDrawBuffers);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Transparent Framebuffer not complete");
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);


            redModelMat = CalculateModelMatrix(new (0, 0, 1));
            greenModelMat = CalculateModelMatrix(new (0, 0, 0));
            blueModelMat = CalculateModelMatrix(new (0, 0, 2));

            solidShader = new Shader("Shaders/solid.vs", "Shaders/solid.fs");
            transparentShader = new Shader("Shaders/transparent.vs", "Shaders/transparent.fs");
            compositeShader = new Shader("Shaders/composite.vs", "Shaders/composite.fs");
            screenShader = new Shader("Shaders/screen.vs", "Shaders/screen.fs");
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            var projection = _camera.GetProjectionMatrix();
            var view = _camera.GetViewMatrix();

            var vp = view * projection;
            
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
            
            GL.ClearColor(0,0,0,0);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, opaqueFbo);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            solidShader.Use();
            solidShader.SetMatrix4("mvp", redModelMat * vp);
            solidShader.SetVector3("color", new (1, 0, 0));
            GL.BindVertexArray(quadVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            
            GL.DepthMask(false);
            GL.Enable(EnableCap.Blend);
            
            GL.BlendFunc(0, BlendingFactorSrc.One, BlendingFactorDest.One);
            GL.BlendFunc(1, BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcColor);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, transparentFbo);
            GL.ClearBuffer(ClearBuffer.Color, 0, zeroFillerVec);
            GL.ClearBuffer(ClearBuffer.Color, 1, oneFillerVec);

            transparentShader.Use();
            transparentShader.SetMatrix4("mvp", greenModelMat * vp);
            transparentShader.SetVector4("color", new (0,1,0,0.5f));
            GL.BindVertexArray(quadVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            
            transparentShader.SetMatrix4("mvp", blueModelMat * vp);
            transparentShader.SetVector4("color", new (0,0,1,0.5f));
            GL.BindVertexArray(quadVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            
            GL.DepthFunc(DepthFunction.Always);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, opaqueFbo);

            compositeShader.Use();
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, accumTexture);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, revealTexture);
            GL.BindVertexArray(quadVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            
            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.ClearColor(0,0,0,0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            screenShader.Use();
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, opaqueTexture);
            GL.BindVertexArray(quadVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            
            SwapBuffers();
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

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
        }

        private Matrix4 CalculateModelMatrix(Vector3 position)
        {
            var rotation = Vector3.Zero;
            var scale = Vector3.One;
            
            var trans = Matrix4.Identity;

            trans = Matrix4.CreateTranslation(position) * trans;
            
            trans = Matrix4.CreateFromAxisAngle(new (1, 0, 0), MathHelper.DegreesToRadians(rotation.X)) * trans;
            trans = Matrix4.CreateFromAxisAngle(new (0, 1, 0), MathHelper.DegreesToRadians(rotation.Y)) * trans;
            trans = Matrix4.CreateFromAxisAngle(new (0, 0, 1), MathHelper.DegreesToRadians(rotation.Z)) * trans;
            
            trans = Matrix4.CreateScale(scale) * trans;

            return trans;
        }
    }
}
