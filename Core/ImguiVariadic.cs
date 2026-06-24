using System.Runtime.InteropServices;
using SharpGen.Runtime;

namespace ExileCore;
public static class ImguiVariadic
{
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
    public unsafe static extern void igTextWrapped(byte* fmt, __arglist);
    public unsafe static void TextWrappedUnformatted(string text)
    {
        igTextWrapped("%s\0"u8.GetPointerUnsafe(), __arglist(text));
    }

    public unsafe static void TextWrappedUnformatted(string text1, string text2)
    {
        igTextWrapped("%s%s\0"u8.GetPointerUnsafe(), __arglist(text1, text2));
    }
}