using System;
using System.Runtime.InteropServices;

namespace LearnOpenTK.Common.FreeType;

[StructLayout(LayoutKind.Sequential)]
public struct FT_BBox
{
    public IntPtr xMin, yMin;
    public IntPtr xMax, yMax;
}