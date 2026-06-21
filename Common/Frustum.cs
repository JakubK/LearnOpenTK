using System;
using OpenTK.Mathematics;

namespace LearnOpenTK.Common;

public class Frustum
{
    public Plane TopFace;
    public Plane BottomFace;
    public Plane RightFace;
    public Plane LeftFace;
    public Plane NearFace;
    public Plane FarFace;

    public static Frustum FromCamera(Camera camera, float aspect, float fovY, float zNear, float zFar)
    {
        Frustum frustum = new();
        var halfVSide = zFar * MathF.Tanh(fovY * .5f);
        var halfHSide = halfVSide * aspect;
        var frontMultFar = zFar * camera.Front;

        frustum.NearFace = new(camera.Position + zNear * camera.Front, camera.Front);
        frustum.FarFace = new(camera.Position + frontMultFar, -camera.Front);
        
        frustum.RightFace = new(camera.Position,  Vector3.Cross(frontMultFar - camera.Right * halfHSide, camera.Up));
        frustum.LeftFace = new(camera.Position, Vector3.Cross(camera.Up, frontMultFar + camera.Right * halfHSide));
        
        frustum.TopFace = new(camera.Position, Vector3.Cross(camera.Right, frontMultFar - camera.Up * halfVSide));
        frustum.BottomFace = new(camera.Position, Vector3.Cross(frontMultFar + camera.Up * halfVSide, camera.Right));
        
        return frustum;
    }
}