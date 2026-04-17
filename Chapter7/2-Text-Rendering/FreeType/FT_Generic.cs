using System.Runtime.InteropServices;

namespace LearnOpenTK;

[StructLayout(LayoutKind.Sequential)]
public struct FT_Generic
{
    public IntPtr data;
    public IntPtr finalizer;
}