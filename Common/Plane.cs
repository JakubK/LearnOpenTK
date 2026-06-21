using OpenTK.Mathematics;

namespace LearnOpenTK.Common;

public struct Plane
{
    public Vector3 Normal = Vector3.UnitY;
    public float Distance = 0;

    public Plane()
    {
    }

    public Plane(Vector3 p1, Vector3 norm)
    {
        Normal = Vector3.Normalize(norm);
        Distance = Vector3.Dot(norm, p1);
    }

    public float GetSignedDistanceToPlane(Vector3 point)
    {
        return Vector3.Dot(Normal, point) - Distance;
    }
}