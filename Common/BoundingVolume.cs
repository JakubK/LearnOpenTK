namespace LearnOpenTK.Common;

public abstract class BoundingVolume
{
    public abstract bool IsOnFrustum(Frustum frustum, Transform transform);

    public abstract bool IsOnOrForwardPlane(Plane plane);

    public bool IsOnFrustum(Frustum camFrustum)
    {
        return (
            IsOnOrForwardPlane(camFrustum.LeftFace)
            && IsOnOrForwardPlane(camFrustum.RightFace)
            && IsOnOrForwardPlane(camFrustum.TopFace)
            && IsOnOrForwardPlane(camFrustum.BottomFace)
            && IsOnOrForwardPlane(camFrustum.NearFace)
            && IsOnOrForwardPlane(camFrustum.FarFace)
        );
    }
}