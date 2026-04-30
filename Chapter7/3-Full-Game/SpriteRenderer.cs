using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LearnOpenTK;

public class SpriteRenderer
{
    private Shader _shader;
    private int quadVao;

    public SpriteRenderer(Shader shader)
    {
        _shader = shader;
        InitRenderData();
    }

    private void InitRenderData()
    {
        float[] vertices = { 
            // pos      // tex
            0.0f, 1.0f, 0.0f, 1.0f,
            1.0f, 0.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f, 

            0.0f, 1.0f, 0.0f, 1.0f,
            1.0f, 1.0f, 1.0f, 1.0f,
            1.0f, 0.0f, 1.0f, 0.0f
        };

        quadVao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertices.Length, vertices, BufferUsageHint.StaticDraw);
        
        GL.BindVertexArray(quadVao);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void DrawSprite(Texture2D texture, Vector2 position, Vector2 size, float rotate, Vector3 color)
    {
        _shader.Use();

        var model = Matrix4.Identity;
        model = Matrix4.CreateTranslation(new Vector3(position.X, position.Y, 0)) * model; // first translate (transformations are: scale happens first, then rotation, and then final translation happens; reversed order)
        model = Matrix4.CreateTranslation(new Vector3(0.5f * size.X, 0.5f * size.Y, 0)) * model; // move origin of rotation to center of quad

        model = Matrix4.CreateFromAxisAngle(new(0, 0, 1), MathHelper.DegreesToRadians(rotate)) * model; // then rotate
        model = Matrix4.CreateTranslation(new Vector3(-0.5f * size.X,-0.5f * size.Y, 0)) * model; // move origin back

        model = Matrix4.CreateScale(new Vector3(size.X, size.Y, 1)) * model; // last scale
        
        _shader.SetMatrix4("model", model);
        
        _shader.SetVector3("spriteColor", color);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        texture.Bind();
        
        GL.BindVertexArray(quadVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);
    }
}
