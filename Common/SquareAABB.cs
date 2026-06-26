using OpenTK.Mathematics;

namespace LearnOpenTK.Common;

public class SquareAABB : BoundingVolume
{
    private readonly Vector3 _center;
    private readonly float _extent;

    public SquareAABB(Vector3 center, float extent)
    {
        _center = center;
        _extent = extent;
    }
    
    public override bool IsOnFrustum(Frustum camFrustum, Transform transform)
    {
        //Get global scale thanks to our transform
        var globalCenter = transform.GetModelMatrix() * new Vector4(_center, 1f);
        // Scaled orientation
        var right = transform.GetRight() * _extent;
        var up = transform.GetUp() * _extent;
        var forward = transform.GetForward() * _extent;

        var newIi = MathHelper.Abs(Vector3.Dot(Vector3.UnitX, right.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitX, up.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitX, forward.Xyz));
        
        var newIj = MathHelper.Abs(Vector3.Dot(Vector3.UnitY, right.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitY, up.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitY, forward.Xyz));
        
        var newIk = MathHelper.Abs(Vector3.Dot(Vector3.UnitZ, right.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitZ, up.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitZ, forward.Xyz));

        var globalAABB = new SquareAABB(globalCenter.Xyz, MathHelper.Max(MathHelper.Max(newIi, newIj), newIk));
        return (globalAABB.IsOnOrForwardPlane(camFrustum.LeftFace) &&
                globalAABB.IsOnOrForwardPlane(camFrustum.RightFace) &&
                globalAABB.IsOnOrForwardPlane(camFrustum.TopFace) &&
                globalAABB.IsOnOrForwardPlane(camFrustum.BottomFace) &&
                globalAABB.IsOnOrForwardPlane(camFrustum.NearFace) &&
                globalAABB.IsOnOrForwardPlane(camFrustum.FarFace));
    }

    public override bool IsOnOrForwardPlane(Plane plane)
    {
        // Compute the projection interval radius of b onto L(t) = b.c + t * p.n
        var r = _extent * MathHelper.Abs(plane.Normal.X) + MathHelper.Abs(plane.Normal.Y) + MathHelper.Abs(plane.Normal.Z);
        return -r <= plane.GetSignedDistanceToPlane(_center);
    }
}