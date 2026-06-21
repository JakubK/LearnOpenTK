namespace LearnOpenTK.Common;

public abstract class BoundingVolume
{
    public abstract bool IsOnFrustum(Frustum frustum, Transform transform);

    public abstract bool IsOnForwardPlane(Plane plane);

    public bool IsOnFrustum(Frustum frustum)
    {
        return (
            IsOnForwardPlane(frustum.LeftFace)
            && IsOnForwardPlane(frustum.RightFace)
            && IsOnForwardPlane(frustum.TopFace)
            && IsOnForwardPlane(frustum.BottomFace)
            && IsOnForwardPlane(frustum.NearFace)
            && IsOnForwardPlane(frustum.FarFace)
        );
    }
}