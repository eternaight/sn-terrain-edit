using System;
using System.Runtime.InteropServices;
public unsafe static class TextureDecoder
{
    const string dllName = "Texture2DDecoderNative";

    static TextureDecoder() {
        var dllPath = "C:\\Users\\komet\\Desktop\\assstudio" + (Environment.Is64BitProcess ? "\\x64" : "\\x86");
        AssetStudio.PInvoke.DllLoader.PreloadDll(dllName, dllPath);
    }

    // TextureDecoder2D functionality
    public static byte[] DecodeTextureDXT1(byte[] image_data, int m_Width, int m_Height) {
        var buff = new byte[m_Width * m_Height * 4];
        if (!TextureDecoder.DecodeDXT1(image_data, m_Width, m_Height, buff)) {
            return null;
        }
        return buff;
    }

    public static byte[] DecodeTextureDXT5(byte[] image_data, int m_Width, int m_Height) {
        var buff = new byte[m_Width * m_Height * 4];
        if (!TextureDecoder.DecodeDXT5(image_data, m_Width, m_Height, buff)) {
            return null;
        }
        return buff;
    }
    //end


    public static bool DecodeDXT1(byte[] data, int width, int height, byte[] image) {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return DecodeDXT1(pData, width, height, pImage);
            }
        }
    }
    public static bool DecodeDXT5(byte[] data, int width, int height, byte[] image)
    {
        fixed (byte* pData = data) {
            fixed (byte* pImage = image) {
                return DecodeDXT5(pData, width, height, pImage);
            }
        }
    }


    [DllImport(dllName, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DecodeDXT1(void* data, int width, int height, void* image);

    [DllImport(dllName, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DecodeDXT5(void* data, int width, int height, void* image);
}
