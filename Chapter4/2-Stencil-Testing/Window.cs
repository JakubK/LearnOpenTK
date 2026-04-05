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
        private Shader _singleColorShader;

        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;
        
        // set up vertex data (and buffer(s)) and configure vertex attributes
        // ------------------------------------------------------------------
        float[] cubeVertices = {
            // positions          // texture Coords
            -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,

            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,

            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,

            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f
        };

        private float[] planeVertices =
        {
            // positions          // texture Coords (note we set these higher than 1 (together with GL_REPEAT as texture wrapping mode). this will cause the floor texture to repeat)
            5.0f, -0.5f, 5.0f, 2.0f, 0.0f,
            -5.0f, -0.5f, 5.0f, 0.0f, 0.0f,
            -5.0f, -0.5f, -5.0f, 0.0f, 2.0f,

            5.0f, -0.5f, 5.0f, 2.0f, 0.0f,
            -5.0f, -0.5f, -5.0f, 0.0f, 2.0f,
            5.0f, -0.5f, -5.0f, 2.0f, 2.0f
        };
        
        private Texture cubeTexture;

        private Texture floorTexture;

        private int cubeVAO;
        private int cubeVBO;

        private int planeVAO;
        private int planeVBO;

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            
            // Configure global OpenGL state
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
            
            GL.Enable(EnableCap.StencilTest);
            GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            
            
            // Cube
            cubeVAO = GL.GenVertexArray();
            cubeVBO = GL.GenBuffer();
            GL.BindVertexArray(cubeVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, cubeVBO);
            GL.BufferData(BufferTarget.ArrayBuffer,  sizeof(float) * cubeVertices.Length, cubeVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3,  VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2,  VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.BindVertexArray(0);

            // Plane
            planeVAO = GL.GenVertexArray();
            planeVBO = GL.GenBuffer();
            GL.BindVertexArray(planeVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, planeVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * planeVertices.Length, planeVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3,  VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2,  VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.BindVertexArray(0);
            
            cubeTexture = Texture.LoadFromFile("Resources/marble.jpg");
            floorTexture = Texture.LoadFromFile("Resources/metal.png");
            
            _modelShader = new Shader("Shaders/shader.vs", "Shaders/shader.fs");
            _singleColorShader = new Shader("Shaders/shader.vs", "Shaders/single_color.fs");
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
            
            _modelShader.Use();
            _modelShader.SetInt("texture1", 0);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            _singleColorShader.Use();
            var model = Matrix4.Identity;
            _singleColorShader.SetMatrix4("view", _camera.GetViewMatrix());
            _singleColorShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            
            _modelShader.Use();
            _modelShader.SetMatrix4("view", _camera.GetViewMatrix());
            _modelShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            
            // Draw floor as normal but dont write the floor to the stencil buffer, we only care about the containers, so 0x00 not to write to stencil buffer
            GL.StencilMask(0x00);
            
            // Floor
            GL.BindVertexArray(planeVAO);
            GL.BindTexture(TextureTarget.Texture2D, floorTexture.Handle);
            _modelShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);
            
            // First render pass, draw objects as normal with writing to Stencil Buffer
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilMask(0xFF);
            
            // Cubes
            GL.BindVertexArray(cubeVAO);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, cubeTexture.Handle);
            
            model = Matrix4.Identity;
            model *= Matrix4.CreateTranslation(new Vector3(-1.0f, 0.0f, -1.0f));
            _modelShader.SetMatrix4("model", model);
            
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(2.0f, 0.0f, 0.0f)) * model;
            _modelShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            
            // Second render pass, now draw slightly bigger version of objects, with disabled stencil writing
            // Stencil buffer is now filled with 1s. Parts with 1s are not drawn, so only difference in size are drawn, making borders
            GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
            GL.StencilMask(0x00);
            GL.Disable(EnableCap.DepthTest);
            _singleColorShader.Use();
            float scale = 1.1f;
            
            // Cubes
            GL.BindVertexArray(cubeVAO);
            GL.BindTexture(TextureTarget.Texture2D, cubeTexture.Handle);
            
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(-1.0f, 0.0f, -1.0f)) * model;
            model = Matrix4.CreateScale(scale) * model;
            
            _singleColorShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(2.0f, 0.0f, 0.0f)) * model;
            model = Matrix4.CreateScale(scale) * model;
            _singleColorShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            
            GL.BindVertexArray(0);
            
            GL.StencilMask(0xFF);
            GL.StencilFunc(StencilFunction.Always, 0, 0xFF);
            GL.Enable(EnableCap.DepthTest);
            
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
