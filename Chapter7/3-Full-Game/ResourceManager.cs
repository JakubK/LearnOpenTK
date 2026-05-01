using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace LearnOpenTK;

public class ResourceManager
{
    public static Dictionary<string, Shader> Shaders = new();
    public static Dictionary<string, Texture2D> Textures = new();
    
    public static Shader LoadShader(string vShaderFile, string fShaderFile, string? gShaderFile, string name)
    {
        var shader = new Shader(vShaderFile, fShaderFile, gShaderFile);
        Shaders[name] = shader;
        return shader;
    }

    public static Shader GetShader(string name)
    {
        return Shaders[name];
    }

    public static Texture2D LoadTexture(string file, bool alpha, string name)
    {
        var texture = TextureFromFile(file, alpha);
        Textures[name] = texture;
        return texture;
    }

    public static Texture2D GetTexture(string name)
    {
        return Textures[name];
    }

    public void Clear()
    {
        foreach (var shader in Shaders.Values)
        {
            GL.DeleteProgram(shader.Handle);
        }
        
        foreach (var texture in Textures.Values)
        {
            GL.DeleteTexture(texture.ID);
        }
        
        Shaders.Clear();
        Textures.Clear();
    }

    private static Texture2D TextureFromFile(string file, bool alpha)
    {
        var texture = new Texture2D();
        if (alpha)
        {
            texture.InternalFormat = PixelInternalFormat.Rgba;
            texture.ImageFormat = PixelFormat.Rgba;
        }
        
        StbImage.stbi_set_flip_vertically_on_load(1);
        using var stream = File.OpenRead(file);
        ImageResult? image;
        if (alpha)
        {
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }
        else
        {
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);
        }
        Console.WriteLine("Loading " + file);
        texture.Generate(image.Width, image.Height, image.Data);

        return texture;
    }
}