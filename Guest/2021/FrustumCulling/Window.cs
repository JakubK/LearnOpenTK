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
        private float sensitivity = 0.2f;
        
        private Shader _modelShader;

        private Camera _camera;
        private Camera _cameraSpy;
        private Model _model;
        private Entity _entity;

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


            _modelShader = new Shader("Shaders/model_loading.vs", "Shaders/model_loading.fs");
            
            _camera = new Camera(new Vector3(0, 10, 0), Size.X / (float)Size.Y);
            _cameraSpy = new Camera(new Vector3(0, 10, 0), Size.X / (float)Size.Y);
            
            
            _model = new Model("Resources/planet.obj");
            _entity = new Entity(_model);

            _entity.Transform.SetLocalPosition(Vector3.Zero);
            
            var scale = 1f;
            _entity.Transform.SetLocalScale(new(scale, scale, scale));

            var lastEntity = _entity;
            for (int x = 0; x < 20; x++)
            {
                for (int z = 0; z < 20; z++)
                {
                    _entity.AddChild(_model);
                    lastEntity = _entity.Children.Last();
                    
                    // Set transform values
                    lastEntity.Transform.SetLocalPosition(new (x * 10f - 100f, 0f, z * 10f - 100f));
                }
            }
            _entity.UpdateSelfAndChild();
            

            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _modelShader.Use();
            
            var camFrustum = Frustum.FromCamera(_camera, Size.X / (float)Size.Y,  MathHelper.DegreesToRadians(_camera.Fov), 0.1f, 100f);
            _cameraSpy.ProcessMouseMovement(2, 0, sensitivity);   
            _modelShader.SetMatrix4("view", _camera.GetViewMatrix());
            _modelShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            
            // draw scene graph
            var total = 0;
            var display = 0;

            _entity.DrawSelfAndChild(camFrustum, _modelShader, ref display, ref total);
            Console.WriteLine("Total process in CPU: " + total + " / Total send to GPU: " + display);
            _entity.UpdateSelfAndChild();
            
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
