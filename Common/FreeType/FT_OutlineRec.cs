using System;
using System.Runtime.InteropServices;

namespace LearnOpenTK.Common.FreeType;

[StructLayout(LayoutKind.Sequential)]
public struct FT_OutlineRec
{
    internal short n_contours;
    internal short n_points;

    internal IntPtr points;
    internal IntPtr tags;
    internal IntPtr contours;

    internal OutlineFlags flags;
}