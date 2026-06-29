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
        private Vector3 lightDir = new (20.0f, 50, 20.0f);
        private int debugLayer;
        private int matrix4SizeInBytes = 4 * 4 * 4;
        private List<Matrix4> lightMatricesCache = new();
        private bool showQuad = false;
        
        private Shader _shader;
        private Shader _simpleDepthShader;
        private Shader _debugDepthQuad;
        private Shader _debugCascadeShader;

        private Texture _woodTexture;
        
        private float[] planeVertices = {
            // positions            // normals         // texcoords
            25.0f, -2f,  25.0f,  0.0f, 1.0f, 0.0f,  25.0f,  0.0f,
            -25.0f, -2f,  25.0f,  0.0f, 1.0f, 0.0f,   0.0f,  0.0f,
            -25.0f, -2f, -25.0f,  0.0f, 1.0f, 0.0f,   0.0f, 25.0f,

            25.0f, -2f,  25.0f,  0.0f, 1.0f, 0.0f,  25.0f,  0.0f,
            -25.0f, -2f, -25.0f,  0.0f, 1.0f, 0.0f,   0.0f, 25.0f,
            25.0f, -2f, -25.0f,  0.0f, 1.0f, 0.0f, 25.0f, 25.0f
        };

        private float cameraFarPlane = 1000f;
        private List<float> shadowCascadeLevels = new ();

        private int lightFbo;
        private int lightDepthMaps;

        private int planeVao;
        private int planeVbo;

        private int matricesUbo;
        
        private int depthMapResolution = 4096;

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
            
            lightDir = lightDir.Normalized();
            
            _shader = new Shader("Shaders/shadow_mapping.vs", "Shaders/shadow_mapping.fs");
            _simpleDepthShader = new Shader("Shaders/shadow_mapping_depth.vs", "Shaders/shadow_mapping_depth.fs", "Shaders/shadow_mapping_depth.gs");
            _debugDepthQuad = new Shader("Shaders/debug_quad.vs", "Shaders/debug_quad.fs");
            _debugCascadeShader = new Shader("Shaders/debug_cascade.vs", "Shaders/debug_cascade.fs");

            shadowCascadeLevels.Add(cameraFarPlane / 50.0f);
            shadowCascadeLevels.Add(cameraFarPlane / 25.0f);
            shadowCascadeLevels.Add(cameraFarPlane / 10.0f);
            shadowCascadeLevels.Add(cameraFarPlane / 2.0f);
            
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            
            // load texture
            _woodTexture = Texture.LoadFromFile("Resources/wood.png");
            
            // configure light fbo
            lightFbo = GL.GenFramebuffer();
            lightDepthMaps = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, lightDepthMaps);
            GL.TexImage3D(
                TextureTarget.Texture2DArray,
                0,
                PixelInternalFormat.DepthComponent32f,
                depthMapResolution,
                depthMapResolution,
                shadowCascadeLevels.Count + 1,
                0,
                PixelFormat.DepthComponent,
                PixelType.Float, 0    
            );
            
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            
            float[] bordercolor = { 1.0f, 1.0f, 1.0f, 1.0f };
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureBorderColor, bordercolor);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, lightFbo);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, lightDepthMaps, 0);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Error:Framebuffer:: Framebuffer not complete");
                throw new Exception();
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // configure ubo
            matricesUbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.UniformBuffer, matricesUbo);
            GL.BufferData(BufferTarget.UniformBuffer, matrix4SizeInBytes * 16, 0, BufferUsageHint.StaticDraw);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, matricesUbo);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
            
            planeVao = GL.GenVertexArray();
            planeVbo = GL.GenBuffer();
            GL.BindVertexArray(planeVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, planeVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, planeVertices.Length * sizeof(float), planeVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), (3 * sizeof(float)));
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), (6 * sizeof(float)));
            GL.BindVertexArray(0);

            _shader.Use();
            _shader.SetInt("diffuseTexture", 0);
            _shader.SetInt("shadowMap", 1);

            _debugDepthQuad.Use();
            _debugDepthQuad.SetInt("depthMap", 0);
            
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            // 0. Ubo setup
            var lightMatrices = GetLightSpaceMatrices();
            GL.BindBuffer(BufferTarget.UniformBuffer, matricesUbo);
            for (int i = 0; i < lightMatrices.Count; ++i)
            {
                var lightMatrix = lightMatrices[i];
                GL.BufferSubData(BufferTarget.UniformBuffer, i * matrix4SizeInBytes, matrix4SizeInBytes, ref lightMatrix);
            }
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
            
            // 1. render depth of scene to texture (from light's perspective)
            _simpleDepthShader.Use();
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, lightFbo);
            GL.Viewport(0, 0, depthMapResolution, depthMapResolution);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.CullFace(CullFaceMode.Front);  // peter panning
            RenderScene(_simpleDepthShader);
            GL.CullFace(CullFaceMode.Back);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // reset viewport
            GL.Viewport(0, 0, Size.X, Size.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            
            // 2. render scene as normal using the generated depth/shadow map  
            GL.Viewport(0, 0, Size.X, Size.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _shader.Use();
            
            _shader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _shader.SetMatrix4("view", _camera.GetViewMatrix());
            // set light uniforms
            _shader.SetVector3("viewPos", _camera.Position);
            _shader.SetVector3("lightDir", lightDir);
            _shader.SetFloat("farPlane", cameraFarPlane);
            _shader.SetInt("cascadeCount", shadowCascadeLevels.Count);
            for (int i = 0; i < shadowCascadeLevels.Count; ++i)
            {
                _shader.SetFloat("cascadePlaneDistances[" + i + "]", shadowCascadeLevels[i]);
            }
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _woodTexture.Handle);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2DArray, lightDepthMaps);
            RenderScene(_shader);

            if (lightMatricesCache.Count != 0)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                _debugCascadeShader.Use();
                _debugCascadeShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
                _debugCascadeShader.SetMatrix4("view", _camera.GetViewMatrix());
                DrawCascadeVolumeVisualizers(lightMatricesCache, _debugCascadeShader);
                GL.Disable(EnableCap.Blend);
            }
            
            // render Depth map to quad for visual debugging
            _debugDepthQuad.Use();
            _debugDepthQuad.SetInt("layer", debugLayer);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2DArray, lightDepthMaps);
            if (showQuad)
            {
                RenderQuad();
            }

            SwapBuffers();
        }

        private List<Matrix4> GetLightSpaceMatrices()
        {
            var result = new List<Matrix4>();
            for (int i = 0; i < shadowCascadeLevels.Count + 1; ++i)
            {
                if (i == 0)
                {
                    result.Add(GetLightSpaceMatrix(_camera.DepthNear, shadowCascadeLevels[i]));
                }
                else if (i < shadowCascadeLevels.Count)
                {
                    result.Add(GetLightSpaceMatrix(shadowCascadeLevels[i - 1], shadowCascadeLevels[i]));
                }
                else
                {
                    result.Add(GetLightSpaceMatrix(shadowCascadeLevels[i - 1], cameraFarPlane));
                }
            }
            return result;
        }

        private Matrix4 GetLightSpaceMatrix(float nearPlane, float farPlane)
        {
            var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(_camera.Fov), (float)Size.X / Size.Y, nearPlane, farPlane);
            var corners = GetFrustumCornersWorldSpace(proj, _camera.GetViewMatrix());

            var center = Vector3.Zero;
            foreach (var v in corners)
            {
                center += v.Xyz;
            }
            center /= corners.Count;
            
            var lightView = Matrix4.LookAt(center + lightDir, center, Vector3.UnitY);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            
            foreach (var v in corners)
            {
                var trf = lightView * v;
                minX = MathF.Min(minX, trf.X);
                maxX = MathF.Max(maxX, trf.X);
                minY = MathF.Min(minY, trf.Y);
                maxY = MathF.Max(maxY, trf.Y);
                minZ = MathF.Min(minZ, trf.Z);
                maxZ = MathF.Max(maxZ, trf.Z);
            }

            // Tune this parameter according to the scene
            var zMult = 10.0f;
            if (minZ < 0)
            {
                minZ *= zMult;
            }
            else
            {
                minZ /= zMult;
            }
            if (maxZ < 0)
            {
                maxZ /= zMult;
            }
            else
            {
                maxZ *= zMult;
            }

            var lightProjection = Matrix4.CreateOrthographicOffCenter(minX, maxX, minY, maxY, minZ, maxZ);
            return lightProjection * lightView;
        }

        private List<Vector4> GetFrustumCornersWorldSpace(Matrix4 proj, Matrix4 view)
        {
            return GetFrustumCornersWorldSpace(proj * view);
        }
        
        private List<Vector4> GetFrustumCornersWorldSpace(Matrix4 projView)
        {
            var inv = projView.Inverted();

            var frustumCorners = new List<Vector4>();
            for (int x = 0; x < 2; ++x)
            {
                for (int y = 0; y < 2; ++y)
                {
                    for (int z = 0; z < 2; ++z)
                    {
                        var pt = inv * new Vector4(2.0f * x - 1.0f, 2.0f * y - 1.0f, 2.0f * z - 1.0f, 1.0f);
                        frustumCorners.Add(pt / pt.W);
                    }
                }
            }

            return frustumCorners;
        }


        private void DrawCascadeVolumeVisualizers(List<Matrix4> lightMatrices, Shader shader)
        {
            int[] indices = {
                0, 2, 3,
                0, 3, 1,
                4, 6, 2,
                4, 2, 0,
                5, 7, 6,
                5, 6, 4,
                1, 3, 7,
                1, 7, 5,
                6, 7, 3,
                6, 3, 2,
                1, 5, 4,
                0, 1, 4
            };

            Vector4[] colors =
            {
                new ( 1, 0, 0, 0.5f ),
                new ( 0, 1, 0, 0.5f ),
                new ( 0, 0, 1, 0.5f ),
            };

            for (int i = 0; i < lightMatrices.Count; ++i)
            {
                var corners = GetFrustumCornersWorldSpace(lightMatrices[i]);
                var vec3s = new List<Vector3>();

                foreach (var corner in corners)
                {
                    vec3s.Add(corner.Xyz);
                }

                var vao = GL.GenVertexArray();
                var vbo = GL.GenBuffer();
                var ebo = GL.GenBuffer();
                
                GL.BindVertexArray(vao);

                var vector3Size = sizeof(float) * 3;

                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, vec3s.Count * vector3Size, vec3s.ToArray(), BufferUsageHint.StaticDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer, 36 * sizeof(int), indices, BufferUsageHint.StaticDraw);

                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vector3Size, 0);

                GL.BindVertexArray(vao);
                shader.SetVector4("color", colors[i % 3]);
                GL.DrawElements(BeginMode.Triangles, 36, DrawElementsType.UnsignedInt, 0);

                GL.DeleteBuffer(vbo);
                GL.DeleteVertexArray(vao);
                GL.DeleteBuffer(ebo);

                GL.BindVertexArray(0);
            }
        }

        private List<Matrix4> modelMatrices = new();
        private void RenderScene(Shader shader)
        {
            // floor
            var model = Matrix4.Identity;
            shader.SetMatrix4("model", model);
            
            GL.BindVertexArray(planeVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            var r = new Random();
            var maxOffset = 10;
            var minOffset = -10;
            var minScale = 1;
            var maxScale = 2;
            var minRotation = 0;
            var maxRotation = 180;
            if (modelMatrices.Count == 0)
            {
                for (int i = 0; i < 10; ++i)
                {
                    var offsetX = GetRandom(r, minOffset, maxOffset);
                    var offsetY = GetRandom(r, minOffset, maxOffset);
                    var offsetZ = GetRandom(r, minOffset, maxOffset);
                    
                    var scale = GetRandom(r, minScale, maxScale);
                    var rotation = GetRandom(r, minRotation, maxRotation);
                    
                    model = Matrix4.Identity;
                    model = Matrix4.CreateTranslation(offsetX, offsetY + 10.0f, offsetZ) * model;
                    model = Matrix4.CreateFromAxisAngle(new Vector3(1,0,1).Normalized(),MathHelper.DegreesToRadians(rotation)) * model;
                    model = Matrix4.CreateScale(scale) * model;
                    modelMatrices.Add(model);
                }
            }

            foreach (var m in modelMatrices)
            {
                shader.SetMatrix4("model", m);
                RenderCube();
            }
        }

        private float GetRandom(Random r, int min, int max)
        {
            return (float)r.NextDouble() * (max - min) + min;
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

            if (input.IsKeyDown(Keys.F))
            {
                showQuad = !showQuad;
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
        
        private int cubeVao = 0;
        private int cubeVbo = 0;
        
        private void RenderCube()
        {
            if (cubeVao == 0)
            {
                float[] vertices =
                {
                    // back face
                    -1.0f, -1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f, // bottom-left
                    1.0f, 1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f, // top-right
                    1.0f, -1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, // bottom-right         
                    1.0f, 1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f, // top-right
                    -1.0f, -1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f, // bottom-left
                    -1.0f, 1.0f, -1.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f, // top-left
                    // front face
                    -1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, // bottom-left
                    1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f, // bottom-right
                    1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f, // top-right
                    1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f, // top-right
                    -1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, // top-left
                    -1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, // bottom-left
                    // left face
                    -1.0f, 1.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f, // top-right
                    -1.0f, 1.0f, -1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f, // top-left
                    -1.0f, -1.0f, -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, // bottom-left
                    -1.0f, -1.0f, -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f, // bottom-left
                    -1.0f, -1.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, // bottom-right
                    -1.0f, 1.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f, // top-right
                    // right face
                    1.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, // top-left
                    1.0f, -1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, // bottom-right
                    1.0f, 1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f, // top-right         
                    1.0f, -1.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f, // bottom-right
                    1.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, // top-left
                    1.0f, -1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, // bottom-left     
                    // bottom face
                    -1.0f, -1.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, // top-right
                    1.0f, -1.0f, -1.0f, 0.0f, -1.0f, 0.0f, 1.0f, 1.0f, // top-left
                    1.0f, -1.0f, 1.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f, // bottom-left
                    1.0f, -1.0f, 1.0f, 0.0f, -1.0f, 0.0f, 1.0f, 0.0f, // bottom-left
                    -1.0f, -1.0f, 1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, // bottom-right
                    -1.0f, -1.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, // top-right
                    // top face
                    -1.0f, 1.0f, -1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, // top-left
                    1.0f, 1.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, // bottom-right
                    1.0f, 1.0f, -1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, // top-right     
                    1.0f, 1.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, // bottom-right
                    -1.0f, 1.0f, -1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, // top-left
                    -1.0f, 1.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f // bottom-left        
                };
                
                cubeVao = GL.GenVertexArray();
                cubeVbo = GL.GenBuffer();
                
                // fill buffer
                GL.BindBuffer(BufferTarget.ArrayBuffer, cubeVbo);
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertices.Length, vertices, BufferUsageHint.StaticDraw);
                
                // link vertex attributes
                GL.BindVertexArray(cubeVao);
                
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
            
                GL.EnableVertexAttribArray(2);
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
                
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);
            }
            
            // Render the cube
            GL.BindVertexArray(cubeVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            GL.BindVertexArray(0);
        }

        private int quadVao = 0;
        private int quadVbo = 0;
        
        private void RenderQuad()
        {
            if (quadVao == 0)
            {
                float[] quadVertices = {
                    // positions        // texture Coords
                    -1.0f,  1.0f, 0.0f, 0.0f, 1.0f,
                    -1.0f, -1.0f, 0.0f, 0.0f, 0.0f,
                    1.0f,  1.0f, 0.0f, 1.0f, 1.0f,
                    1.0f, -1.0f, 0.0f, 1.0f, 0.0f,
                };
                // setup plane VAO
                quadVao = GL.GenVertexArray();
                quadVbo = GL.GenBuffer();
                GL.BindVertexArray(quadVao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, quadVbo);
                GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * quadVertices.Length, quadVertices, BufferUsageHint.StaticDraw);
                
                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
                GL.EnableVertexAttribArray(1);
                GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (3 * sizeof(float)));
            }
            
            // Render the cube
            GL.BindVertexArray(quadVao);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            GL.BindVertexArray(0);
        }
    }
}
