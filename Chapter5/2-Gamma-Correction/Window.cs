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
        private Texture floorTexture;
        private Texture floorTextureGammaCorrected;
        
        int gammaEnabled = 0;
        bool gammaKeyPressed = false;
        
        private float[] planeVertices = {
            // positions            // normals         // texcoords
            10.0f, -0.5f,  10.0f,  0.0f, 1.0f, 0.0f,  10.0f,  0.0f,
            -10.0f, -0.5f,  10.0f,  0.0f, 1.0f, 0.0f,   0.0f,  0.0f,
            -10.0f, -0.5f, -10.0f,  0.0f, 1.0f, 0.0f,   0.0f, 10.0f,

            10.0f, -0.5f,  10.0f,  0.0f, 1.0f, 0.0f,  10.0f,  0.0f,
            -10.0f, -0.5f, -10.0f,  0.0f, 1.0f, 0.0f,   0.0f, 10.0f,
            10.0f, -0.5f, -10.0f,  0.0f, 1.0f, 0.0f,  10.0f, 10.0f
        };

        private float[] positions;
        private float[] colors;

        private List<Vector3> LightPositions = new()
        {
            new Vector3(-3f, 0, 0),
            new Vector3(-1f, 0, 0),
            new Vector3(1f, 0, 0),
            new Vector3(3f, 0, 0),
        };
        
        private List<Vector3> LightColors = new()
        {
            new Vector3(0.25f),
            new Vector3(0.5f),
            new Vector3(0.75f),
            new Vector3(1f),
        };

        private int planeVao;
        private int planeVbo;
        
        private Shader _shader;

        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            
            positions = LightPositions
                .SelectMany(v => new[] { v.X, v.Y, v.Z })
                .ToArray();
            
            colors = LightColors
                .SelectMany(v => new[] { v.X, v.Y, v.Z })
                .ToArray();
            
            // plane VAO
            planeVao = GL.GenVertexArray();
            planeVbo = GL.GenBuffer();
            GL.BindVertexArray(planeVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, planeVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * planeVertices.Length, planeVertices, BufferUsageHint.StaticDraw);
            
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
            
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            GL.BindVertexArray(0);

            floorTexture = Texture.LoadFromFile("Resources/wood.png", TextureWrapMode.ClampToEdge);
            floorTextureGammaCorrected = Texture.LoadFromFile("Resources/wood.png", TextureWrapMode.ClampToEdge, true);
            
            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            _shader.Use();
            _shader.SetInt("floorTexture", 0);
            
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            GL.ClearColor(0.1f,0.1f,0.1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            _shader.Use();
            var projection = _camera.GetProjectionMatrix();
            var view = _camera.GetViewMatrix();
            _shader.SetMatrix4("projection", projection);
            _shader.SetMatrix4("view", view);
            
            // set light uniforms
            var lightPositionLocation = GL.GetUniformLocation(_shader.Handle, "lightPositions");
            var lightColorLocation = GL.GetUniformLocation(_shader.Handle, "lightColors");
            
            GL.Uniform3(lightPositionLocation, 4, positions);
            GL.Uniform3(lightColorLocation, 4, colors);
            
            _shader.SetVector3("viewPos", _camera.Position);
            _shader.SetInt("gamma", gammaEnabled);
            
            // floor
            GL.BindVertexArray(planeVao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, gammaEnabled == 1 ? floorTextureGammaCorrected.Handle : floorTexture.Handle);
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
            
            if (input.IsKeyDown(Keys.B) && !gammaKeyPressed) 
            {
                gammaEnabled = 1 - gammaEnabled;
                gammaKeyPressed = true;
            }
            if (input.IsKeyReleased(Keys.B)) 
            {
                gammaKeyPressed = false;
            }

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
