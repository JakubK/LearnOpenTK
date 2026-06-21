using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace LearnOpenTK
{
    // In this tutorial we focus on how to set up a scene with multiple lights, both of different types but also
    // with several point lights
    public class Window : GameWindow
    {
        private Shader _screenQuad;
        private ComputeShader _computeShader;

        private int _fCounter = 0;

        private const int TextureWidth = 1000;
        private const int TextureHeight = 1000;
        

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);

            // query limitations
            int[] maxInvocations = new[] { 0 };
            Console.WriteLine("OpenGL Limitations:");
            for (int idx = 0; idx < 3; idx++)
            {
                GL.GetInteger((GetIndexedPName)All.MaxComputeWorkGroupCount, idx, out var maxComputeWorkGroupCount);
                GL.GetInteger((GetIndexedPName)All.MaxComputeWorkGroupSize, idx, out var maxComputeWorkGroupSize);
                
                Console.WriteLine("Max number of work groups in dimension " + idx + " " + maxComputeWorkGroupCount);
                Console.WriteLine("Max work group size in dimension " + idx + " " + maxComputeWorkGroupSize);
            }
            GL.GetInteger((GetPName)All.MaxComputeWorkGroupInvocations, maxInvocations);
            Console.WriteLine("Number of invocations in a single local work group that may be dispatched to a compute shader " + maxInvocations[0]);
            
            _screenQuad = new Shader("Shaders/screenQuad.vs", "Shaders/screenQuad.fs");
            _computeShader = new ComputeShader("Shaders/computeShader.comp");

            _screenQuad.Use();
            _screenQuad.SetInt("tex", 0);

            var texture = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, TextureWidth, TextureHeight, 0, PixelFormat.Rgba, PixelType.Float, 0);

            GL.BindImageTexture(0, texture, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            
            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            if (_fCounter > 500)
            {
                Console.WriteLine("FPS: " + 1 / e.Time);
                _fCounter = 0;
            }
            else
            {
                _fCounter++;
            }

            _computeShader.Use();
            _computeShader.SetFloat("t", (float)GLFW.GetTime());
            
            GL.DispatchCompute(TextureWidth / 10, TextureHeight / 10, 1);
        
            // make sure writing to image has finished before read
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
            
            // render image to quad
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _screenQuad.Use();

            RenderQuad();

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
    }
}
