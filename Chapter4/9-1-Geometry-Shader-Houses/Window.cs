using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace LearnOpenTK
{
    // Be warned, there is a LOT of stuff here. It might seem complicated, but just take it slow and you'll be fine.
    // OpenGL's initial hurdle is quite large, but once you get past that, things will start making more sense.
    public class Window : GameWindow
    {
        private readonly float[] points = {
            -0.5f,  0.5f, 1.0f, 0.0f, 0.0f, // top-left
            0.5f,  0.5f, 0.0f, 1.0f, 0.0f, // top-right
            0.5f, -0.5f, 0.0f, 0.0f, 1.0f, // bottom-right
            -0.5f, -0.5f, 1.0f, 1.0f, 0.0f  // bottom-left
        };
        
        private int vao;
        private int vbo;

        
        private Shader _shader;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        // Now, we start initializing OpenGL.
        protected override void OnLoad()
        {
            base.OnLoad();

            
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

           
            vbo = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            GL.BufferData(BufferTarget.ArrayBuffer, points.Length * sizeof(float), points, BufferUsageHint.StaticDraw);

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            _shader = new Shader("Shaders/shader.vs", "Shaders/shader.fs", "Shaders/shader.gs");
            _shader.Use();
        }

        // Now that initialization is done, let's create our render loop.
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            _shader.Use();

            GL.BindVertexArray(vao);

            GL.DrawArrays(PrimitiveType.Points, 0, 4);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

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
