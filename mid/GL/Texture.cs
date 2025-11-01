using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace midtermGame.Graphics
{
    // Simple texture loader using StbImageSharp. Handles SRGB vs linear internal formats
    // which is important for correct lighting when Framebuffer sRGB is enabled.
    public sealed class Texture
    {
        public int Handle { get; private set; }

        public static Texture Load2D(string path, bool srgb = false)
        {
            using var s = File.OpenRead(path);
            var img = ImageResult.FromStream(s, ColorComponents.RedGreenBlueAlpha);

            int handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, handle);

            var ifmt = srgb ? PixelInternalFormat.SrgbAlpha : PixelInternalFormat.Rgba;

            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            var pinned = GCHandle.Alloc(img.Data, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = pinned.AddrOfPinnedObject();
                GL.TexImage2D(TextureTarget.Texture2D, level: 0, internalformat: ifmt,
                              width: img.Width, height: img.Height, border: 0,
                              format: PixelFormat.Rgba, type: PixelType.UnsignedByte, pixels: ptr);
            }
            finally
            {
                pinned.Free();
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            try
            {
                GL.GetFloat((GetPName)0x84FF, out float maxAniso);
                if (maxAniso > 0f)
                {
                    float desired = MathF.Min(maxAniso, 8f);
                    GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)0x84FE, desired);
                }
            }
            catch {  }

            return new Texture { Handle = handle };
        }

        public void Bind(TextureUnit unit)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }
        public static Texture CreateSolid(int width = 4, int height = 4,
                                          byte r = 255, byte g = 255, byte b = 255, byte a = 255,
                                          bool srgb = false)
        {
            int handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, handle);

            var ifmt = srgb ? PixelInternalFormat.SrgbAlpha : PixelInternalFormat.Rgba;
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            byte[] data = new byte[width * height * 4];
            for (int i = 0; i < data.Length; i += 4)
            {
                data[i + 0] = r;
                data[i + 1] = g;
                data[i + 2] = b;
                data[i + 3] = a;
            }

            var pinned = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = pinned.AddrOfPinnedObject();
                GL.TexImage2D(TextureTarget.Texture2D, level: 0, internalformat: ifmt,
                              width: width, height: height, border: 0,
                              format: PixelFormat.Rgba, type: PixelType.UnsignedByte, pixels: ptr);
            }
            finally
            {
                pinned.Free();
            }

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return new Texture { Handle = handle };
        }

        public static Texture CreateSolidColor(byte r = 255, byte g = 255, byte b = 255, byte a = 255, bool srgb = false)
        {
            return CreateSolid(1, 1, r, g, b, a, srgb);
        }
    }
}
