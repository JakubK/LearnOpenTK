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
        private static readonly Vector3 LightColor = new (0.870588f, 0.721569f, 0.529412f);
        private LtcMatrices _ltcMatrices;
        private Vector3 _areaLightTranslate;
        private Shader _shaderLtc;
        private Shader _shaderLightPlane;

        private Texture _concreteTexture;

        private Camera _camera;

        private bool _firstMove = true;

        private Vector2 _lastPos;
        
        private const float psize = 10.0f;

        private VertexAL[] _planeVertices = new[]
        {
            new VertexAL(new (-psize, 0, -psize), Vector3.UnitY, Vector2.Zero),
            new VertexAL(new (-psize, 0, psize), Vector3.UnitY, Vector2.UnitY),
            new VertexAL(new (psize, 0, psize), Vector3.UnitY, Vector2.One),
            new VertexAL(new (-psize, 0, -psize), Vector3.UnitY, Vector2.Zero),
            new VertexAL(new (psize, 0, psize), Vector3.UnitY, Vector2.One),
            new VertexAL(new (psize, 0, -psize), Vector3.UnitY, Vector2.UnitX),
        };
        
        private VertexAL[] _areaLightVertices = new[]
        {
            new VertexAL(new (-8, 2.4f, -1), Vector3.UnitX, Vector2.Zero), // 0 1 5 4
            new VertexAL(new (-8, 2.4f, 1), Vector3.UnitX, Vector2.UnitY),
            new VertexAL(new (-8, 0.4f, 1), Vector3.UnitX, Vector2.One),
            new VertexAL(new (-8, 2.4f, -1), Vector3.UnitX, Vector2.Zero),
            new VertexAL(new (-8, 0.4f, 1), Vector3.UnitX, Vector2.One),
            new VertexAL(new (-8, 0.4f, -1), Vector3.UnitX, Vector2.UnitX),
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

            _ltcMatrices = new LtcMatrices();
            _ltcMatrices.Mat1 = LoadMTexture();
            _ltcMatrices.Mat2 = LoadLUTTexture();
            
            _shaderLtc = new Shader("Shaders/area_light.vs", "Shaders/area_light.fs");
            _shaderLightPlane = new Shader("Shaders/light_plane.vs", "Shaders/light_plane.fs");

            _concreteTexture = Texture.LoadFromFile("Resources/concreteTexture.png");

            _shaderLtc.Use();
            _shaderLtc.SetVector3("areaLight.points[0]", _areaLightVertices[0].Position);
            _shaderLtc.SetVector3("areaLight.points[1]", _areaLightVertices[1].Position);
            _shaderLtc.SetVector3("areaLight.points[2]", _areaLightVertices[4].Position);
            _shaderLtc.SetVector3("areaLight.points[3]", _areaLightVertices[5].Position);
            _shaderLtc.SetVector3("areaLight.color", LightColor);
            _shaderLtc.SetInt("LTC1", 0);
            _shaderLtc.SetInt("LTC2", 1);
            _shaderLtc.SetInt("material.diffuse", 2);

            IncrementRoughness(0f);
            IncrementLightIntensity(0f);
            SwitchTwoSided(false);

            _shaderLightPlane.Use();
            _shaderLightPlane.SetMatrix4("model", Matrix4.Identity);

            ConfigureMockupData();
            _areaLightTranslate = Vector3.Zero;
            
            _camera = new Camera(new Vector3(0, 1, 0.5f), Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
        }

        private bool twoSided = true;
        private void SwitchTwoSided(bool doSwitch)
        {
            if (doSwitch)
            {
                twoSided = !twoSided;
            }
            
            _shaderLtc.Use();
            _shaderLtc.SetFloat("areaLight.twoSided", twoSided ? 1f : 0f);
            GL.UseProgram(0);
        }

        private float intensity = 4f;
        private void IncrementLightIntensity(float step)
        {
            intensity += step;
            intensity = MathHelper.Clamp(intensity, 0.0f, 10.0f);
            _shaderLtc.Use();
            _shaderLtc.SetFloat("areaLight.intensity", intensity);
            GL.UseProgram(0);
        }

        private float roughness = 0.5f;
        private void IncrementRoughness(float step)
        {
            var color = new Vector3(0.439216f, 0.501961f, 0.564706f);
            roughness += step;
            roughness = MathHelper.Clamp(roughness, 0.0f, 1.0f);
            _shaderLtc.Use();
            _shaderLtc.SetVector4("material.albedoRoughness", new Vector4(color, roughness));
            GL.UseProgram(0);
        }

        private int LoadLUTTexture()
        {
            var texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 64, 64,
                0, PixelFormat.Rgba, PixelType.Float, LtcMatrix.LTC1);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            return texture;
        }

        private int LoadMTexture()
        {
            var texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 64, 64,
                0, PixelFormat.Rgba, PixelType.Float, LtcMatrix.LTC1);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            return texture;
        }

        private int planeVao;
        private int planeVbo;

        private int arealightVao;
        private int arealightVbo;
        
        private void ConfigureMockupData()
        {
            // PLANE
            planeVao = GL.GenVertexArray();
            planeVbo = GL.GenBuffer();

            var vec3SizeInBytes = sizeof(float) * 3;
            var vec2SizeInBytes = sizeof(float) * 2;

            var vertexAlSize = vec3SizeInBytes * 2 + vec2SizeInBytes;
            
            GL.BindVertexArray(planeVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, planeVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _planeVertices.Length * vertexAlSize, _planeVertices, BufferUsageHint.StaticDraw);

            // position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // normal
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (3 * sizeof(float)));
            GL.EnableVertexAttribArray(1);

            // texcoord
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), (6 * sizeof(float)));
            GL.EnableVertexAttribArray(2);
            GL.BindVertexArray(0);

            // AREA LIGHT
            arealightVao = GL.GenVertexArray();
            GL.BindVertexArray(arealightVao);

            arealightVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, arealightVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexAlSize * _areaLightVertices.Length, _areaLightVertices, BufferUsageHint.StaticDraw);

            // position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // normal
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (3 * sizeof(float)));
            GL.EnableVertexAttribArray(1);

            // texcoord
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), (6 * sizeof(float)));
            
            GL.EnableVertexAttribArray(2);
            GL.BindVertexArray(0);

            GL.BindVertexArray(0);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0,0,0,1f);

            _shaderLtc.Use();
            var model = Matrix4.Identity;
            var normal = new Matrix3(model);
            _shaderLtc.SetMatrix4("model", model);
            _shaderLtc.SetMatrix3("normalMatrix", normal);

            var view = _camera.GetViewMatrix();
            var projection = _camera.GetProjectionMatrix();
            
            _shaderLtc.SetMatrix4("projection", projection);
            _shaderLtc.SetMatrix4("view", view);
            _shaderLtc.SetVector3("viewPosition", _camera.Position);
            _shaderLtc.SetVector3("areaLightTranslate", _areaLightTranslate);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _ltcMatrices.Mat1);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, _ltcMatrices.Mat2);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, _concreteTexture.Handle);

            RenderPlane();

            _shaderLightPlane.Use();
            model = Matrix4.CreateTranslation(_areaLightTranslate) * model;
            _shaderLightPlane.SetMatrix4("model", model);
            _shaderLightPlane.SetMatrix4("view", view);
            _shaderLightPlane.SetMatrix4("projection", projection);
            
            RenderAreaLight();
            
            SwapBuffers();
        }

        private void RenderAreaLight()
        {
            GL.BindVertexArray(arealightVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);
        }

        private void RenderPlane()
        {
            GL.BindVertexArray(planeVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);
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

            if (input.IsKeyDown(Keys.R))
            {
                IncrementRoughness(0.01f);
            }
            if (input.IsKeyDown(Keys.T))
            {
                IncrementRoughness(-0.01f);
            }
            
            if (input.IsKeyDown(Keys.I))
            {
                IncrementLightIntensity(0.025f);
            }
            if (input.IsKeyDown(Keys.J))
            {
                IncrementLightIntensity(-0.025f);
            }
            
            if (input.IsKeyDown(Keys.B))
            {
                SwitchTwoSided(true);
            }
            if (input.IsKeyDown(Keys.N))
            {
                SwitchTwoSided(false);
            }

            if (input.IsKeyDown(Keys.Up))
            {
                _areaLightTranslate.Y += 0.01f;
            }
            if (input.IsKeyDown(Keys.Right))
            {
                _areaLightTranslate.Z -= 0.01f;
                
            }
            if (input.IsKeyDown(Keys.Left))
            {
                _areaLightTranslate.Z += 0.01f;
            }
            if (input.IsKeyDown(Keys.Down))
            {
                _areaLightTranslate.Y -= 0.01f;
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
