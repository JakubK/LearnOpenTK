using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace LearnOpenTK
{
    // In this tutorial we focus on how to set up a scene with multiple lights, both of different types but also
    // with several point lights
    public class Window : GameWindow
    {
        private Shader shader;
        private Shader screenShader;
        
        private Camera _camera;
        
        private bool _firstMove = true;

        private Vector2 _lastPos;

        private int cubeVao;
        private int cubeVbo;
        
        private float[] cubeVertices = {
            // positions       
            -0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f,  0.5f, -0.5f,
            0.5f,  0.5f, -0.5f,
            -0.5f,  0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,

            -0.5f, -0.5f,  0.5f,
            0.5f, -0.5f,  0.5f,
            0.5f,  0.5f,  0.5f,
            0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,

            -0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f, -0.5f,
            -0.5f, -0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,

            0.5f,  0.5f,  0.5f,
            0.5f,  0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f,  0.5f,
            0.5f,  0.5f,  0.5f,

            -0.5f, -0.5f, -0.5f,
            0.5f, -0.5f, -0.5f,
            0.5f, -0.5f,  0.5f,
            0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f,  0.5f,
            -0.5f, -0.5f, -0.5f,

            -0.5f,  0.5f, -0.5f,
            0.5f,  0.5f, -0.5f,
            0.5f,  0.5f,  0.5f,
            0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f,  0.5f,
            -0.5f,  0.5f, -0.5f
        };
        
        private int quadVao;
        private int quadVbo;
        
        private float[] quadVertices = {   // vertex attributes for a quad that fills the entire screen in Normalized Device Coordinates.
            // positions   // texCoords
            -1.0f,  1.0f,  0.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 0.0f,
            1.0f, -1.0f,  1.0f, 0.0f,

            -1.0f,  1.0f,  0.0f, 1.0f,
            1.0f, -1.0f,  1.0f, 0.0f,
            1.0f,  1.0f,  1.0f, 1.0f
        };

        private int screenTexture;
        private int framebuffer;
        private int intermediateFbo;
        

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);
            
            // Shader
            shader = new Shader("Shaders/antialiasing.vs",  "Shaders/antialiasing.fs");
            screenShader = new Shader("Shaders/aa_post.vs",  "Shaders/aa_post.fs");

            // Cube
            cubeVao = GL.GenVertexArray();
            cubeVbo = GL.GenBuffer();
            GL.BindVertexArray(cubeVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, cubeVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, cubeVertices.Length * sizeof(float), cubeVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            
            // Quad
            quadVao = GL.GenVertexArray();
            quadVbo = GL.GenBuffer();
            GL.BindVertexArray(quadVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float),  2 * sizeof(float));
            
            // Configure MSAA Framebuffer
            framebuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            
            // Create a multi-sampled color attachment texture
            var textureColorBufferMultiSampled = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DMultisample, textureColorBufferMultiSampled);
            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, 4, PixelInternalFormat.Rgb, 800, 600, true);
            GL.BindTexture(TextureTarget.Texture2DMultisample, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, textureColorBufferMultiSampled, 0);
            
            // Create a mulit-sampled renderbuffer object for depth & stencil attachments
            var rbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 4, RenderbufferStorage.Depth24Stencil8, 800, 600);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer not complete");
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // Configure second post-processing framebuffer
            intermediateFbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, intermediateFbo);
            
            // Create color attachment texture
            screenTexture =  GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, screenTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 800, 600, 0, PixelFormat.Rgb, PixelType.Byte, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, screenTexture, 0); // we only need a color buffer
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Framebuffer not complete");
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            screenShader.Use();
            screenShader.SetInt("screenTexture", 0);
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            
            // 1. Draw screne as normal in multisampled buffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);
            
            // Set transformation matrices
            shader.Use();
            
            shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            shader.SetMatrix4("view", _camera.GetViewMatrix());
            shader.SetMatrix4("model", Matrix4.Identity);
            
            GL.BindVertexArray(cubeVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0 ,36);
            
            // 2. Now blit multisampled buffers to normal colorbuffer of intermediate fbo. Image is stored in screenTexture
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebuffer);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, intermediateFbo);
            GL.BlitFramebuffer(0, 0, 800, 600, 0,0, 800, 600, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            
            // 3. Now render quad with scene's visuals as its texture image
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.ClearColor(1f, 1f, 1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Disable(EnableCap.DepthTest);
            
            // Draw Screen quad
            screenShader.Use();
            GL.BindVertexArray(quadVao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, screenTexture);
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
