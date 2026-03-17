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
        private Shader redShader;
        private Shader greenShader;
        private Shader blueShader;
        private Shader yellowShader;
        
        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;
        
        // set up vertex data (and buffer(s)) and configure vertex attributes
        // ------------------------------------------------------------------
        float[] cubeVertices = {
            // positions         
            -0.5f, -0.5f, -0.5f, 
            0.5f, -0.5f, -0.5f,  
            0.5f,  0.5f, -0.5f,  
            0.5f,  0.5f, -0.5f,  
            -0.5f,  0.5f, -0.5f, 
            -0.5f, -0.5f, -0.5f, 

            -0.5f, -0.5f,  0.5f, 
            0.5f, -0.5f,  0.5f,  
            0.5f,  0.5f,  0.5f,  
            0.5f,  0.5f,  0.5f,  
            -0.5f,  0.5f,  0.5f, 
            -0.5f, -0.5f,  0.5f, 

            -0.5f,  0.5f,  0.5f, 
            -0.5f,  0.5f, -0.5f, 
            -0.5f, -0.5f, -0.5f, 
            -0.5f, -0.5f, -0.5f, 
            -0.5f, -0.5f,  0.5f, 
            -0.5f,  0.5f,  0.5f, 

            0.5f,  0.5f,  0.5f,  
            0.5f,  0.5f, -0.5f,  
            0.5f, -0.5f, -0.5f,  
            0.5f, -0.5f, -0.5f,  
            0.5f, -0.5f,  0.5f,  
            0.5f,  0.5f,  0.5f,  

            -0.5f, -0.5f, -0.5f, 
            0.5f, -0.5f, -0.5f,  
            0.5f, -0.5f,  0.5f,  
            0.5f, -0.5f,  0.5f,  
            -0.5f, -0.5f,  0.5f, 
            -0.5f, -0.5f, -0.5f, 

            -0.5f,  0.5f, -0.5f, 
            0.5f,  0.5f, -0.5f,  
            0.5f,  0.5f,  0.5f,  
            0.5f,  0.5f,  0.5f,  
            -0.5f,  0.5f,  0.5f, 
            -0.5f,  0.5f, -0.5f, 
        };

        private int cubeVAO;
        private int cubeVBO;

        private int uboMatrices;

        private Matrix4 projection;

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
            cubeVAO = GL.GenVertexArray();
            cubeVBO = GL.GenBuffer();
            GL.BindVertexArray(cubeVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, cubeVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * cubeVertices.Length, cubeVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, 0);
            
            // Shaders
            blueShader = new Shader("Shaders/shader.vert",  "Shaders/blue.frag");
            greenShader = new Shader("Shaders/shader.vert",  "Shaders/green.frag");
            redShader = new Shader("Shaders/shader.vert",  "Shaders/red.frag");
            yellowShader = new Shader("Shaders/shader.vert",  "Shaders/yellow.frag");
            
            // Uniform Buffer Object
            // Get relevant block indices
            var uniformBlockIndexBlue = GL.GetUniformBlockIndex(blueShader.Handle, "Matrices");
            var uniformBlockIndexGreen = GL.GetUniformBlockIndex(greenShader.Handle, "Matrices");
            var uniformBlockIndexRed = GL.GetUniformBlockIndex(redShader.Handle, "Matrices");
            var uniformBlockIndexYellow = GL.GetUniformBlockIndex(yellowShader.Handle, "Matrices");
            // link each shader's uniform block to this uniform binding point
            GL.UniformBlockBinding(blueShader.Handle, uniformBlockIndexBlue, 0);
            GL.UniformBlockBinding(greenShader.Handle, uniformBlockIndexGreen, 0);
            GL.UniformBlockBinding(redShader.Handle, uniformBlockIndexRed, 0);
            GL.UniformBlockBinding(yellowShader.Handle, uniformBlockIndexYellow, 0);
            // Now actually create the buffer
            uboMatrices = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.UniformBuffer, uboMatrices);
            GL.BufferData(BufferTarget.UniformBuffer, 2 * 16 * sizeof(float), 0, BufferUsageHint.StaticDraw); // 16 * sizeof(float) <=> sizeof(Matrix4)
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
            // define the range of the buffer that links to a uniform binding point
            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, 0, uboMatrices, 0, 2 * 16 * sizeof(float));
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
            
            // store the projection matrix (we only do this once now) (note: we're not using zoom anymore by changing the FoV)
            projection =  _camera.GetProjectionMatrix();
            GL.BindBuffer(BufferTarget.UniformBuffer, uboMatrices);
            GL.BufferSubData(BufferTarget.UniformBuffer, 0, 16 * sizeof(float), ref projection);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // draw scene as usual
            var view = _camera.GetViewMatrix();          
            GL.BindBuffer(BufferTarget.UniformBuffer, uboMatrices);
            GL.BufferSubData(BufferTarget.UniformBuffer, 16 * sizeof(float), 16 * sizeof(float), ref view);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
            
            // Draw 4 cubes
            GL.BindVertexArray(cubeVAO);
            
            // RED
            redShader.Use();
            var model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(-0.75f, 0.75f, 0f)) * model; // move top-left
            redShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            // GREEN
            greenShader.Use();
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(0.75f, 0.75f, 0f)) * model; // move top-right
            greenShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            // YELLOW
            yellowShader.Use();
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(-0.75f, -0.75f, 0f)) * model; // move bottom-left
            yellowShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            // BLUE
            blueShader.Use();
            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new Vector3(0.75f, -0.75f, 0f)) * model; // move bottom-right
            blueShader.SetMatrix4("model", model);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            
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
