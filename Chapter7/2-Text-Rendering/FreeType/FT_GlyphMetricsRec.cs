using System.Runtime.InteropServices;

namespace LearnOpenTK;

[StructLayout(LayoutKind.Sequential)]
internal struct FT_GlyphMetricsRec
{
    internal IntPtr width;
    internal IntPtr height;

    internal IntPtr horiBearingX;
    internal IntPtr horiBearingY;
    internal IntPtr horiAdvance;

    internal IntPtr vertBearingX;
    internal IntPtr vertBearingY;
    internal IntPtr vertAdvance;
}