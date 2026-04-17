using System.Runtime.InteropServices;

namespace LearnOpenTK;

[StructLayout(LayoutKind.Sequential)]
public struct FT_BBox
{
    public IntPtr xMin, yMin;
    public IntPtr xMax, yMax;
}