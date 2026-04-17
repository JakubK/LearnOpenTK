using System.Runtime.InteropServices;

namespace LearnOpenTK;

[StructLayout(LayoutKind.Sequential)]
internal struct FT_OutlineRec
{
    internal short n_contours;
    internal short n_points;

    internal IntPtr points;
    internal IntPtr tags;
    internal IntPtr contours;

    internal OutlineFlags flags;
}