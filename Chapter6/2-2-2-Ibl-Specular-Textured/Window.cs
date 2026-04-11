using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using StbImageSharp;

namespace LearnOpenTK
{
    public class Window : GameWindow
    {
        private int brdfLUTTexture;
        private int prefilterMap;
        private int irradianceMap;
        
        private int envCubeMap;
        
        private int hdrTexture;
        
        private int captureFbo;
        private int captureRbo;
        
        private int nrRows = 7;
        private int nrColumns = 7;
        private float spacing = 2.5f;
        
        private List<Vector3> LightPositions = new();
        private List<Vector3> LightColors = new();

        private Shader pbrShader;
        private Shader equirectangularToCubemapShader;
        private Shader irradianceShader;
        private Shader prefilterShader;
        private Shader brdfShader;
        private Shader backgroundShader;

        private Texture ironAlbedoMap;
        private Texture ironNormalMap;
        private Texture ironMetallicMap;
        private Texture ironRoughnessMap;
        private Texture ironAOMap;

        private Texture goldAlbedoMap;
        private Texture goldNormalMap;
        private Texture goldMetallicMap;
        private Texture goldRoughnessMap;
        private Texture goldAOMap;

        private Texture grassAlbedoMap;
        private Texture grassNormalMap;
        private Texture grassMetallicMap;
        private Texture grassRoughnessMap;
        private Texture grassAOMap;

        private Texture plasticAlbedoMap;
        private Texture plasticNormalMap;
        private Texture plasticMetallicMap;
        private Texture plasticRoughnessMap;
        private Texture plasticAOMap;

        private Texture wallAlbedoMap;
        private Texture wallNormalMap;
        private Texture wallMetallicMap;
        private Texture wallRoughnessMap;
        private Texture wallAOMap;
        
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
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal); // set depth function to less than AND equal for skybox depth trick.
            GL.Enable(EnableCap.TextureCubeMapSeamless);
            
            // load pbr material textures
            
            // rusted iron
            ironAlbedoMap = Texture.LoadFromFile("Resources/rusted_iron/albedo.png");
            ironNormalMap = Texture.LoadFromFile("Resources/rusted_iron/normal.png");
            ironMetallicMap = Texture.LoadFromFile("Resources/rusted_iron/metallic.png");
            ironRoughnessMap = Texture.LoadFromFile("Resources/rusted_iron/roughness.png");
            ironAOMap = Texture.LoadFromFile("Resources/rusted_iron/ao.png");

            // gold
            goldAlbedoMap = Texture.LoadFromFile("Resources/gold/albedo.png");
            goldNormalMap = Texture.LoadFromFile("Resources/gold/normal.png");
            goldMetallicMap = Texture.LoadFromFile("Resources/gold/metallic.png");
            goldRoughnessMap = Texture.LoadFromFile("Resources/gold/roughness.png");
            goldAOMap = Texture.LoadFromFile("Resources/gold/ao.png");

            // grass
            grassAlbedoMap = Texture.LoadFromFile("Resources/grass/albedo.png");
            grassNormalMap = Texture.LoadFromFile("Resources/grass/normal.png");
            grassMetallicMap = Texture.LoadFromFile("Resources/grass/metallic.png");
            grassRoughnessMap = Texture.LoadFromFile("Resources/grass/roughness.png");
            grassAOMap = Texture.LoadFromFile("Resources/grass/ao.png");

            // plastic
            plasticAlbedoMap = Texture.LoadFromFile("Resources/plastic/albedo.png");
            plasticNormalMap = Texture.LoadFromFile("Resources/plastic/normal.png");
            plasticMetallicMap = Texture.LoadFromFile("Resources/plastic/metallic.png");
            plasticRoughnessMap = Texture.LoadFromFile("Resources/plastic/roughness.png");
            plasticAOMap = Texture.LoadFromFile("Resources/plastic/ao.png");

            // wall
            wallAlbedoMap = Texture.LoadFromFile("Resources/wall/albedo.png");
            wallNormalMap = Texture.LoadFromFile("Resources/wall/normal.png");
            wallMetallicMap = Texture.LoadFromFile("Resources/wall/metallic.png");
            wallRoughnessMap = Texture.LoadFromFile("Resources/wall/roughness.png");
            wallAOMap = Texture.LoadFromFile("Resources/wall/ao.png");

