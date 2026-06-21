using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LearnOpenTK.Common;

public class ComputeShader
{
    public readonly int Handle;
    private readonly Dictionary<string, int> _uniformLocations;

    public ComputeShader(string path)
    {
        _uniformLocations = new();
        var shaderSource = File.ReadAllText(path);
        var computeShader = GL.CreateShader(ShaderType.ComputeShader);
        
        // Now, bind the GLSL source code
        GL.ShaderSource(computeShader, shaderSource);
        
        // And then compile
        CompileShader(computeShader);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, computeShader);
        GL.LinkProgram(Handle);
        GL.DeleteShader(computeShader);
        
        // The shader is now ready to go, but first, we're going to cache all the shader uniform locations.
        // Querying this from the shader is very slow, so we do it once on initialization and reuse those values
        // later.

        // First, we have to get the number of active uniforms in the shader.
        GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

        // Next, allocate the dictionary to hold the locations.
        _uniformLocations = new Dictionary<string, int>();

        // Loop over all the uniforms,
        for (var i = 0; i < numberOfUniforms; i++)
        {
            // get the name of this uniform,
            var key = GL.GetActiveUniform(Handle, i, out var size, out var uniformType);
            if (size == 1)
            {
                // get the location,
                var location = GL.GetUniformLocation(Handle, key);
                // and then add it to the dictionary.
                _uniformLocations.Add(key, location);
            }
            else
            {
                // Most likely array type, needs special handler to register all locations
                for (int j = 0; j < size; j++)
                {
                    var newKey = key.Replace("[0]", $"[{j}]");
                    // get the location,
                    var location = GL.GetUniformLocation(Handle, newKey);
                    _uniformLocations.Add(newKey, location);
                }
            }
        }
    }
    
    private static void CompileShader(int shader)
    {
        // Try to compile the shader
        GL.CompileShader(shader);

        // Check for compilation errors
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
        if (code != (int)All.True)
        {
            // We can use `GL.GetShaderInfoLog(shader)` to get information about the error.
            var infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
        }
    }
    
    // A wrapper function that enables the shader program.
    public ComputeShader Use()
    {
        GL.UseProgram(Handle);
        return this;
    }

    // The shader sources provided with this project use hardcoded layout(location)-s. If you want to do it dynamically,
    // you can omit the layout(location=X) lines in the vertex shader, and use this in VertexAttribPointer instead of the hardcoded values.
    public int GetAttribLocation(string attribName)
    {
        return GL.GetAttribLocation(Handle, attribName);
    }

    // Uniform setters
    // Uniforms are variables that can be set by user code, instead of reading them from the VBO.
    // You use VBOs for vertex-related data, and uniforms for almost everything else.

    // Setting a uniform is almost always the exact same, so I'll explain it here once, instead of in every method:
    //     1. Bind the program you want to set the uniform on
    //     2. Get a handle to the location of the uniform with GL.GetUniformLocation.
    //     3. Use the appropriate GL.Uniform* function to set the uniform.

    /// <summary>
    /// Set a uniform int on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetInt(string name, int data)
    {
        GL.UseProgram(Handle);
        GL.Uniform1(_uniformLocations[name], data);
    }

    /// <summary>
    /// Set a uniform bool on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetBool(string name, bool data)
    {
        GL.UseProgram(Handle);
        GL.Uniform1(_uniformLocations[name], data ? 1 : 0);
    }

    /// <summary>
    /// Set a uniform float on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetFloat(string name, float data)
    {
        GL.UseProgram(Handle);
        GL.Uniform1(_uniformLocations[name], data);
    }

    /// <summary>
    /// Set a uniform Matrix4 on this shader
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <remarks>
    ///   <para>
    ///   The matrix is transposed before being sent to the shader.
    ///   </para>
    /// </remarks>
    public void SetMatrix4(string name, Matrix4 data)
    {
        GL.UseProgram(Handle);
        GL.UniformMatrix4(_uniformLocations[name], false, ref data);
    }
    
    /// <summary>
    /// Set a uniform Matrix3 on this shader
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <remarks>
    ///   <para>
    ///   The matrix is transposed before being sent to the shader.
    ///   </para>
    /// </remarks>
    public void SetMatrix3(string name, Matrix3 data)
    {
        GL.UseProgram(Handle);
        GL.UniformMatrix3(_uniformLocations[name], false, ref data);
    }

    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetVector3(string name, Vector3 data)
    {
        GL.UseProgram(Handle);
        GL.Uniform3(_uniformLocations[name], data);
    }
    
    /// <summary>
    /// Set a uniform Vector2 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetVector2(string name, Vector2 data)
    {
        GL.UseProgram(Handle);
        GL.Uniform2(_uniformLocations[name], data);
    }
    
    /// <summary>
    /// Set a uniform Vector4 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    public void SetVector4(string name, Vector4 data)
    {
        GL.UseProgram(Handle);
        GL.Uniform4(_uniformLocations[name], data);
    }
}