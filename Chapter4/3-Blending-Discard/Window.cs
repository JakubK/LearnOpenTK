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
        private Shader _modelShader;

        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;
        
        private Texture cubeTexture;
        private Texture floorTexture;
        private Texture transparentTexture;

        private int cubeVao;
        private int cubeVbo;

        private int planeVao;
        private int planeVbo;

        private int transparentVao;
        private int transparentVbo;

        // set up vertex data (and buffer(s)) and configure vertex attributes
        // ------------------------------------------------------------------
        float[] cubeVertices =
        {
            // positions          // texture Coords
            -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
            0.5f, -0.5f, -0.5f, 1.0f, 0.0f,
            0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
            0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
            -0.5f, 0.5f, -0.5f, 0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,

            -0.5f, -0.5f, 0.5f, 0.0f, 0.0f,
            0.5f, -0.5f, 0.5f, 1.0f, 0.0f,
            0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
            0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
            -0.5f, 0.5f, 0.5f, 0.0f, 1.0f,
            -0.5f, -0.5f, 0.5f, 0.0f, 0.0f,

            -0.5f, 0.5f, 0.5f, 1.0f, 0.0f,
            -0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f, 0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f, 0.0f, 1.0f,
            -0.5f, -0.5f, 0.5f, 0.0f, 0.0f,
            -0.5f, 0.5f, 0.5f, 1.0f, 0.0f,

            0.5f, 0.5f, 0.5f, 1.0f, 0.0f,
            0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
            0.5f, -0.5f, -0.5f, 0.0f, 1.0f,
            0.5f, -0.5f, -0.5f, 0.0f, 1.0f,
            0.5f, -0.5f, 0.5f, 0.0f, 0.0f,
            0.5f, 0.5f, 0.5f, 1.0f, 0.0f,

            -0.5f, -0.5f, -0.5f, 0.0f, 1.0f,
            0.5f, -0.5f, -0.5f, 1.0f, 1.0f,
            0.5f, -0.5f, 0.5f, 1.0f, 0.0f,
            0.5f, -0.5f, 0.5f, 1.0f, 0.0f,
            -0.5f, -0.5f, 0.5f, 0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f, 0.0f, 1.0f,

            -0.5f, 0.5f, -0.5f, 0.0f, 1.0f,
            0.5f, 0.5f, -0.5f, 1.0f, 1.0f,
            0.5f, 0.5f, 0.5f, 1.0f, 0.0f,
            0.5f, 0.5f, 0.5f, 1.0f, 0.0f,
            -0.5f, 0.5f, 0.5f, 0.0f, 0.0f,
            -0.5f, 0.5f, -0.5f, 0.0f, 1.0f
        };

        float[] planeVertices =
        {
            // positions          // texture Coords 
            5.0f, -0.5f, 5.0f, 0.0f, 0.0f,
            -5.0f, -0.5f, 5.0f, 2.0f, 0.0f,
            -5.0f, -0.5f, -5.0f, 2.0f, 2.0f,

            5.0f, -0.5f, 5.0f, 2.0f, 2.0f,
            -5.0f, -0.5f, -5.0f, 0.0f, 2.0f,
            5.0f, -0.5f, -5.0f, 0.0f, 0.0f
        };

        float[] transparentVertices = {
            // positions         // texture Coords
            0.0f,  0.5f,  0.0f,  0.0f,  1.0f,
            0.0f, -0.5f,  0.0f,  0.0f,  0.0f,
            1.0f, -0.5f,  0.0f,  1.0f,  0.0f,

            0.0f,  0.5f,  0.0f,  0.0f,  1.0f,
            1.0f, -0.5f,  0.0f,  1.0f,  0.0f,
            1.0f,  0.5f,  0.0f,  1.0f,  1.0f
        };

        private List<Vector3> vegetation = new()
        {
            new(-1.5f, 0.0f, -0.48f),
            new(1.5f, 0.0f, 0.51f),
            new(0.0f, 0.0f, 0.7f),
            new(-0.3f, 0.0f, -2.3f),
            new(0.5f, 0.0f, -0.6f),
        };

    public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);
            
            // Cube VAO
            cubeVao = GL.GenVertexArray();
            cubeVbo = GL.GenBuffer();
            
            GL.BindVertexArray(cubeVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, cubeVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * cubeVertices.Length ,cubeVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            
            // Plane VAO
            planeVao = GL.GenVertexArray();
            planeVbo = GL.GenBuffer();
            
            GL.BindVertexArray(planeVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, planeVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * planeVertices.Length, planeVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            
            // Transparent VAO
            transparentVao  = GL.GenVertexArray();
            transparentVbo = GL.GenBuffer();
            
            GL.BindVertexArray(transparentVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, transparentVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * transparentVertices.Length, transparentVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            GL.BindVertexArray(0);
            
            cubeTexture = Texture.LoadFromFile("Resources/marble.jpg");
            floorTexture = Texture.LoadFromFile("Resources/metal.png");
            transparentTexture = Texture.LoadFromFile("Resources/grass.png", TextureWrapMode.ClampToEdge);
            
            _modelShader = new Shader("Shaders/shader.vs", "Shaders/shader.fs");
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
            
            _modelShader.Use();
            _modelShader.SetInt("texture1", 0);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _modelShader.Use();

            _modelShader.SetMatrix4("view", _camera.GetViewMatrix());
            _modelShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            var model = Matrix4.Identity;
            
            // Cubes
            GL.BindVertexArray(cubeVao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, cubeTexture.Handle);
            
            model = Matrix4.CreateTranslation(new Vector3(-1.0f, 0.0f, -1.0f));
            _modelShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            model = Matrix4.Identity;
            model *= Matrix4.CreateTranslation(new Vector3(2.0f, 0.0f, 0.0f));
            _modelShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            
            // Floor
            GL.BindVertexArray(planeVao);
            GL.BindTexture(TextureTarget.Texture2D, floorTexture.Handle);
            model = Matrix4.Identity;
            _modelShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            
            // Vegetation
            GL.BindVertexArray(transparentVao);
            GL.BindTexture(TextureTarget.Texture2D, transparentTexture.Handle);
            foreach (var t in vegetation)
            {
                model = Matrix4.Identity;
                model = Matrix4.CreateTranslation(t) * model;
                _modelShader.SetMatrix4("model", model);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
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
