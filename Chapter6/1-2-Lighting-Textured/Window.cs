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

        private int nrRows = 7;
        private int nrColumns = 7;
        private float spacing = 2.5f;
        
        private List<Vector3> LightPositions = new();
        private List<Vector3> LightColors = new();

        private Shader shader;
        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;

        private Texture albedo;
        private Texture normal;
        private Texture metallic;
        private Texture roughness;
        private Texture ao;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);

            shader = new Shader("Shaders/pbr.vs", "Shaders/pbr.fs");
            shader.Use();
            shader.SetInt("albedoMap", 0);
            shader.SetInt("normalMap", 1);
            shader.SetInt("metallicMap", 2);
            shader.SetInt("roughnessMap", 3);
            shader.SetInt("aoMap", 4);
            
            shader.SetMatrix4("projection", _camera.GetProjectionMatrix());

            albedo = Texture.LoadFromFile("Resources/albedo.png");
            normal = Texture.LoadFromFile("Resources/normal.png");
            metallic = Texture.LoadFromFile("Resources/metallic.png");
            roughness = Texture.LoadFromFile("Resources/roughness.png");
            ao = Texture.LoadFromFile("Resources/ao.png");
            
            LightPositions.Add(new(0, 0, 10.0f));
            
            LightColors.Add(new(150.0f, 150.0f, 150.0f));
            
            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            GL.ClearColor(0.1f,0.1f,0.1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            shader.Use();
            shader.SetMatrix4("view", _camera.GetViewMatrix());
            shader.SetVector3("camPos", _camera.Position);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, albedo.Handle);
            
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, normal.Handle);
            
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, metallic.Handle);
            
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, roughness.Handle);
            
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, ao.Handle);
            
            // render rows*column number of spheres with varying metallic/roughness values scaled by rows and columns respectively
            var model = Matrix4.Identity;
            for (int row = 0; row < nrRows; ++row)
            {
                for (int col = 0; col < nrColumns; ++col)
                {
                    model = Matrix4.Identity;
                    model = Matrix4.CreateTranslation(new Vector3((col - (nrColumns / 2)) * spacing,
                        (row - (nrRows / 2)) * spacing, 0)) * model;
                    shader.SetMatrix4("model", model);

                    Matrix3.Transpose(new Matrix3(model.Inverted()), out var normalMatrix);
                    shader.SetMatrix3("normalMatrix", normalMatrix);
                    RenderSphere();
                }
            }

            // render light source (simply re-render sphere at light positions)
            // this looks a bit off as we use the same shader, but it'll make their positions obvious and 
            // keeps the codeprint small.
            for (int i = 0; i < LightPositions.Count; ++i)
            {
                shader.SetVector3("lightPositions[" + i + "]", LightPositions[i]);
                shader.SetVector3("lightColors[" + i + "]", LightColors[i]);
                
                model = Matrix4.Identity;
                model = Matrix4.CreateTranslation(LightPositions[i]) * model;
                model = Matrix4.CreateScale(0.5f) * model;
                
                shader.SetMatrix4("model", model);
                Matrix3.Transpose(new Matrix3(model.Inverted()), out var normalMatrix);
                shader.SetMatrix3("normalMatrix", normalMatrix);
                RenderSphere();
            }

            SwapBuffers();
        }

        private int sphereVao = 0;
        private int indexCount = 0;

        private void RenderSphere()
        {
            if (sphereVao == 0)
            {
                sphereVao = GL.GenVertexArray();
                var vbo = GL.GenBuffer();
                var ebo = GL.GenBuffer();
                var positions = new List<Vector3>();
                var normals = new List<Vector3>();
                var uv = new List<Vector2>();
                var indices = new List<int>();
                const int XSegments = 64;
                const int YSegments = 64;
                const float PI = 3.14159265359f;
                for (int x = 0; x <= XSegments; ++x)
                {
                    for (int y = 0; y <= YSegments; ++y)
                    {
                        
                        var xSegment = (float)x / XSegments;
                        var ySegment = (float)y / YSegments;
                        var xPos = MathF.Cos(xSegment * 2.0f * PI) *
                                   MathF.Sin(ySegment * PI);
                        var yPos = MathF.Cos(ySegment * PI);
                        var zPos = MathF.Sin(xSegment * 2.0f * PI) *
                                   MathF.Sin(ySegment * PI);
                        positions.Add(new (xPos,yPos,zPos));
                        uv.Add(new (xSegment, ySegment));
                        normals.Add(new (xPos,yPos,zPos));
                    }
                }

                var oddRow = false;
                for (int y = 0; y < YSegments; ++y)
                {
                    if (!oddRow) // even rows: y == 0, y == 2; and so on
                    {
                        for (int x = 0; x <= XSegments; ++x)
                        {
                            indices.Add(y * (XSegments + 1) + x);
                            indices.Add((y+1) * (XSegments + 1) + x);
                        }
                    }
                    else
                    {
                        for (int x = XSegments; x >= 0; --x)
                        {
                            indices.Add((y+1) * (XSegments + 1) + x);
                            indices.Add(y * (XSegments + 1) + x);
                        }
                    }

                    oddRow = !oddRow;
                }

                indexCount = indices.Count;
                var data = new List<float>();
                for (int i = 0; i < positions.Count; ++i)
                {
                    data.Add(positions[i].X);
                    data.Add(positions[i].Y);
                    data.Add(positions[i].Z);

                    if (normals.Count > 0)
                    {
                        data.Add(normals[i].X);
                        data.Add(normals[i].Y);
                        data.Add(normals[i].Z);
                    }

                    if (uv.Count > 0)
                    {
                        data.Add(uv[i].X);
                        data.Add(uv[i].Y);
                        
                    }
                }
                
                GL.BindVertexArray(sphereVao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, data.Count * sizeof(float), data.ToArray(), BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(int), indices.ToArray(), BufferUsageHint.StaticDraw);
                var stride = (3 + 2 + 3) * sizeof(float);
                
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
                GL.EnableVertexAttribArray(2);
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            }
            GL.BindVertexArray(sphereVao);
            GL.DrawElements(BeginMode.TriangleStrip, indexCount,  DrawElementsType.UnsignedInt, 0);
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
