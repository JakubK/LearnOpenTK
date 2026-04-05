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
        private Shader asteroidsShader;
        private Shader planetShader;
        
        private Camera _camera;
        
        private Model planet;
        private Model rock;

        private int amount = 1000;
        
        private bool _firstMove = true;

        private Vector2 _lastPos;

        private List<Matrix4> ModelMatrices = new();
        

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);
            
            // Shader
            asteroidsShader = new Shader("Shaders/asteroids.vs",  "Shaders/asteroids.fs");
            planetShader = new Shader("Shaders/planet.vs",  "Shaders/planet.fs");

            planet = new Model("Resources/planet/planet.obj");
            rock = new Model("Resources/rock/rock.obj");


            var radius = 50.0;
            var offset = 2.5f;

            for (int i = 0; i < amount; i++)
            {
                var model = Matrix4.Identity;
                
                // translation: displace along circle with radius in range [-offset, offset]
                
                var angle = i / (float)amount * 360.0f;
                var displacement = (new Random().Next() % (int)(2 * offset * 100)) / 100.0f - offset;
                var x = (float)(Math.Sin(angle) * radius + displacement);
                displacement = (new Random().Next() % (int)(2 * offset * 100)) / 100.0f - offset;
                var y = displacement * 0.4f; // keep height of asteroid field smaller compared to width of x and z
                displacement = (new Random().Next() % (int)(2 * offset * 100)) / 100.0f - offset;
                var z = (float)(Math.Cos(angle) * radius + displacement);
    
                model = Matrix4.CreateTranslation(new Vector3(x, y, z)) * model;
                
                // scale: Scale between 0.05 and 0.25f
                var scale = (float)(new Random().Next() % 20 / 100.0 + 0.05);
                model = Matrix4.CreateScale(scale) * model;
                
                // rotation: Add random rotation around a semi-randomly picked rotation axis vector
                var rotAngle = new Random().Next() % 360;
                model = Matrix4.CreateFromAxisAngle(new Vector3(0.4f, 0.6f, 0.8f), rotAngle) * model;
                
                ModelMatrices.Add(model);
            }

            var buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
            var data = ModelMatrices.ToArray();
            GL.BufferData(BufferTarget.ArrayBuffer, amount * 16 * sizeof(float), data, BufferUsageHint.StaticDraw);
            
            // set transformation matrices as an instance matrix vertex attribute (with divisor 1)
            // note: we're cheating a little by taking the, now publicly declared, VAO of the model's mesh(es) and adding new vertexAttribPointers
            // normally you'd want to do this in a more organized fashion, but for learning purposes this will do.
            var meshes = rock.GetMeshes();
            // var mat4Size = sizeof(float) * 16;
            int vec4Size = 4 * sizeof(float);
            int mat4Size = 16 * sizeof(float);

            
            
            for (int i = 0; i < meshes.Count; i++)
            {
                var vao = meshes[i].GetVao();
                GL.BindVertexArray(vao);
                
                // set attribute pointers to matrix (4 times vec4)
                GL.EnableVertexAttribArray(3);
                GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, mat4Size, 0);
                GL.EnableVertexAttribArray(4);
                GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, mat4Size, vec4Size);
                GL.EnableVertexAttribArray(5);
                GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, mat4Size, 2 * vec4Size);
                GL.EnableVertexAttribArray(6);
                GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, mat4Size, 3 * vec4Size);            
                
                GL.VertexAttribDivisor(3, 1);
                GL.VertexAttribDivisor(4, 1);
                GL.VertexAttribDivisor(5, 1);
                GL.VertexAttribDivisor(6, 1);
                
                GL.BindVertexArray(0);
            }
            
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var projection = _camera.GetProjectionMatrix();
            var view  = _camera.GetViewMatrix();
            
            planetShader.Use();
            planetShader.SetMatrix4("projection", projection);
            planetShader.SetMatrix4("view", view);
            
            // Draw a planet
            var model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(0.0f, -3.0f, 0.0f)) * model;
            model = Matrix4.CreateScale(new Vector3(4.0f, 4.0f, 4.0f)) * model;
            planetShader.SetMatrix4("model", model);
            planet.Draw(planetShader);
            
            // Draw meteorites
            asteroidsShader.Use();
            asteroidsShader.SetInt("texture_diffuse1", 0);
            asteroidsShader.SetMatrix4("projection", projection);
            asteroidsShader.SetMatrix4("view", view);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, rock.GetTexturesLoaded()[0].Id);
            
            var rockMeshes = rock.GetMeshes();
            for (int i = 0; i < rockMeshes.Count; i++)
            {
                GL.BindVertexArray(rockMeshes[i].GetVao());
                GL.DrawElementsInstanced(PrimitiveType.Triangles, rockMeshes[i].Indices.Count, DrawElementsType.UnsignedInt, 0, amount);
                GL.BindVertexArray(0);
            }
            
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
