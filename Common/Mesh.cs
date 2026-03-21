using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;


namespace LearnOpenTK.Common;

public class Mesh
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;
        public Vector3 Tangent;
        public Vector3 Bitangent;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Texture
    {
         public int Id;
         public string Type;
         public string Path;
    }
    
    public List<Vertex> Vertices;
    public List<int> Indices;
    public List<Texture> Textures;

    private int _vao;
    private int _vbo;
    private int _ebo;
    
    public Mesh(List<Vertex> vertices, List<int> indices, List<Texture> textures)
    {
        Vertices = vertices;
        Indices = indices;
        Textures = textures;
        
        SetupMesh();
    }

    public int GetVao()
    {
        return _vao;
    }
    
    public void Draw(Shader shader)
    {
        int diffuseNr = 1;
        int specularNr = 1;
        int normalNr = 1;
        int heightNr = 1;
        
        for (int i = 0; i < Textures.Count; i++)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + i);


            string number = string.Empty;
            string name = Textures[i].Type;
            if (name == "texture_diffuse")
            {
                number = (diffuseNr++).ToString();
            } else if (name == "texture_specular")
            {
                number = (specularNr++).ToString();
            }  else if (name == "texture_normal")
            {
                number = (normalNr++).ToString();
            } else if (name == "texture_height")
            {
                number = (heightNr++).ToString();
            }
            
            GL.Uniform1(GL.GetUniformLocation(shader.Handle, (name + number)), i);
            GL.BindTexture(TextureTarget.Texture2D, Textures[i].Id);
        }
        
        // Draw Mesh
        GL.BindVertexArray(_vao);
        GL.DrawElements(PrimitiveType.Triangles, Indices.Count, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
        
        GL.ActiveTexture(TextureUnit.Texture0);
    }

    private void SetupMesh()
    {
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        var vertexSize = Marshal.SizeOf<Vertex>();
        
        GL.BindVertexArray(_vao);
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, Vertices.Count * vertexSize, Vertices.ToArray(), BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, Indices.Count * sizeof(int), Indices.ToArray(), BufferUsageHint.StaticDraw);
        
        // Vertex Positions
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, 0);
        
        // Vertex Normals
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal)));
        
        // Texture coords
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, vertexSize, Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoord)));
        
        // Vertex Tangent
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, vertexSize, Marshal.OffsetOf<Vertex>(nameof(Vertex.Tangent)));
        
        // Vertex Bitangent
        GL.EnableVertexAttribArray(4);
        GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, vertexSize, Marshal.OffsetOf<Vertex>(nameof(Vertex.Bitangent)));
        
        GL.BindVertexArray(0);
    }
}