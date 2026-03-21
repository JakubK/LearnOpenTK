using System;
using System.Collections.Generic;
using System.IO;
using Assimp;
using OpenTK.Mathematics;

namespace LearnOpenTK.Common;

public class Model
{
    private readonly List<Mesh.Texture> _texturesLoaded = new();
    private readonly List<Mesh> _meshes = new();
    private string _directory;

    public Model(string path)
    {
        LoadModel(path);
    }

    public List<Mesh.Texture> GetTexturesLoaded()
    {
        return _texturesLoaded;
    }

    public List<Mesh> GetMeshes()
    {
        return _meshes;
    }
    
    public void Draw(Shader shader)
    {
        for (int i = 0; i < _meshes.Count; i++)
        {
            _meshes[i].Draw(shader);
        }
    }

    private void LoadModel(string path)
    {
        AssimpContext importer = new AssimpContext();
        var scene = importer.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace);
        if (scene == null || scene.SceneFlags.HasFlag(SceneFlags.Incomplete) || scene.RootNode == null)
        {
            Console.WriteLine("Something went wrong when loading model");
            return;
        }
        
        _directory = Path.GetDirectoryName(path);
        
        ProcessNode(scene.RootNode, scene);
    }

    private void ProcessNode(Node node, Scene scene)
    {
        for (int i = 0; i < node.MeshCount; i++)
        {
            var mesh = scene.Meshes[node.MeshIndices[i]];
            _meshes.Add(ProcessMesh(mesh, scene));
        }

        for (int j = 0; j < node.ChildCount; j++)
        {
            ProcessNode(node.Children[j], scene);
        }
    }

    private Mesh ProcessMesh(Assimp.Mesh assimpMesh, Scene scene)
    {
        var vertices = new List<Mesh.Vertex>();
        var indices = new List<int>();
        var textures = new List<Mesh.Texture>();

        for (int i = 0; i < assimpMesh.VertexCount; i++)
        {
            var vector = new Vector3(
                assimpMesh.Vertices[i].X,
                assimpMesh.Vertices[i].Y,
                assimpMesh.Vertices[i].Z
            );

            Mesh.Vertex vertex = new Mesh.Vertex();
            vertex.Position = vector;

            if (assimpMesh.HasNormals)
            {
                vector.X = assimpMesh.Normals[i].X;
                vector.Y = assimpMesh.Normals[i].Y;
                vector.Z = assimpMesh.Normals[i].Z;
                vertex.Normal = vector;
            }

            if (assimpMesh.HasTextureCoords(0))
            {
                var vec = new Vector2();
                vec.X = assimpMesh.TextureCoordinateChannels[0][i].X;
                vec.Y = assimpMesh.TextureCoordinateChannels[0][i].Y;
                vertex.TexCoord = vec;

                vector.X = assimpMesh.Tangents[i].X;
                vector.Y = assimpMesh.Tangents[i].Y;
                vector.Z = assimpMesh.Tangents[i].Z;
                vertex.Tangent = vector;
                
                vector.X = assimpMesh.BiTangents[i].X;
                vector.Y = assimpMesh.BiTangents[i].Y;
                vector.Z = assimpMesh.BiTangents[i].Z;
                
                vertex.Bitangent = vector;
            }
            else
            {
                vertex.TexCoord = Vector2.Zero;
            }
            
            vertices.Add(vertex);
        }

        for (int i = 0; i < assimpMesh.FaceCount; i++)
        {
            var face = assimpMesh.Faces[i];
            for (int j = 0; j < face.IndexCount; j++)
            {
                indices.Add(face.Indices[j]);
            }
        }

        var material = scene.Materials[assimpMesh.MaterialIndex];

        var diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse, "texture_diffuse");
        textures.AddRange(diffuseMaps);
        
        var specularMaps = LoadMaterialTextures(material, TextureType.Specular, "texture_specular");
        textures.AddRange(specularMaps);
        
        var normalMaps = LoadMaterialTextures(material, TextureType.Height, "texture_normal");
        textures.AddRange(normalMaps);
        
        var heightMaps = LoadMaterialTextures(material, TextureType.Ambient, "texture_height");
        textures.AddRange(heightMaps);

        return new Mesh(vertices, indices, textures);
    }

    private List<Mesh.Texture> LoadMaterialTextures(Material material, TextureType textureType, string typeName)
    {
        var textures = new List<Mesh.Texture>();
        var materialTextureCount = material.GetMaterialTextureCount(textureType);
        
        for (int i = 0; i < materialTextureCount; i++)
        {
            material.GetMaterialTexture(textureType, i, out var slot);
            bool skip = false;
            for (int j = 0; j < _texturesLoaded.Count; j++)
            {
                if (_texturesLoaded[j].Path == slot.FilePath)
                {
                    textures.Add(_texturesLoaded[j]);
                    skip = true;
                    break;
                }
            }

            if (!skip)
            {
                var texture = new Mesh.Texture();
                texture.Id = Utils.TextureFromFile(slot.FilePath, _directory);
                texture.Type = typeName;
                texture.Path = slot.FilePath;
                textures.Add(texture);
                _texturesLoaded.Add(texture);
            }
        }
        
        return textures;
    }
}