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
        private Shader shader;
        
        private Texture _texture;

        private int cubeVao;
        private int cubeVbo;

        private bool _firstMove = true;

        private Vector2 _lastPos;
        
        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            
            GL.LoadBindings(new GLFWBindingsContext());
            // Enable debug context
            GL.GetInteger(GetPName.ContextFlags, out int flags);
            
            GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 4);
            GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 3);

            if ((flags & (int)ContextFlagMask.ContextFlagDebugBit) != 0)
            {
                GL.Enable(EnableCap.DebugOutput);
                GL.Enable(EnableCap.DebugOutputSynchronous);

                GL.DebugMessageCallback(GLUtils.DebugCallback, IntPtr.Zero);
                GL.DebugMessageControl(
                    DebugSourceControl.DontCare,
                    DebugTypeControl.DontCare,
                    DebugSeverityControl.DontCare,
                    0,
                    Array.Empty<int>(),
                    true);
            }
            
            
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            shader = new Shader("Shaders/debugging.vs", "Shaders/debugging.fs");
            _texture = Texture.LoadFromFile("Resources/wood.png");
            
            float[] vertices = {
                 // back face
                -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, // bottom-left
                 0.5f,  0.5f, -0.5f,  1.0f,  1.0f, // top-right
                 0.5f, -0.5f, -0.5f,  1.0f,  0.0f, // bottom-right         
                 0.5f,  0.5f, -0.5f,  1.0f,  1.0f, // top-right
                -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, // bottom-left
                -0.5f,  0.5f, -0.5f,  0.0f,  1.0f, // top-left
                 // front face
                -0.5f, -0.5f,  0.5f,  0.0f,  0.0f, // bottom-left
                 0.5f, -0.5f,  0.5f,  1.0f,  0.0f, // bottom-right
                 0.5f,  0.5f,  0.5f,  1.0f,  1.0f, // top-right
                 0.5f,  0.5f,  0.5f,  1.0f,  1.0f, // top-right
                -0.5f,  0.5f,  0.5f,  0.0f,  1.0f, // top-left
                -0.5f, -0.5f,  0.5f,  0.0f,  0.0f, // bottom-left
                 // left face
                -0.5f,  0.5f,  0.5f, -1.0f,  0.0f, // top-right
                -0.5f,  0.5f, -0.5f, -1.0f,  1.0f, // top-left
                -0.5f, -0.5f, -0.5f, -0.0f,  1.0f, // bottom-left
                -0.5f, -0.5f, -0.5f, -0.0f,  1.0f, // bottom-left
                -0.5f, -0.5f,  0.5f, -0.0f,  0.0f, // bottom-right
                -0.5f,  0.5f,  0.5f, -1.0f,  0.0f, // top-right
                 // right face
                 0.5f,  0.5f,  0.5f,  1.0f,  0.0f, // top-left
                 0.5f, -0.5f, -0.5f,  0.0f,  1.0f, // bottom-right
                 0.5f,  0.5f, -0.5f,  1.0f,  1.0f, // top-right         
                 0.5f, -0.5f, -0.5f,  0.0f,  1.0f, // bottom-right
                 0.5f,  0.5f,  0.5f,  1.0f,  0.0f, // top-left
                 0.5f, -0.5f,  0.5f,  0.0f,  0.0f, // bottom-left     
                 // bottom face
                -0.5f, -0.5f, -0.5f,  0.0f,  1.0f, // top-right
                 0.5f, -0.5f, -0.5f,  1.0f,  1.0f, // top-left
                 0.5f, -0.5f,  0.5f,  1.0f,  0.0f, // bottom-left
                 0.5f, -0.5f,  0.5f,  1.0f,  0.0f, // bottom-left
                -0.5f, -0.5f,  0.5f,  0.0f,  0.0f, // bottom-right
                -0.5f, -0.5f, -0.5f,  0.0f,  1.0f, // top-right
                 // top face
                -0.5f,  0.5f, -0.5f,  0.0f,  1.0f, // top-left
                 0.5f,  0.5f,  0.5f,  1.0f,  0.0f, // bottom-right
                 0.5f,  0.5f, -0.5f,  1.0f,  1.0f, // top-right     
                 0.5f,  0.5f,  0.5f,  1.0f,  0.0f, // bottom-right
                -0.5f,  0.5f, -0.5f,  0.0f,  1.0f, // top-left
                -0.5f,  0.5f,  0.5f,  0.0f,  0.0f  // bottom-left        
            };
            cubeVao = GL.GenVertexArray();
            cubeVbo = GL.GenBuffer();
            // fill buffer
            // GL.BindBuffer(BufferTarget.ArrayBuffer, cubeVbo); // Comment this line to emit DebugMessageCallback
            GL.Enable((EnableCap)12345); // Uncomment this line to emit GL.GetError
            GLUtils.CheckError();
            
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            // link vertex attributes
            GL.BindVertexArray(cubeVao);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (3 * sizeof(float)));
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            
            
            // then before rendering, configure the viewport to the original framebuffer's screen dimensions
            GL.Viewport(0, 0, Size.X, Size.Y);
            
            shader.SetInt("tex", 0);
            
            var projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f), 800f / 600f, 0.1f, 100f
            );
            shader.SetMatrix4("projection", projection);
            
            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.ClearColor(0.2f,0.3f,0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
      
            shader.Use();
            var rotationSpeed = 10f;
            var angle = (float)GLFW.GetTime() * rotationSpeed;
            var model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new(0, 0, -2.5f)) * model;
            model = Matrix4.CreateFromAxisAngle(new(1, 1, 1), MathHelper.DegreesToRadians(angle)) * model;
            
            shader.SetMatrix4("model", model);
            
            GL.BindTexture(TextureTarget.Texture2D, _texture.Handle);
            GL.BindVertexArray(cubeVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            GL.BindVertexArray(0);
            

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
        }
        

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
        }
    }
}
