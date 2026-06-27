using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace LearnOpenTK.Common;

public class Entity
{
    public List<Entity> Children = new();
    public Entity? Parent { get; set; }
    
    //Space information
    public Transform Transform = new ();
    public Model Model;

    private BoundingVolume _boundingVolume;
    
    public Entity(Model model)
    {
        Model = model;
        _boundingVolume = GenerateAABB(model);
    }

    public void AddChild(Model model)
    {
        Children.Add(new Entity(model));
        Children.Last().Parent = this;
    }
    
    public void UpdateSelfAndChild()
    {
        if (Transform.IsDirty)
        {
            ForceUpdateSelfAndChild();
            return;
        }

        foreach (var child in Children)
        {
            child.UpdateSelfAndChild();
        }
    }

    private void ForceUpdateSelfAndChild()
    {
        if (Parent != null)
        {
            Transform.ComputeModelMatrix(Parent.Transform.GetModelMatrix());
        }
        else
        {
            Transform.ComputeModelMatrix();
        }

        foreach (var child in Children)
        {
            child.ForceUpdateSelfAndChild();
        }
    }

    public void DrawSelfAndChild(Frustum frustum, Shader shader, ref int display, ref int total)
    {
        if (_boundingVolume.IsOnFrustum(frustum, Transform))
        {
            shader.SetMatrix4("model", Transform.GetModelMatrix());
            Model.Draw(shader);
            display++;
        }

        total++;
        
        foreach (var child in Children)
        {
            child.DrawSelfAndChild(frustum, shader, ref display, ref total);
        }
        
    }

    private AABB GenerateAABB(Model model)
    {
        var minAABB = new Vector3(float.MaxValue);
        var maxAABB = new Vector3(float.MinValue);

        foreach (var mesh in model.GetMeshes())
        {
            foreach (var vertex in mesh.Vertices)
            {
                minAABB.X = MathHelper.Min(minAABB.X, vertex.Position.X);
                minAABB.Y = MathHelper.Min(minAABB.Y, vertex.Position.Y);
                minAABB.Z = MathHelper.Min(minAABB.Z, vertex.Position.Z);
                
                maxAABB.X = MathHelper.Max(maxAABB.X, vertex.Position.X);
                maxAABB.Y = MathHelper.Max(maxAABB.Y, vertex.Position.Y);
                maxAABB.Z = MathHelper.Max(maxAABB.Z, vertex.Position.Z);
            }
        }
        
        return new AABB(minAABB, maxAABB);
    }
}