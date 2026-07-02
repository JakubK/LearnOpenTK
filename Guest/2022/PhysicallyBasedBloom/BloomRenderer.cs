using LearnOpenTK.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace LearnOpenTK;

public class BloomRenderer
{
    private bool init;
    private BloomFbo mFbo = new();
    private Vector2 srcViewportSize;
    private Vector2 srcViewportSizeFloat;
    private Shader downSampleShader;
    private Shader upSampleShader;
    private bool karisAverageOnDownsample = true;

    
    public BloomRenderer()
    {
        init = false;
    }
    
    public bool Init(int screenWidth, int screenHeight)
    {
        if (init) return true;
        srcViewportSize = new Vector2(screenWidth, screenWidth);
        srcViewportSizeFloat = new Vector2(screenWidth, screenWidth);

        // Framebuffer
        var num_bloom_mips = 6; // TODO: Play around with this value
        bool status = mFbo.Init(screenWidth, screenHeight, num_bloom_mips);
        if (!status) {
            Console.WriteLine("Failed to initialize bloom FBO - cannot create bloom renderer!");
            return false;
        }

        // Shaders
        downSampleShader = new Shader("Shaders/new_downsample.vs", "Shaders/new_downsample.fs");
        upSampleShader = new Shader("Shaders/new_upsample.vs", "Shaders/new_upsample.fs");

        // Downsample
        downSampleShader.Use();
        downSampleShader.SetInt("srcTexture", 0);
        GL.UseProgram(0);

        // Upsample
        upSampleShader.Use();
        upSampleShader.SetInt("srcTexture", 0);
        GL.UseProgram(0);

        return true;
    }

    public void RenderBloomTexture(int srcTexture, float filterRadius)
    {
        mFbo.BindForWriting();

        RenderDownsamples(srcTexture);
        RenderUpsamples(filterRadius);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        // Restore viewport
        GL.Viewport(0, 0, (int)srcViewportSize.X, (int)srcViewportSize.Y);
    }

    public int BloomTexture()
    {
        return mFbo.MipChain[0].Texture;
    }
    
    private void RenderUpsamples(float filterRadius)
    {
        var mipChain = mFbo.MipChain;
        
        upSampleShader.Use();
        upSampleShader.SetFloat("filterRadius", filterRadius);

        // Enable additive blending
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
        GL.BlendEquation(BlendEquationMode.FuncAdd);

        for (int i = (int)mipChain.Count - 1; i > 0; i--)
        {
            var mip = mipChain[i];
            var nextMip = mipChain[i - 1];

            // Bind viewport and texture from where to read
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, mip.Texture);

            // Set framebuffer render target (we write to this texture)
            GL.Viewport(0, 0, (int)nextMip.Size.X, (int)nextMip.Size.Y);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, nextMip.Texture, 0);

            // Render screen-filled quad of resolution of current mip
            RenderQuad();
        }

        // Disable additive blending
        // GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.Blend);

        GL.UseProgram(0);
    }

    private void RenderDownsamples(int srcTexture)
    {
        var mipChain = mFbo.MipChain;

        downSampleShader.Use();
        downSampleShader.SetVector2("srcResolution", srcViewportSizeFloat);
        if (karisAverageOnDownsample) {
            downSampleShader.SetInt("mipLevel", 0);
        }

        // Bind srcTexture (HDR color buffer) as initial texture input
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, srcTexture);

        // Progressively downsample through the mip chain
        for (int i = 0; i < (int)mipChain.Count; i++)
        {
            var mip = mipChain[i];
            GL.Viewport(0, 0, (int)mip.Size.X, (int)mip.Size.Y);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, mip.Texture, 0);

            // Render screen-filled quad of resolution of current mip
            RenderQuad();

            // Set current mip resolution as srcResolution for next iteration
            downSampleShader.SetVector2("srcResolution", mip.Size);
            // Set current mip as texture input for next iteration
            GL.BindTexture(TextureTarget.Texture2D, mip.Texture);
            // Disable Karis average for consequent downsamples
            if (i == 0) { downSampleShader.SetInt("mipLevel", 1); }
        }

        GL.UseProgram(0);
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