using OpenTK.Mathematics;

namespace LearnOpenTK;

public record Collision(bool Occured, Direction Direction, Vector2 DiffVector);
