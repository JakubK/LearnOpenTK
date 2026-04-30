using OpenTK.Mathematics;

namespace LearnOpenTK;

public class PowerUp : GameObject
{
    private static readonly Vector2 PowerUpSize = new (60f, 20f);
    private static readonly Vector2 PowerUpVelocity = new (0, 150f);
    public string Type;
    public float Duration;
    public bool Activated;

    public PowerUp(string type, Vector3 color, float duration, Vector2 position, Texture2D texture) : base(position, PowerUpSize, PowerUpVelocity, color, texture)
    {
        Type = type;
        Duration = duration;
    }
}