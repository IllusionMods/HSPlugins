using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace VideoExport.Core
{
    static class TextureEncoder
    {
        public static void EncodeAndWriteTexture(Texture2D texture, VideoExport.ImgFormat format, string savePath)
        {
            byte[] bytes = EncodeTexture(texture, format);
            if (bytes != null)
                File.WriteAllBytes(savePath, bytes);
        }

        public static byte[] EncodeTexture(Texture2D texture, VideoExport.ImgFormat format)
        {
            byte[] bytes = null;
            switch (format)
            {
                default:
                case VideoExport.ImgFormat.BMP:
                    bytes = EncodeToBMP(texture, (int)texture.width, (int)texture.height);
                    break;
                case VideoExport.ImgFormat.PNG:
                    bytes = texture.EncodeToPNG();
                    break;
                case VideoExport.ImgFormat.EXR:
                    bytes = texture.EncodeToEXR();
                    break;
            }
            return bytes;
        }

        private static byte[] EncodeToBMP(Texture2D texture, int width, int height)
        {
            byte[] fileBytes = new byte[0];

            unsafe
            {
                uint byteSize = (uint)(width * height * 3);
                uint fileSize = (uint)(_bmpHeader.Length + byteSize);
                if (fileBytes.Length != fileSize)
                {
                    fileBytes = new byte[fileSize];
                    Array.Copy(_bmpHeader, fileBytes, _bmpHeader.Length);

                    fileBytes[2] = ((byte*)&fileSize)[0];
                    fileBytes[3] = ((byte*)&fileSize)[1];
                    fileBytes[4] = ((byte*)&fileSize)[2];
                    fileBytes[5] = ((byte*)&fileSize)[3];

                    fileBytes[18] = ((byte*)&width)[0];
                    fileBytes[19] = ((byte*)&width)[1];
                    fileBytes[20] = ((byte*)&width)[2];
                    fileBytes[21] = ((byte*)&width)[3];

                    fileBytes[22] = ((byte*)&height)[0];
                    fileBytes[23] = ((byte*)&height)[1];
                    fileBytes[24] = ((byte*)&height)[2];
                    fileBytes[25] = ((byte*)&height)[3];

                    fileBytes[34] = ((byte*)&byteSize)[0];
                    fileBytes[35] = ((byte*)&byteSize)[1];
                    fileBytes[36] = ((byte*)&byteSize)[2];
                    fileBytes[37] = ((byte*)&byteSize)[3];
                }

                int i = _bmpHeader.Length;
                Color32[] pixels = texture.GetPixels32();
                foreach (Color32 c in pixels)
                {
                    fileBytes[i++] = c.b;
                    fileBytes[i++] = c.g;
                    fileBytes[i++] = c.r;
                }
                return fileBytes;
            }
        }

        private static readonly byte[] _bmpHeader = {
            0x42, 0x4D,
            0, 0, 0, 0,
            0, 0,
            0, 0,
            54, 0, 0, 0,
            40, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            1, 0,
            24, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0
        };
    }
}
