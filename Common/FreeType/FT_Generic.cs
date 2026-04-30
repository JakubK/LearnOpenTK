using System;
using System.Runtime.InteropServices;

namespace LearnOpenTK.Common.FreeType;

[StructLayout(LayoutKind.Sequential)]
public struct FT_Generic
{
    public IntPtr data;
    public IntPtr finalizer;
}