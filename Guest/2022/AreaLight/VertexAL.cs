using OpenTK.Mathematics;

namespace LearnOpenTK;

public struct VertexAL
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TexCoord;

    public VertexAL(Vector3 pos, Vector3 normal, Vector2 texCoord)
    {
        Position = pos;
        Normal = normal;
        TexCoord = texCoord;
    }
}