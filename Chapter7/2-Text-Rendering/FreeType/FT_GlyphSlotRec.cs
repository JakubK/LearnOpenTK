using System.Runtime.InteropServices;

namespace LearnOpenTK;

[StructLayout(LayoutKind.Sequential)]
internal struct FT_GlyphSlotRec
{
    internal IntPtr library;
    internal IntPtr face;
    internal IntPtr next;
    internal uint reserved;
    internal FT_Generic generic;

    internal FT_GlyphMetricsRec metrics;
    internal IntPtr linearHoriAdvance;
    internal IntPtr linearVertAdvance;
    internal FT_Vector26Dot6 advance;

    internal GlyphFormat format;

    internal FT_BitmapRec bitmap;
    internal int bitmap_left;
    internal int bitmap_top;

    internal FT_OutlineRec outline;

    internal uint num_subglyphs;
    internal IntPtr subglyphs;

    internal IntPtr control_data;
    internal IntPtr control_len;

    internal IntPtr lsb_delta;
    internal IntPtr rsb_delta;

    internal IntPtr other;

    private IntPtr @internal;
}