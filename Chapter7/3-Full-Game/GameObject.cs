using OpenTK.Mathematics;

namespace LearnOpenTK;

public class GameObject
{
    public string Name { get; set; }
    
    public Vector2 Position;
    public Vector2 Size;
    public Vector2 Velocity;
    public Vector3 Color;
    public Texture2D? Sprite;
    
    public float Rotation;

    public bool IsSolid = false;
    public bool IsDestroyed = false;

    public GameObject() : this(Vector2.Zero, Vector2.One, Vector2.Zero, Vector3.One, null!)
    {
    }

    public GameObject(Vector2 pos, Vector2 size, Vector2 velocity, Vector3 color, Texture2D sprite)
    {
        Position = pos;
        Velocity = velocity;
        Color = color;
        Sprite = sprite;
        Size = size;
    }

    public virtual void Draw(SpriteRenderer renderer)
    {
        renderer.DrawSprite(Sprite!, Position, Size, Rotation, Color);
    }
}
