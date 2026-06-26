using OpenTK.Mathematics;

namespace LearnOpenTK.Common;

public class Sphere : BoundingVolume
{
    private readonly Vector3 _center;
    private readonly float _radius;

    public Sphere(Vector3 center, float radius)
    {
        _center = center;
        _radius = radius;
    }
    
    public override bool IsOnFrustum(Frustum camFrustum, Transform transform)
    {
        //Get global scale thanks to our transform
        var globalScale = transform.GetGlobalScale();

        //Get our global center with process it with the global model matrix of our transform
        var globalCenter = transform.GetModelMatrix() * new Vector4(_center, 1f);
        
        //To wrap correctly our shape, we need the maximum scale scalar.
        var maxScale = MathHelper.Max(MathHelper.Max(globalScale.X, globalScale.Y), globalScale.Z);
        
        //Max scale is assuming for the diameter. So, we need the half to apply it to our radius
        var globalSphere = new Sphere(globalCenter.Xyz, _radius * (maxScale * 0.5f));
        
        //Check Firstly the result that have the most chance to failure to avoid to call all functions.
        return globalSphere.IsOnOrForwardPlane(camFrustum.LeftFace) &&
                globalSphere.IsOnOrForwardPlane(camFrustum.RightFace) &&
                globalSphere.IsOnOrForwardPlane(camFrustum.FarFace) &&
                globalSphere.IsOnOrForwardPlane(camFrustum.NearFace) &&
                globalSphere.IsOnOrForwardPlane(camFrustum.TopFace) &&
                globalSphere.IsOnOrForwardPlane(camFrustum.BottomFace);
    }

    public override bool IsOnOrForwardPlane(Plane plane)
    {
        return plane.GetSignedDistanceToPlane(_center) > -_radius;
    }
}