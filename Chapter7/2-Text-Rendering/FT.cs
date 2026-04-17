using System.Runtime.InteropServices;

namespace LearnOpenTK;

[StructLayout(LayoutKind.Sequential)]
public struct FT_BBox
{
    public IntPtr xMin, yMin;
    public IntPtr xMax, yMax;
}

public struct FT_Generic
{
    public IntPtr data;
    public IntPtr finalizer;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GlyphMetricsRec
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

[StructLayout(LayoutKind.Sequential)]
internal struct FTVector26Dot6
{
    internal IntPtr x;
    internal IntPtr y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct BitmapRec
{
    internal int rows;
    internal int width;
    internal int pitch;
    internal IntPtr buffer;
    internal short num_grays;
    internal PixelMode pixel_mode;
    internal byte palette_mode;
    internal IntPtr palette;
}

[StructLayout(LayoutKind.Sequential)]
internal struct OutlineRec
{
    internal short n_contours;
    internal short n_points;

    internal IntPtr points;
    internal IntPtr tags;
    internal IntPtr contours;

    internal OutlineFlags flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GlyphSlotRec
{
    internal IntPtr library;
    internal IntPtr face;
    internal IntPtr next;
    internal uint reserved;
    internal FT_Generic generic;

    internal GlyphMetricsRec metrics;
    internal IntPtr linearHoriAdvance;
    internal IntPtr linearVertAdvance;
    internal FTVector26Dot6 advance;

    internal GlyphFormat format;

    internal BitmapRec bitmap;
    internal int bitmap_left;
    internal int bitmap_top;

    internal OutlineRec outline;

    internal uint num_subglyphs;
    internal IntPtr subglyphs;

    internal IntPtr control_data;
    internal IntPtr control_len;

    internal IntPtr lsb_delta;
    internal IntPtr rsb_delta;

    internal IntPtr other;

    private IntPtr @internal;
}


[StructLayout(LayoutKind.Sequential)]
public struct FT_FaceRec
{
    public IntPtr num_faces;
    public IntPtr face_index;
    public IntPtr face_flags;
    public IntPtr style_flags;
    public IntPtr num_glyphs;

    public string family_name;
    public string style_name;

    public int num_fixed_sizes;
    public IntPtr available_sizes;

    public int num_charmaps;
    public IntPtr charmaps;

    public FT_Generic generic;

    public FT_BBox bbox;

    public ushort units_per_EM;
    public short ascender;
    public short descender;
    public short height;

    public short max_advance_width;
    public short max_advance_height;

    public short underline_position;
    public short underline_thickness;

    public IntPtr glyph;
    public IntPtr size;
    public IntPtr charmap;

    public IntPtr driver;
    public IntPtr memory;
    public IntPtr stream;

    public IntPtr sizes_list;
    public IntPtr autohint;

    public IntPtr extensions;
    public IntPtr @internal;
}


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