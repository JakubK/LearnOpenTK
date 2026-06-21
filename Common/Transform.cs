using OpenTK.Mathematics;

namespace LearnOpenTK.Common;

public class Transform
{
    // Local Space
    private Vector3 _pos = Vector3.Zero;
    private Vector3 _eulerRot = Vector3.Zero; //degrees
    private Vector3 _scale = Vector3.One;
    
    // Global Space
    private Matrix4 _model = Matrix4.Identity;
    
    public bool IsDirty;

    public void SetLocalRotation(Vector3 localRotation)
    {
        _eulerRot = localRotation;
        IsDirty = true;
    }
    
    public void SetLocalPosition(Vector3 localPos)
    {
        _pos = localPos;
        IsDirty = true;
    }
    
    public void SetLocalScale(Vector3 localScale)
    {
        _scale = localScale;
        IsDirty = true;
    }

    public Matrix4 GetModelMatrix()
    {
        return _model;
    }

    public Matrix4 GetLocalModelMatrix()
    {
        var transformX = Matrix4.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.DegreesToRadians(_eulerRot.X));
        var transformY = Matrix4.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.DegreesToRadians(_eulerRot.Y));
        var transformZ = Matrix4.CreateFromAxisAngle(new Vector3(0, 0, 1), MathHelper.DegreesToRadians(_eulerRot.Z));

        // Y * X * Z
        var rotationMatrix = transformY * transformX * transformZ;

        // translation * rotation * scale (also know as TRS matrix)
        return Matrix4.CreateTranslation(_pos) * rotationMatrix * Matrix4.CreateScale(_scale);
    }

    public Vector3 GetLocalRotation()
    {
        return _eulerRot;
    }

    public void ComputeModelMatrix()
    {
        _model = GetLocalModelMatrix();
        IsDirty = false;
    }

    public void ComputeModelMatrix(Matrix4 parentModelMatrix)
    {
        _model = parentModelMatrix * GetLocalModelMatrix();
        IsDirty = false;
    }
}