using OpenTK.Mathematics;

namespace LearnOpenTK.Common;

public class AABB : BoundingVolume
{
    private readonly Vector3 _center;
    private readonly Vector3 _extents;

    public AABB(Vector3 min, Vector3 max)
    {
        _center = (min + max) * 0.5f;
        _extents = new Vector3(max.X - _center.X, max.Y - _center.Y, max.Z - _center.Z);
    }
    
    public AABB(Vector3 center, float ii, float ij, float ik)
    {
        _center = center;
        _extents = new Vector3(ii, ij, ik);
    }
    
    public override bool IsOnFrustum(Frustum camFrustum, Transform transform)
    {
        //Get global scale thanks to our transform
        // var globalCenter = transform.GetModelMatrix() * new Vector4(_center, 1f);
        var globalCenter =  new Vector4(_center, 1f) * transform.GetModelMatrix();
        
        // Scaled orientation
        var right = transform.GetRight() * _extents.X;
        var up = transform.GetUp() * _extents.Y;
        var forward = transform.GetForward() * _extents.Z;

        var newIi = MathHelper.Abs(Vector3.Dot(Vector3.UnitX, right.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitX, up.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitX, forward.Xyz));
        
        var newIj = MathHelper.Abs(Vector3.Dot(Vector3.UnitY, right.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitY, up.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitY, forward.Xyz));
        
        var newIk = MathHelper.Abs(Vector3.Dot(Vector3.UnitZ, right.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitZ, up.Xyz))
                    + MathHelper.Abs(Vector3.Dot(Vector3.UnitZ, forward.Xyz));

        var globalAABB = new AABB(globalCenter.Xyz, newIi, newIj, newIk);
        
        return (globalAABB.IsOnOrForwardPlane(camFrustum.LeftFace) &&
                globalAABB.IsOnOrForwardPlane(camFrustum.RightFace) &&
                globalAABB.IsOnOrForwardPlane(camFrustum.TopFace) &&
                globalAABB.IsOnOrForwardPlane(camFrustum.BottomFace) &&
                globalAABB.IsOnOrForwardPlane(camFrustum.NearFace) &&
                globalAABB.IsOnOrForwardPlane(camFrustum.FarFace));
    }

    // see https://gdbooks.gitbooks.io/3dcollisions/content/Chapter2/static_aabb_plane.html
    public override bool IsOnOrForwardPlane(Plane plane)
    {
        var r = _extents.X * MathHelper.Abs(plane.Normal.X)
                + _extents.Y * MathHelper.Abs(plane.Normal.Y)
                + _extents.Z * MathHelper.Abs(plane.Normal.Z);

        return -r <= plane.GetSignedDistanceToPlane(_center);
    }
}