using System.Runtime.InteropServices;

namespace LearnOpenTK;

internal static class FT
{
    private const string WindowsLib = "freetype.dll";
    private const string LinuxLib = "libfreetype.so.6";

    [DllImport(WindowsLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_Init_FreeType")]
    private static extern int FT_Init_FreeType_Windows(out IntPtr library);

    [DllImport(LinuxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_Init_FreeType")]
    private static extern int FT_Init_FreeType_Linux(out IntPtr library);

    public static int FT_Init_FreeType(out IntPtr library)
    {
        if (OperatingSystem.IsWindows())
            return FT_Init_FreeType_Windows(out library);
        
        return FT_Init_FreeType_Linux(out library);
    }
    
    [DllImport(WindowsLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_New_Face")]
    private static extern int FT_New_Face_Windows(
        IntPtr library,
        string filepath,
        int faceIndex,
        out IntPtr face);
    
    [DllImport(LinuxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_New_Face")]
    private static extern int FT_New_Face_Linux(
        IntPtr library,
        string filepath,
        int faceIndex,
        out IntPtr face);
    
    public static int FT_New_Face(IntPtr library,
        string filepath,
        int faceIndex,
        out IntPtr face)
    {
        if (OperatingSystem.IsWindows())
            return FT_New_Face_Windows(library, filepath, faceIndex, out face);
        
        return FT_New_Face_Linux(library, filepath, faceIndex, out face);
    }
    
    [DllImport(WindowsLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_Set_Pixel_Sizes")]
    private static extern int FT_Set_Pixel_Sizes_Windows(IntPtr face, int width, int height);

    [DllImport(LinuxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_Set_Pixel_Sizes")]
    private static extern int FT_Set_Pixel_Sizes_Linux(IntPtr face, int width, int height);

    public static int FT_Set_Pixel_Sizes(IntPtr face, int width, int height)
    {
        if (OperatingSystem.IsWindows())
            return FT_Set_Pixel_Sizes_Windows(face, width, height);
        
        return FT_Set_Pixel_Sizes_Linux(face, width, height);
    }
    
    [DllImport(WindowsLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_Load_Char")]
    private static extern int FT_Load_Char_Windows(IntPtr face, ulong charCode, int loadFlags);

    [DllImport(LinuxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_Load_Char")]
    private static extern int FT_Load_Char_Linux(IntPtr face, ulong charCode, int loadFlags);

    public static int FT_Load_Char(IntPtr face, ulong charCode, int loadFlags)
    {
        if (OperatingSystem.IsWindows())
            return FT_Load_Char_Windows(face, charCode, loadFlags);
        
        return FT_Load_Char_Linux(face, charCode, loadFlags);
    }
    
    [DllImport(WindowsLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_Done_Face")]
    private static extern int FT_Done_Face_Windows(IntPtr face);

    [DllImport(LinuxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_Done_Face")]
    private static extern int FT_Done_Face_Linux(IntPtr face);

    public static int FT_Done_Face(IntPtr face)
    {
        if (OperatingSystem.IsWindows())
            return FT_Done_Face_Windows(face);
        
        return FT_Done_Face_Linux(face);
    }
    
    [DllImport(WindowsLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_Done_FreeType")]
    private static extern int FT_Done_FreeType_Windows(IntPtr library);

    [DllImport(LinuxLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FT_Done_FreeType")]
    private static extern int FT_Done_FreeType_Linux(IntPtr library);

    public static int FT_Done_FreeType(IntPtr library)
    {
        if (OperatingSystem.IsWindows())
            return FT_Done_FreeType_Windows(library);
        
        return FT_Done_FreeType_Linux(library);
    }
}