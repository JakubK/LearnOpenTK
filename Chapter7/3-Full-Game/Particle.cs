using OpenTK.Mathematics;

namespace LearnOpenTK;

public struct Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector4 Color;
    public float Life;

    public Particle()
    {
        Position = Vector2.Zero;
        Velocity = Vector2.Zero;
        Color = Vector4.One;
        Life = 0;
    }
}