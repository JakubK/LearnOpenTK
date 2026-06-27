using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace LearnOpenTK
{
    public class Window : GameWindow
    {
        private Shader _shader;
        private int vao;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        // Now, we start initializing OpenGL.
        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            

            _shader = new Shader("Shaders/shader.vs", "Shaders/shader.fs");
            _shader.Use();
            
            float[] vertices = {
                0.5f,  0.5f, 0.0f,
                0.5f, -0.5f, 0.0f,
                -0.5f, -0.5f, 0.0f,
                -0.5f,  0.5f, 0.0f
            };

            // Here we create the VBO with DSA.
            var vbo = 0;
            
            // Note how we do not have to call glBindBuffer() after the create call.
            GL.CreateBuffers(1, out vbo);
            GL.NamedBufferStorage(vbo, vertices.Length * sizeof(float), vertices, 0);
            
            int[] indices = {
                0, 1, 3,
                1, 2, 3
            };

            // Here we create the EBO with DSA while also specifying that the buffer is mutable from the client side.
            var ebo = 0;
            GL.CreateBuffers(1, out ebo);
            GL.NamedBufferStorage(ebo, indices.Length * sizeof(int), 0, BufferStorageFlags.DynamicStorageBit);
            GL.NamedBufferSubData(ebo, 0, indices.Length * sizeof(int), indices);
            
            // Here we create the VAO with DSA and specify its format.
            vao = 0;
            GL.CreateVertexArrays(1, out vao);
            
            // Specifying our vertex layout with the VAO.
            GL.EnableVertexArrayAttrib(vao, 0);
            GL.VertexArrayAttribFormat(vao, 0, 3, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(vao, 0, 0);
            
            // Binding the VBO and Element Buffer to the VAO.
            GL.VertexArrayVertexBuffer(vao, 0, vbo, 0, sizeof(float) * 3);
            GL.VertexArrayElementBuffer(vao, ebo);
        }

        // Now that initialization is done, let's create our render loop.
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            _shader.Use();
            GL.BindVertexArray(vao);
            GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedInt, 0);
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

            // When the window gets resized, we have to call GL.Viewport to resize OpenGL's viewport to match the new size.
            // If we don't, the NDC will no longer be correct.
            GL.Viewport(0, 0, Size.X, Size.Y);
        }
    }
}
