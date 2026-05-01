using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;

namespace LearnOpenTK;

public class PostProcessor
{
    public Shader PostProcessingShader;
    public Texture2D Texture = new ();
    public int Width;
    public int Height;

    public bool Confuse;
    public bool Chaos;
    public bool Shake;

    private int msfbo; // MSFBO = Multisampled FBO. FBO is regular, used for blitting MS color-buffer to texture
    private int fbo;
    private int rbo; // RBO is used for multisampled color buffer
    private int vao;
    
    public PostProcessor(Shader shader, int width, int height)
    {
        PostProcessingShader = shader;
        Width = width;
        Height = height;
        Confuse = false;
        Chaos = false;
        Shake = false;
        
        // initialize renderbuffer/framebuffer object
        msfbo = GL.GenFramebuffer();
        fbo = GL.GenFramebuffer();
        rbo = GL.GenRenderbuffer();
        // initialize renderbuffer storage with a multisampled color buffer (don't need a depth/stencil buffer)
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, msfbo);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
        
        GL.RenderbufferStorageMultisample(
            RenderbufferTarget.Renderbuffer,
            4,
            RenderbufferStorage.Rgb8,
            width,
            height
        );        
        
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, rbo); // attach MS render buffer object to framebuffer
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            Console.WriteLine("ERROR::POSTPROCESSOR);: Failed to initialize MSFBO");
        // also initialize the FBO/texture to blit multisampled color-buffer to; used for shader operations (for postprocessing effects)
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        Texture.Generate(width, height, null);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, Texture.ID, 0); // attach texture to framebuffer as its color attachment
        if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
        {
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            Console.WriteLine(status);
            Console.WriteLine("ERROR::POSTPROCESSOR: Failed to initialize FBO");
        }
        
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        // initialize render data and uniforms
        InitRenderData();
        PostProcessingShader.SetInt("scene", 0);
        float offset = 1.0f / 300.0f;
        var offsets = new []{
             -offset,  offset  ,  // top-left
              0.0f,    offset  ,  // top-center
              offset,  offset  ,  // top-right
             -offset,  0.0f    ,  // center-left
              0.0f,    0.0f    ,  // center-center
              offset,  0.0f    ,  // center - right
             -offset, -offset,  // bottom-left
              0.0f,   -offset,  // bottom-center
              offset, -offset     // bottom-right    
        };
        
        
        GL.Uniform2(GL.GetUniformLocation(PostProcessingShader.Handle, "offsets"), 9, offsets);
        
        var edge_kernel = new []{
            -1, -1, -1,
            -1,  8, -1,
            -1, -1, -1
        };
        GL.Uniform1(GL.GetUniformLocation(PostProcessingShader.Handle, "edge_kernel"), 9, edge_kernel);
        
        var blur_kernel = new []{
            1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f,
            2.0f / 16.0f, 4.0f / 16.0f, 2.0f / 16.0f,
            1.0f / 16.0f, 2.0f / 16.0f, 1.0f / 16.0f
        };
        GL.Uniform1(GL.GetUniformLocation(PostProcessingShader.Handle, "blur_kernel"), 9, blur_kernel);   
    }

    public void BeginRender()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, msfbo);
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void EndRender()
    {
        // now resolve multisampled color-buffer into intermediate FBO to store to texture
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, msfbo);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fbo);
        GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0); // binds both READ and WRITE framebuffer to default framebuffer
    }

    public void Render(float time)
    {
        // set uniforms/options
        PostProcessingShader.Use();
        PostProcessingShader.SetFloat("time", time);
        PostProcessingShader.SetBool("confuse", Confuse);
        PostProcessingShader.SetBool("chaos", Chaos);
        PostProcessingShader.SetBool("shake", Shake);
        // render textured quad
        GL.ActiveTexture(TextureUnit.Texture0);
        Texture.Bind();	
        GL.BindVertexArray(vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        GL.BindVertexArray(0);
    }

    private void InitRenderData()
    {
        // configure VAO/VBO
        float[] vertices = {
            // pos        // tex
            -1.0f, -1.0f, 0.0f, 0.0f,
            1.0f,  1.0f, 1.0f, 1.0f,
            -1.0f,  1.0f, 0.0f, 1.0f,

            -1.0f, -1.0f, 0.0f, 0.0f,
            1.0f, -1.0f, 1.0f, 0.0f,
            1.0f,  1.0f, 1.0f, 1.0f
        };
        vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * vertices.Length, vertices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(vao);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

}