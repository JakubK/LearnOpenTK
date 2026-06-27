using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using StbImageSharp;

namespace LearnOpenTK
{
    // In this tutorial we focus on how to set up a scene with multiple lights, both of different types but also
    // with several point lights
    public class Window : GameWindow
    {
        private int numStrips;
        private int numTrisPerStrip;
        private Shader _heightMapShader;

        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;

        private int terrainVao;
        private int terrainVbo;
        private int terrainUbo;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            
            StbImage.stbi_set_flip_vertically_on_load(1);
            using var stream = File.OpenRead("Resources/iceland_heightmap.png");
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            var vertices = new List<float>();
            var yScale = 64f / 256f;
            var yShift = 16f;
            var rez = 1;
            var bytePerPixel = (int)image.SourceComp;

            for (int i = 0; i < image.Height; i++)
            {
                for (int j = 0; j < image.Width; j++)
                {
                    int index = (j + image.Width * i) * bytePerPixel;
                    var y = image.Data[index];
                    
                    vertices.Add(-image.Height/2f + image.Height * i / (float)image.Height); // vx
                    vertices.Add(y * yScale - yShift); // vy
                    vertices.Add(-image.Width/2f + image.Width * j / (float)image.Width); // vz
                }
            }
            Console.WriteLine($"Loaded {vertices.Count / 3} vertices");

            var indices = new List<int>();
            for (int i = 0; i < image.Height - 1; i += rez)
            {
                for (int j = 0; j < image.Width; j += rez)
                {
                    for(int k = 0; k < 2; k++)
                    {
                        indices.Add(j + image.Width * (i + k*rez));
                    }
                }
            }
            Console.WriteLine($"Loaded {indices.Count} indices");

            numStrips = (image.Height - 1) / rez;
            numTrisPerStrip = (image.Width / rez) * 2 - 2;
            Console.WriteLine($"Created lattice {numStrips} strips with {numTrisPerStrip} triangles each");
            Console.WriteLine($"Created {numStrips * numTrisPerStrip} triangles total");

            terrainVao = GL.GenVertexArray();
            GL.BindVertexArray(terrainVao);

            terrainVbo = GL.GenBuffer();
            var vertexArray = vertices.ToArray();
            GL.BindBuffer(BufferTarget.ArrayBuffer, terrainVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertexArray, BufferUsageHint.StaticDraw);
            
            // Position attribute
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            terrainUbo = GL.GenBuffer();
            var indexArray = indices.ToArray();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, terrainUbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(int), indexArray, BufferUsageHint.StaticDraw);
            GL.BindVertexArray(0);
            

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);


            _heightMapShader = new Shader("Shaders/cpuheight.vs", "Shaders/cpuheight.fs");
            _heightMapShader.Use();
            
            // _camera = new Camera(new (67.0f, 627.5f, 169.9f), Size.X / (float)Size.Y);
            _camera = new Camera(Vector3.UnitY * 15, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);

            _heightMapShader.Use();

            _heightMapShader.SetMatrix4("view", _camera.GetViewMatrix());
            _heightMapShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            // Console.WriteLine(_camera.Position.Y);
            var model = Matrix4.Identity;
            _heightMapShader.SetMatrix4("model", model);
            
            GL.BindVertexArray(terrainVao);
            for(var strip = 0; strip < numStrips; strip++)
            {
                GL.DrawElements(BeginMode.TriangleStrip,   // primitive type
                    numTrisPerStrip+2,   // number of indices to render
                    DrawElementsType.UnsignedInt,     // index data type
                    (sizeof(int) * (numTrisPerStrip+2) * strip) // offset to starting index
                ); 
            }
            
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

            const float cameraSpeed = 100.5f;
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
