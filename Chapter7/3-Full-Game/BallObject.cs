using OpenTK.Mathematics;

namespace LearnOpenTK;

public class BallObject : GameObject
{
    public float Radius;
    public bool Stuck;
    public bool Sticky;
    public bool PassThrough;

    public BallObject() : base()
    {
        Radius = 12.5f;
        Stuck = true;
        Sticky = false;
        PassThrough = false;
    }

    public BallObject(Vector2 pos, float radius, Vector2 velocity, Texture2D sprite) : base(pos, new Vector2(radius * 2, radius * 2), velocity, Vector3.One, sprite)
    {
        Radius = radius;
        Stuck = true;
        Sticky = false;
        PassThrough = false;
    }

    public Vector2 Move(float dt,  int windowWidth)
    {
        // if not stuck to player board
        if (!Stuck)
        {
            // move the ball
            Position += Velocity * dt;
            
            // then check if outside window bounds and if so, reverse velocity and restore at correct position
            if (Position.X <= 0)
            {
                Velocity.X = -Velocity.X;
                Position.X = 0;
            } else if (Position.X + Size.X >= windowWidth)
            {
                Velocity.X = -Velocity.X;
                Position.X = windowWidth - Size.X;
            }

            if (Position.Y <= 0)
            {
                Velocity.Y = -Velocity.Y;
                Position.Y = 0;
            }
        }

        return Position;
    }

    public void Reset(Vector2 position, Vector2 velocity)
    {
        Position = position;
        Velocity = velocity;

        Stuck = true;
        Sticky = false;
        PassThrough = false;
    }
}