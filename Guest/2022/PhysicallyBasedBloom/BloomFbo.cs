using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LearnOpenTK;

public class BloomFbo
{
    private bool init;
    private int fbo;
    
    public bool Init(int screenWidth, int screenHeight, int numBloomMips)
    {
        if (init) return true;

        fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        
        var mipSize = new Vector2(screenWidth, screenHeight);
        var mipIntSize = new Vector2i(screenWidth, screenHeight);

        for (var i = 0; i < numBloomMips; i++)
        {
            BloomMip mip;

            mipSize *= 0.5f;
            mipIntSize /= 2;
            mip.Size = mipSize;
            mip.IntSize = mipIntSize;

            mip.Texture = GL.GenTexture();
            
            GL.BindTexture(TextureTarget.Texture2D, mip.Texture);
            // we are downscaling an HDR color buffer, so we need a float texture format
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R11fG11fB10f,
                (int)mipSize.X, (int)mipSize.Y,
                0, PixelFormat.Rgb, PixelType.Float, 0);
            
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            
            Console.WriteLine("Created bloom mip x:" + mipIntSize.X + " y: " + mipIntSize.Y);
            
            MipChain.Add(mip);
        }

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, MipChain[0].Texture, 0);

        // setup attachments
        DrawBuffersEnum[] attachments = { DrawBuffersEnum.ColorAttachment0 };
        GL.DrawBuffers(1, attachments);

        // check completion status
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            Console.WriteLine("gbuffer FBO error, status:" + status);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return false;
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        init = true;
        return true;
    }

    public void BindForWriting()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
    }

    public List<BloomMip> MipChain = new();
}