using System;
using System.Runtime.InteropServices;

namespace LearnOpenTK.Common.FreeType;

[StructLayout(LayoutKind.Sequential)]
public struct FT_GlyphSlotRec
{
    public IntPtr library;
    public IntPtr face;
    public IntPtr next;
    public uint reserved;
    public FT_Generic generic;

    public FT_GlyphMetricsRec metrics;
    public IntPtr linearHoriAdvance;
    public IntPtr linearVertAdvance;
    public FT_Vector26Dot6 advance;

    public GlyphFormat format;

    public FT_BitmapRec bitmap;
    public int bitmap_left;
    public int bitmap_top;

    public FT_OutlineRec outline;

    public uint num_subglyphs;
    public IntPtr subglyphs;

    public IntPtr control_data;
    public IntPtr control_len;

    public IntPtr lsb_delta;
    public IntPtr rsb_delta;

    public IntPtr other;

    public IntPtr @internal;
}