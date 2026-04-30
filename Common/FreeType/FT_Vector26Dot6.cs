using System;
using System.Runtime.InteropServices;

namespace LearnOpenTK.Common.FreeType;

[StructLayout(LayoutKind.Sequential)]
public struct FT_Vector26Dot6
{
    public IntPtr x;
    public IntPtr y;
}