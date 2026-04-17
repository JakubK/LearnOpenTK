using System.Runtime.InteropServices;
using LearnOpenTK.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using Camera = LearnOpenTK.Common.Camera;

namespace LearnOpenTK
{
    public class Window : GameWindow
    {
        private Shader shader;
        private Camera _camera;
        private int vao;
        private int vbo;

        private bool _firstMove = true;

        private Dictionary<char, Character> Characters = new();

        private Vector2 _lastPos;
        
        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings) {}

        protected override void OnLoad()
        {
            base.OnLoad();
            _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            shader = new Shader("Shaders/text.vs", "Shaders/text.fs");
            shader.Use();
            
            var projection = Matrix4.CreateOrthographicOffCenter(
                0.0f, 800,
                0.0f, 600,
                -1.0f, 1.0f
            );
            shader.SetMatrix4("projection", projection);
            
            // then before rendering, configure the viewport to the original framebuffer's screen dimensions
            GL.Viewport(0, 0, Size.X, Size.Y);
            
            // FreeType
            // All functions return a value different than 0 whenever an error occurred
            if (FT.FT_Init_FreeType(out var ft) != 0 || ft == IntPtr.Zero)
            {
                Console.WriteLine("ERROR::FREETYPE: Could not init FreeType Library");
                Environment.Exit(-1);
            }

            // load font as face
            if (FT.FT_New_Face(ft, "Resources/Antonio-Bold.ttf", 0, out var face) != 0 || face == IntPtr.Zero)
            {
                Console.WriteLine("ERROR::FREETYPE: Failed to load font");
                Environment.Exit(-1);
            }

            // set size to load glyphs as
            var setPixelSizeResult = FT.FT_Set_Pixel_Sizes(face, 0, 48);
            
            // disable byte-alignment restriction
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            
            // load first 128 characters of ASCII set
            for (int c = 0; c < 128; c++)
            {
                // Load character glyph 
                var loadCharResult = FT.FT_Load_Char(face, (uint)c, (int)FtLoad.Render);
                if (loadCharResult != 0)
                {
                    Console.WriteLine("ERROR::FREETYPE: Failed to load Glyph");
                }
                
                // generate texture
                var texture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, texture);
                
                var faceRec = Marshal.PtrToStructure<FT_FaceRec>(face);
                var glyph = Marshal.PtrToStructure<GlyphSlotRec>(faceRec.glyph);
                var bitmap = glyph.bitmap;
                
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, (int)bitmap.width, (int)bitmap.rows, 0, PixelFormat.Red, PixelType.UnsignedByte, bitmap.buffer);
                
                // set texture options
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                var character = new Character()
                {
                    TextureId = texture,
                    Size = new(bitmap.width, bitmap.rows),
                    Bearing = new(glyph.bitmap_left, glyph.bitmap_top),
                    Advance = (int)glyph.advance.x,
                };
                Characters[(char)c] = character;
            }
            
            // Destroy FreeType once we're done
            FT.FT_Done_Face(face);
            FT.FT_Done_FreeType(ft);
            
            
            // Configure vao/vbo for texture quads
            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 6 * 4, 0, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            
            GL.ClearColor(0.2f,0.3f,0.3f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            RenderText(shader, "This is sample text", 25f, 25f, 1f, new(0.5f, 0.8f, 0.2f));
            RenderText(shader, "(C) LearnOpenGL.com", 540.0f, 570.0f, 0.5f, new(0.3f, 0.7f, 0.9f));

            SwapBuffers();
        }

        private void RenderText(Shader shader, string text, float x, float y, float scale, Vector3 color)
        {
            shader.Use();
            shader.SetVector3("textColor", color);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindVertexArray(vao);

            // Iterate over all characters in given text
            foreach (var c in text)
            {
                var ch = Characters[c];

                var xpos = x + ch.Bearing.X * scale;
                var ypos = y - (ch.Size.Y - ch.Bearing.Y) * scale;

                var w = ch.Size.X * scale;
                var h = ch.Size.Y * scale;
                
                // Update VBO for each character
                float[] vertices =
                {
                    xpos, ypos + h, 0.0f, 0.0f,
                    xpos, ypos, 0.0f, 1.0f,
                    xpos + w, ypos, 1.0f, 1.0f,

                    xpos, ypos + h, 0.0f, 0.0f,
                    xpos + w, ypos, 1.0f, 1.0f,
                    xpos + w, ypos + h, 1.0f, 0.0f
                };
                
                // render glyph texture over quad
                GL.BindTexture(TextureTarget.Texture2D, ch.TextureId);
                
                // update content of VBO memory
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.BufferSubData(BufferTarget.ArrayBuffer, 0, vertices.Length * sizeof(float), vertices); // be sure to use glBufferSubData and not glBufferData
                
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                // render quad
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
                // now advance cursors for next glyph (note that advance is number of 1/64 pixels)
                x += (ch.Advance >> 6) * scale; // bitshift by 6 to get value in pixels (2^6 = 64 (divide amount of 1/64th pixels by 64 to get amount of pixels))
            }
            
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
        
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (!IsFocused)
            {
                return;
            }

            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            _camera.AspectRatio = Size.X / (float)Size.Y;
        }
    }
}
