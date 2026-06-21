using System.Collections.Generic;
using System.Linq;

namespace LearnOpenTK.Common;

public class Entity
{
    public List<Entity> Children = new();
    public Entity? Parent { get; set; }
    
    //Space information
    public Transform Transform = new ();
    public Model Model;
    
    public Entity(Model model)
    {
        Model = model;
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

    public void DrawSelfAndChild(Frustum frustum, Shader shader, int display, int total)
    {
        // if (boundingVolume.isOnFrustum(frustum, Transform))
        // {
        //     shader.SetMatrix4("model", Transform.GetModelMatrix());
        //     Model.Draw(shader);
        //     display++;
        // }
        //
        // total++;
        //
        // foreach (var child in Children)
        // {
        //     child.DrawSelfAndChild(frustum, shader, display, total);
        // }
    }
}