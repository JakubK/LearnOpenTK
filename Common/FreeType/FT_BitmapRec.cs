using System;
using System.Runtime.InteropServices;

namespace LearnOpenTK.Common.FreeType;

[StructLayout(LayoutKind.Sequential)]
public struct FT_BitmapRec
{
    public int rows;
    public int width;
    public int pitch;
    public IntPtr buffer;
    public short num_grays;
    public PixelMode pixel_mode;
    public byte palette_mode;
    public IntPtr palette;
}
