using System.Runtime.InteropServices;

namespace LearnOpenTK;

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