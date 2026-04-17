using OpenTK.Mathematics;

namespace LearnOpenTK;

public struct Character
{
    public int TextureId; // ID handle of the glyph texture
    public Vector2 Size; // Size of Glyph
    public Vector2 Bearing; // Offset from baseline to left/top of glyph
    public int Advance; // Horizontal offset to advance to next glyph
}