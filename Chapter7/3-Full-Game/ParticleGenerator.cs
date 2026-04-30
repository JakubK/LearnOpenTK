using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LearnOpenTK;

public class ParticleGenerator
{
    private List<Particle> particles = new();
    private int amount;

    private Shader shader;
    private Texture2D texture;

    private int vao;

    private int lastUsedParticle = 0;
    
    public ParticleGenerator(Shader shader, Texture2D texture, int amount)
    {
        this.shader = shader;
        this.texture = texture;
        this.amount = amount;
        
        Init();
    }

    public void Update(float dt, GameObject gameObject, int newParticles, Vector2 offset)
    {
        // add new particles
        for (int i = 0; i < newParticles; ++i)
        {
            var unusedParticle = FirstUnusedParticle();
            var part = particles[unusedParticle];
            RespawnParticle(ref part, gameObject, offset);
        }
        
        // update all particles
        for (int i = 0; i < amount; ++i)
        {
            var p = particles[i];
            p.Life -= dt;

            if (p.Life > 0f)
            {
                p.Position -= p.Velocity * dt;
                p.Color.W -= dt * 2.5f;
            }
        }
    }

    public void Draw()
    {
        // use additive blending to give it a 'glow' effect
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
        shader.Use();

        foreach (var particle in particles)
        {
            if (particle.Life > 0f)
            {
                shader.SetVector2("offset", particle.Position);
                shader.SetVector4("color", particle.Color);
                
                texture.Bind();
                
                GL.BindVertexArray(vao);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
                GL.BindVertexArray(0);
            }
        }
        
        // don't forget to reset to default blending mode
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    private void Init()
    {
        // set up mesh and attribute properties
        float[] particleQuad = {
            0.0f, 1.0f, 0.0f, 1.0f,
            1.0f, 0.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f,

            0.0f, 1.0f, 0.0f, 1.0f,
            1.0f, 1.0f, 1.0f, 1.0f,
            1.0f, 0.0f, 1.0f, 0.0f
        };

        vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();
        
        GL.BindVertexArray(vao);
        
        // fill mesh buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * particleQuad.Length, particleQuad, BufferUsageHint.StaticDraw);
        
        // set mesh attributes
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.BindVertexArray(0);

        // create this->amount default particle instances
        for (int i = 0; i < amount; ++i)
        {
            particles.Add(new ());
        }
    }

    private int FirstUnusedParticle()
    {
        // first search from last used particle, this will usually return almost instantly
        for (int i = lastUsedParticle; i < amount; ++i)
        {
            if (particles[i].Life <= 0)
            {
                lastUsedParticle = i;
                return i;
            }
        }
        
        // otherwise, do a linear search
        for (int i = 0; i < amount; ++i)
        {
            if (particles[i].Life <= 0)
            {
                lastUsedParticle = i;
                return i;
            }
        }

        // all particles are taken, override the first one (note that if it repeatedly hits this case, more particles should be reserved)
        lastUsedParticle = 0;
        return 0;
    }

    private void RespawnParticle(ref Particle particle, GameObject gameObject, Vector2 offset)
    {
        var r = new Random();
        var random = (r.Next() % 100 - 50) / 10.0f;
        var rColor = 0.5f + r.Next() % 100 / 100.0f;

        particle.Position = gameObject.Position + new Vector2(random, random) + offset;
        particle.Color = new Vector4(rColor, rColor, rColor, 1);
        particle.Life = 1.0f;

        particle.Velocity = new (gameObject.Velocity.X * 0.1f, gameObject.Velocity.Y * 0.1f);
    }
}