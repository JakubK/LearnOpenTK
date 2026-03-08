using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace LearnOpenTK.Common;

public static class Utils
{
    public static int TextureFromFile(string path, string directory, bool gamma = false)
    {
        var fullPath = Path.Combine(directory, path);

        var textureId = GL.GenTexture();
        StbImage.stbi_set_flip_vertically_on_load(1);
            
        // Here we open a stream to the file and pass it to StbImageSharp to load.
        using (var stream = File.OpenRead(fullPath))
        {
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            if (image != null)
            {
                GL.BindTexture(TextureTarget.Texture2D, textureId);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,  (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,  (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,  (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,  (int)TextureMinFilter.Linear);
            }
            else
            {
                Console.WriteLine($"Something went wrong when loading image from Path {fullPath}");
            }
        }
        
        return textureId;
    }
}