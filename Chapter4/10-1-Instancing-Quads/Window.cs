using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace LearnOpenTK
{
    // In this tutorial we focus on how to set up a scene with multiple lights, both of different types but also
    // with several point lights
    public class Window : GameWindow
    {
        private Shader shader;
        
        private Camera _camera;

        private List<Vector2> translations = new();
        
        private float[] quadVertices = {
            // positions     // colors
            -0.05f,  0.05f,  1.0f, 0.0f, 0.0f,
            0.05f, -0.05f,  0.0f, 1.0f, 0.0f,
            -0.05f, -0.05f,  0.0f, 0.0f, 1.0f,

            -0.05f,  0.05f,  1.0f, 0.0f, 0.0f,
            0.05f, -0.05f,  0.0f, 1.0f, 0.0f,
            0.05f,  0.05f,  0.0f, 1.0f, 1.0f
        };

        private int instanceVbo;

        private int quadVbo;
        private int quadVao;


        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);

            translations = new List<Vector2>();
            var offset = 0.1f;
            for (int y = -10; y < 10; y += 2)
            {
                for (int x = -10; x < 10; x += 2)
                {
                    translations.Add(new Vector2(x / 10.0f + offset, y / 10.0f + offset));
                }
            }
            
            // Shader
            shader = new Shader("Shaders/instancing.vs",  "Shaders/instancing.fs");
            
            
            // Instances
            instanceVbo = GL.GenBuffer();
            var vec2Size = sizeof(float) * 2;
            var data = translations.ToArray();
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vec2Size * 100, data, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            
            // Quad
            quadVao = GL.GenVertexArray();
            quadVbo = GL.GenBuffer();
            GL.BindVertexArray(quadVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float,  false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float,  false, 5 * sizeof(float), 2 * sizeof(float));
            
            // Set instance data
            GL.EnableVertexAttribArray(2);
            GL.BindBuffer(BufferTarget.ArrayBuffer, instanceVbo);
            GL.VertexAttribPointer(2, 2,  VertexAttribPointerType.Float,  false, 2 * sizeof(float), 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.VertexAttribDivisor(2, 1); // Tell OpenGL that this is an instanced vertex attribute
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.Use();

            GL.BindVertexArray(quadVao);
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, 100);
            GL.BindVertexArray(0);
            
            SwapBuffers();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
        }
    }
}