            pbrShader = new Shader("Shaders/pbr.vs", "Shaders/pbr.fs");
            pbrShader.Use();
            pbrShader.SetInt("irradianceMap", 0);
            pbrShader.SetInt("prefilterMap", 1);
            pbrShader.SetInt("brdfLUT", 2);
            pbrShader.SetInt("albedoMap", 3);
            pbrShader.SetInt("normalMap", 4);
            pbrShader.SetInt("metallicMap", 5);
            pbrShader.SetInt("roughnessMap", 6);
            pbrShader.SetInt("aoMap", 7);

            backgroundShader = new Shader("Shaders/background.vs", "Shaders/background.fs");
            backgroundShader.Use();
            backgroundShader.SetInt("environmentMap", 0);
            
            LightPositions.Add(new(-10, 10, 10));
            LightPositions.Add(new(10, 10, 10));
            LightPositions.Add(new(-10, -10, 10));
            LightPositions.Add(new(-10, -10, 10));
            
            LightColors.Add(new(300, 300, 300));
            LightColors.Add(new(300, 300, 300));
            LightColors.Add(new(300, 300, 300));
            LightColors.Add(new(300, 300, 300));
            
            // pbr: setup framebuffer
            captureFbo = GL.GenFramebuffer();
            captureRbo = GL.GenRenderbuffer();
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, 512, 512);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, captureRbo);
            
            // pbr: load the HDR environment map
            StbImage.stbi_set_flip_vertically_on_load(1);
            using (Stream stream = File.OpenRead("Resources/newport_loft.hdr"))
            {
                var image = ImageResultFloat.FromStream(stream, ColorComponents.RedGreenBlue);
                hdrTexture = GL.GenTexture();
                
                GL.BindTexture(TextureTarget.Texture2D, hdrTexture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, image.Width, image.Height, 0, PixelFormat.Rgb, PixelType.Float, image.Data); // note how we specify the texture's data value to be float
                
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            }
            
            // pbr: setup cubemap to render to and attach to framebuffer
            envCubeMap = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, envCubeMap);

            for (int i = 0; i < 6; i++)
            {
                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgb16f, 512, 512, 0, PixelFormat.Rgb, PixelType.Float, 0);
            }
                
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); // enable pre-filter mipmap sampling (combatting visible dots artifact)
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            
            // pbr: set up projection and view matrices for capturing data onto the 6 cubemap face directions
            var captureProjection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), 1f, 0.1f, 10.0f);
            var captureViews = new List<Matrix4>();
            captureViews.Add(Matrix4.LookAt(Vector3.Zero, new (1, 0, 0), new (0, -1, 0)));
            captureViews.Add(Matrix4.LookAt(Vector3.Zero, new (-1, 0, 0), new (0, -1, 0)));
            captureViews.Add(Matrix4.LookAt(Vector3.Zero, new (0, 1, 0), new (0, 0, 1)));
            captureViews.Add(Matrix4.LookAt(Vector3.Zero, new (0, -1, 0), new (0, 0, -1)));
            captureViews.Add(Matrix4.LookAt(Vector3.Zero, new (0, 0, 1), new (0, -1, 0)));
            captureViews.Add(Matrix4.LookAt(Vector3.Zero, new (0, 0, -1), new (0, -1, 0)));
            
            // pbr: convert HDR equirectangular environment map to cubemap equivalent
            equirectangularToCubemapShader = new Shader("Shaders/cubemap.vs", "Shaders/equirectangular_to_cubemap.fs");
            equirectangularToCubemapShader.Use();
            equirectangularToCubemapShader.SetInt("equirectangularMap", 0);
            equirectangularToCubemapShader.SetMatrix4("projection", captureProjection);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, hdrTexture);
            
            GL.Viewport(0, 0, 512, 512); // don't forget to configure the viewport to the capture dimensions.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
            for (int i = 0; i < 6; ++i)
            {
                equirectangularToCubemapShader.SetMatrix4("view", captureViews[i]);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.TextureCubeMapPositiveX + i, envCubeMap, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                RenderCube();
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // then let OpenGL generate mipmaps from first mip face (combatting visible dots artifact)
            GL.BindTexture(TextureTarget.TextureCubeMap, envCubeMap);
            GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);
            
            // pbr: create an irradiance cubemap, and re-scale capture FBO to irradiance scale.
            irradianceMap = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, irradianceMap);
            for (int i = 0; i < 6; ++i)
            {
                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgb16f, 32, 32, 0, PixelFormat.Rgb, PixelType.Float, 0);
            }
            
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
            
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, 32, 32);
            
            // pbr: solve diffuse integral by convolution to create an irradiance (cube)map.
            irradianceShader = new Shader("Shaders/cubemap.vs", "Shaders/irradiance_convolution.fs");
            irradianceShader.Use();
            irradianceShader.SetInt("environmentMap", 0);
            irradianceShader.SetMatrix4("projection", captureProjection);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureCubeMap, envCubeMap);
            
            GL.Viewport(0, 0, 32, 32); // don't forget to configure the viewport to the capture dimensions.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
            for (int i = 0; i < 6; ++i)
            {
                irradianceShader.SetMatrix4("view", captureViews[i]);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.TextureCubeMapPositiveX + i, irradianceMap, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                
                RenderCube();
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // pbr: create a pre-filter cubemap, and re-scale capture FBO to pre-filter scale.
            prefilterMap = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, prefilterMap);
            for (int i = 0; i < 6; ++i)
            {
                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgb16f, 128, 128, 0, PixelFormat.Rgb, PixelType.Float, 0);
            }
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); // be sure to set minification filter to mip_linear
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            // generate mipmaps for the cubemap so OpenGL automatically allocates the required memory.
            GL.GenerateMipmap(GenerateMipmapTarget.TextureCubeMap);
            
            // pbr: run a quasi monte-carlo simulation on the environment lighting to create a prefilter (cube)map.
            prefilterShader = new Shader("Shaders/cubemap.vs", "Shaders/prefilter.fs");
            prefilterShader.Use();
            prefilterShader.SetInt("environmentMap", 0);
            prefilterShader.SetMatrix4("projection", captureProjection);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureCubeMap, envCubeMap);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
            var maxMipLevels = 5;
            for (int mip = 0; mip < maxMipLevels; ++mip)
            {
                // resize framebuffer according to mip-level size.
                var mipWidth = 128 * (int)MathF.Pow(0.5f, mip);
                var mipHeight = 128 * (int)MathF.Pow(0.5f, mip);
                
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, mipWidth, mipHeight);
                
                GL.Viewport(0, 0, mipWidth, mipHeight);

                prefilterShader.SetFloat("roughness",  (float)mip / (maxMipLevels - 1));
                for (int i = 0; i < 6; ++i)
                {
                    prefilterShader.SetMatrix4("view", captureViews[i]);
                    GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.TextureCubeMapPositiveX + i, prefilterMap, mip);
                    
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    RenderCube();
                }
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // pbr: generate a 2D LUT from the BRDF equations used.
            brdfLUTTexture = GL.GenTexture();
            // pre-allocate enough memory for the LUT texture.
            GL.BindTexture(TextureTarget.Texture2D, brdfLUTTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rg16f, 512, 512, 0, PixelFormat.Rg, PixelType.Float, 0);
            
            // be sure to set wrapping mode to GL_CLAMP_TO_EDGE
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            
            // then re-configure capture framebuffer object and render screen-space quad with BRDF shader.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, captureFbo);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, captureRbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, 512, 512);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, brdfLUTTexture, 0);
            
            GL.Viewport(0, 0, 512, 512);
            brdfShader = new Shader("Shaders/brdf.vs", "Shaders/brdf.fs");
            brdfShader.Use();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            RenderQuad();
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // initialize static shader uniforms before rendering
            pbrShader.Use();
            pbrShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            backgroundShader.Use();
            backgroundShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            
            // then before rendering, configure the viewport to the original framebuffer's screen dimensions
            GL.Viewport(0, 0, Size.X, Size.Y);
            
            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            GL.ClearColor(0.2f,0.3f,0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            // render scene, supplying the convoluted irradiance map to the final shader.
            pbrShader.Use();
            pbrShader.SetMatrix4("view", _camera.GetViewMatrix());
            pbrShader.SetVector3("camPos", _camera.Position);
            
            // bind pre-computed IBL data
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureCubeMap, irradianceMap);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.TextureCubeMap, prefilterMap);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, brdfLUTTexture);
            
            var model = Matrix4.Identity;
            
            // rusted iron
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, ironAlbedoMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, ironNormalMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture5);
            GL.BindTexture(TextureTarget.Texture2D, ironMetallicMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture6);
            GL.BindTexture(TextureTarget.Texture2D, ironRoughnessMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture7);
            GL.BindTexture(TextureTarget.Texture2D, ironAOMap.Handle);

            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new(-5, 0, 2)) * model;
            pbrShader.SetMatrix4("model", model);
            Matrix3.Transpose(new Matrix3(model.Inverted()), out var ironNormalMatrix);
            pbrShader.SetMatrix3("normalMatrix", ironNormalMatrix);
            RenderSphere();
            
            // gold
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, goldAlbedoMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, goldNormalMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture5);
            GL.BindTexture(TextureTarget.Texture2D, goldMetallicMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture6);
            GL.BindTexture(TextureTarget.Texture2D, goldRoughnessMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture7);
            GL.BindTexture(TextureTarget.Texture2D, goldAOMap.Handle);

            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new(-3, 0, 2)) * model;
            pbrShader.SetMatrix4("model", model);
            Matrix3.Transpose(new Matrix3(model.Inverted()), out var goldNormalMatrix);
            pbrShader.SetMatrix3("normalMatrix", goldNormalMatrix);
            RenderSphere();
            
            // grass
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, grassAlbedoMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, grassNormalMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture5);
            GL.BindTexture(TextureTarget.Texture2D, grassMetallicMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture6);
            GL.BindTexture(TextureTarget.Texture2D, grassRoughnessMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture7);
            GL.BindTexture(TextureTarget.Texture2D, grassAOMap.Handle);

            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new(-1, 0, 2)) * model;
            pbrShader.SetMatrix4("model", model);
            Matrix3.Transpose(new Matrix3(model.Inverted()), out var grassNormalMatrix);
            pbrShader.SetMatrix3("normalMatrix", grassNormalMatrix);
            RenderSphere();
            
            // plastic
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, plasticAlbedoMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, plasticNormalMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture5);
            GL.BindTexture(TextureTarget.Texture2D, plasticMetallicMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture6);
            GL.BindTexture(TextureTarget.Texture2D, plasticRoughnessMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture7);
            GL.BindTexture(TextureTarget.Texture2D, plasticAOMap.Handle);

            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new(1, 0, 2)) * model;
            pbrShader.SetMatrix4("model", model);
            Matrix3.Transpose(new Matrix3(model.Inverted()), out var plasticNormalMatrix);
            pbrShader.SetMatrix3("normalMatrix", plasticNormalMatrix);
            RenderSphere();
            
            // wall
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, wallAlbedoMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, wallNormalMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture5);
            GL.BindTexture(TextureTarget.Texture2D, wallMetallicMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture6);
            GL.BindTexture(TextureTarget.Texture2D, wallRoughnessMap.Handle);
            GL.ActiveTexture(TextureUnit.Texture7);
            GL.BindTexture(TextureTarget.Texture2D, wallAOMap.Handle);

            model = Matrix4.Identity;
            model = Matrix4.CreateTranslation(new(3, 0, 2)) * model;
            pbrShader.SetMatrix4("model", model);
            Matrix3.Transpose(new Matrix3(model.Inverted()), out var wallNormalMatrix);
            pbrShader.SetMatrix3("normalMatrix", wallNormalMatrix);
            RenderSphere();
            
            
            
            // render light source (simply re-render sphere at light positions)
            // this looks a bit off as we use the same shader, but it'll make their positions obvious and 
            // keeps the codeprint small.
            for (int i = 0; i < LightPositions.Count; i++)
            {
                var newPos = LightPositions[i];
                pbrShader.SetVector3($"lightPositions[{i}]", newPos);
                pbrShader.SetVector3($"lightColors[{i}]", LightColors[i]);

                model = Matrix4.Identity;
                model = Matrix4.CreateTranslation(newPos) * model;
                model = Matrix4.CreateScale(0.5f) * model;
                
                pbrShader.SetMatrix4("model", model);
                Matrix3.Transpose(new Matrix3(model.Inverted()), out var normalMatrix);
                pbrShader.SetMatrix3("normalMatrix", normalMatrix);
                RenderSphere();
            }
            
            // render skybox (render as last to prevent overdraw)
            backgroundShader.Use();
            backgroundShader.SetMatrix4("view", _camera.GetViewMatrix());
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureCubeMap, envCubeMap);
            // GL.BindTexture(TextureTarget.TextureCubeMap, irradianceMap); // display irradiance map
            // GL.BindTexture(TextureTarget.TextureCubeMap, prefilterMap); // display prefilter map
            RenderCube();
      

            SwapBuffers();
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
