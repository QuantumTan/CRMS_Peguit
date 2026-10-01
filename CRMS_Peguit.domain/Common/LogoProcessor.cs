using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;

namespace CRMS_Peguit.domain.Common
{
    [SupportedOSPlatform("windows")]
    public static class LogoProcessor
    {
        public const int MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB
        public const int MaxDimensionPixels = 4096;
        public const int TargetNormalizedDimension = 256;

        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF };

        /// <summary>
        /// Validates actual image format (PNG or JPG), magic bytes, size limits, and decodability.
        /// Re-encodes the image to a clean, normalized PNG (max 256x256), stripping all embedded metadata.
        /// </summary>
        public static (bool Success, byte[]? NormalizedBytes, string? ErrorMessage) ProcessAndNormalize(byte[]? rawBytes)
        {
            if (rawBytes == null || rawBytes.Length == 0)
            {
                return (false, null, "Image file is empty.");
            }

            if (rawBytes.Length > MaxFileSizeBytes)
            {
                return (false, null, "Image file exceeds the maximum allowed size of 2 MB.");
            }

            // Verify Magic Bytes
            bool isPng = HasHeader(rawBytes, PngHeader);
            bool isJpeg = HasHeader(rawBytes, JpegHeader);

            if (!isPng && !isJpeg)
            {
                return (false, null, "Invalid image format. Only genuine PNG and JPG files are supported.");
            }

            try
            {
                using var inStream = new MemoryStream(rawBytes);
                using var image = Image.FromStream(inStream, useEmbeddedColorManagement: false, validateImageData: true);

                if (image.Width <= 0 || image.Height <= 0)
                {
                    return (false, null, "Image dimensions are invalid.");
                }

                if (image.Width > MaxDimensionPixels || image.Height > MaxDimensionPixels)
                {
                    return (false, null, $"Image resolution ({image.Width}x{image.Height}) exceeds maximum allowed limit of {MaxDimensionPixels}x{MaxDimensionPixels} pixels.");
                }

                // Calculate scaled dimensions (max 256x256 while preserving aspect ratio)
                int targetW = image.Width;
                int targetH = image.Height;

                if (targetW > TargetNormalizedDimension || targetH > TargetNormalizedDimension)
                {
                    float ratio = Math.Min((float)TargetNormalizedDimension / targetW, (float)TargetNormalizedDimension / targetH);
                    targetW = Math.Max(1, (int)(targetW * ratio));
                    targetH = Math.Max(1, (int)(targetH * ratio));
                }

                // Re-encode into clean 32-bit ARGB PNG, stripping all EXIF/metadata
                using var normalizedBitmap = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(normalizedBitmap))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.DrawImage(image, 0, 0, targetW, targetH);
                }

                using var outStream = new MemoryStream();
                normalizedBitmap.Save(outStream, ImageFormat.Png);
                return (true, outStream.ToArray(), null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Image validation failed: {ex.Message}");
            }
        }

        private static bool HasHeader(byte[] data, byte[] header)
        {
            if (data.Length < header.Length) return false;
            for (int i = 0; i < header.Length; i++)
            {
                if (data[i] != header[i]) return false;
            }
            return true;
        }
    }
}
