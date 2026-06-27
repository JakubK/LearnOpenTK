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
        private int rez = 20;
        private Shader _heightMapShader;

        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;

        private int terrainVao;
        private int terrainVbo;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            
            _heightMapShader = new Shader(
                "Shaders/gpuheight.vs",
                "Shaders/gpuheight.fs",
                null,
                "Shaders/gpuheight.tcs",
                "Shaders/gpuheight.tes"
            );
            _heightMapShader.Use();

            var texture = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            
            StbImage.stbi_set_flip_vertically_on_load(1);
            using var stream = File.OpenRead("Resources/iceland_heightmap.png");
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            
            
            _heightMapShader.SetInt("heightMap", 0);
            Console.WriteLine($"Loaded height map {image.Width} x {image.Height}");
            

            var vertices = new List<float>();
            var width = image.Width;
            var height = image.Height;
            for (int i = 0; i <= rez - 1; i++)
            {
                for (int j = 0; j <= rez - 1; j++)
                {
                    vertices.Add(-width/2.0f + width*i/(float)rez); // v.x
                    vertices.Add(0.0f); // v.y
                    vertices.Add(-height/2.0f + height*j/(float)rez); // v.z
                    vertices.Add(i / (float)rez); // u
                    vertices.Add(j / (float)rez); // v

                    vertices.Add(-width/2.0f + width*(i+1)/(float)rez); // v.x
                    vertices.Add(0.0f); // v.y
                    vertices.Add(-height/2.0f + height*j/(float)rez); // v.z
                    vertices.Add((i+1) / (float)rez); // u
                    vertices.Add(j / (float)rez); // v

                    vertices.Add(-width/2.0f + width*i/(float)rez); // v.x
                    vertices.Add(0.0f); // v.y
                    vertices.Add(-height/2.0f + height*(j+1)/(float)rez); // v.z
                    vertices.Add(i / (float)rez); // u
                    vertices.Add((j+1) / (float)rez); // v

                    vertices.Add(-width/2.0f + width*(i+1)/(float)rez); // v.x
                    vertices.Add(0.0f); // v.y
                    vertices.Add(-height/2.0f + height*(j+1)/(float)rez); // v.z
                    vertices.Add((i+1) / (float)rez); // u
                    vertices.Add((j+1) / (float)rez); // v
                }
            }
            Console.WriteLine($"Loaded {rez * rez} patches of 4 control points each");
            Console.WriteLine($"Processing {4 * rez * rez} vertices in vertex shader");

            terrainVao = GL.GenVertexArray();
            GL.BindVertexArray(terrainVao);

            terrainVbo = GL.GenBuffer();
            var vertexArray = vertices.ToArray();
            GL.BindBuffer(BufferTarget.ArrayBuffer, terrainVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertexArray, BufferUsageHint.StaticDraw);
            
            // Position attribute
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            
            // texCoord attribute
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            GL.PatchParameter(PatchParameterInt.PatchVertices, 4);

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            
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
            GL.DrawArrays(PrimitiveType.Patches, 0, 4 * rez * rez);
            
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
