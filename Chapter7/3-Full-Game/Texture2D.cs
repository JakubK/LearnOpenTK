using OpenTK.Graphics.OpenGL4;

namespace LearnOpenTK;

public class Texture2D
{
    public int ID;

    public int Width;
    public int Height;

    public PixelInternalFormat InternalFormat;
    public PixelFormat ImageFormat;


    public int WrapS;
    public int WrapT;
    public int FilterMin;
    public int FilterMag;

    public Texture2D()
    {
        ID = GL.GenTexture();

        InternalFormat = PixelInternalFormat.Rgb;
        ImageFormat = PixelFormat.Rgb;
        WrapS = (int)TextureWrapMode.Repeat;
        WrapT = (int)TextureWrapMode.Repeat;
        FilterMin = (int)TextureMinFilter.Linear;
        FilterMag = (int)TextureMagFilter.Linear;
    }

    public void Generate(int width, int height, byte[]? data)
    {
        Width = width;
        Height = height;
        
        // create texture
        GL.BindTexture(TextureTarget.Texture2D, ID);
        if (data == null)
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat, width, height, 0, ImageFormat, PixelType.UnsignedByte, 0);
        }
        else
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat, width, height, 0, ImageFormat, PixelType.UnsignedByte, data);
        }
        
        // set Texture wrap and filter modes
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, WrapS);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, WrapT);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, FilterMin);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, FilterMag);
        
        // unbind texture
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2D, ID);
    }
}