using System.Runtime.InteropServices;

namespace LearnOpenTK;

[StructLayout(LayoutKind.Sequential)]
internal struct FT_BitmapRec
{
    internal int rows;
    internal int width;
    internal int pitch;
    internal IntPtr buffer;
    internal short num_grays;
    internal PixelMode pixel_mode;
    internal byte palette_mode;
    internal IntPtr palette;
}
