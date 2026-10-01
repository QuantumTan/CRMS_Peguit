using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CRMS_Peguit.winforms.Models.Services
{
    /// <summary>
    /// Central provider for application branding assets, including the brand logo and window icon.
    /// </summary>
    public static class AppBrand
    {
        private static readonly object _lock = new();
        private static byte[]? _logoBytes;
        private static Icon? _appIcon;
        private static bool _initialized;

        /// <summary>
        /// Creates a completely independent, standalone GDI+ Bitmap from a byte array.
        /// Does NOT keep any stream open, preventing GDI+ "Parameter is not valid" crashes
        /// when Windows Forms controls or ImageAnimator inspect or animate the image.
        /// </summary>
        public static Bitmap? CreateBitmapFromBytes(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            try
            {
                using var ms = new MemoryStream(bytes);
                using var temp = Image.FromStream(ms);
                return new Bitmap(temp);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the brand logo image as a fresh, independent Bitmap instance.
        /// </summary>
        public static Image? Logo
        {
            get
            {
                lock (_lock)
                {
                    EnsureLoaded();
                    return CreateBitmapFromBytes(_logoBytes);
                }
            }
        }

        /// <summary>
        /// Gets the raw brand logo bytes.
        /// </summary>
        public static byte[]? LogoBytes
        {
            get
            {
                lock (_lock)
                {
                    EnsureLoaded();
                    return _logoBytes != null ? (byte[])_logoBytes.Clone() : null;
                }
            }
        }

        /// <summary>
        /// Gets the application icon derived from the brand logo.
        /// Always returns an independent, non-disposed Icon instance.
        /// </summary>
        public static Icon? AppIcon
        {
            get
            {
                lock (_lock)
                {
                    EnsureLoaded();
                    if (_appIcon != null)
                    {
                        try
                        {
                            // Test if the handle is valid
                            _ = _appIcon.Handle;
                            return (Icon)_appIcon.Clone();
                        }
                        catch
                        {
                            _appIcon = null;
                            _initialized = false;
                            EnsureLoaded();
                            if (_appIcon != null)
                            {
                                try
                                {
                                    return (Icon)_appIcon.Clone();
                                }
                                catch { }
                            }
                        }
                    }
                    return null;
                }
            }
        }

        public static void ApplyAppIcon(Form form)
        {
            if (form is null) return;

            try
            {
                var icon = AppIcon;
                if (icon != null)
                {
                    form.Icon = icon;
                }
            }
            catch
            {
                // Silently ignore if OS icon creation is not supported
            }

            ApplyDarkTitleBar(form);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        /// <summary>
        /// Applies custom dark chrome title bar (#0F172A) with white caption text via DWM API,
        /// matching the left navigation sidebar and eliminating OS purple/magenta title bars.
        /// </summary>
        public static void ApplyDarkTitleBar(Form form, Color? captionColor = null, Color? textColor = null)
        {
            if (form is null) return;

            void Apply()
            {
                try
                {
                    if (Environment.OSVersion.Version.Major >= 10 && form.IsHandleCreated)
                    {
                        int trueValue = 1;
                        DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, sizeof(int));

                        Color cap = captionColor ?? Color.FromArgb(15, 23, 42); // Slate 900 (#0F172A)
                        int colorRef = (cap.B << 16) | (cap.G << 8) | cap.R;
                        DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref colorRef, sizeof(int));

                        Color txt = textColor ?? Color.White;
                        int textColorRef = (txt.B << 16) | (txt.G << 8) | txt.R;
                        DwmSetWindowAttribute(form.Handle, DWMWA_TEXT_COLOR, ref textColorRef, sizeof(int));
                    }
                }
                catch
                {
                    // Ignore on non-Windows 11 or if unsupported by OS
                }
            }

            if (form.IsHandleCreated)
            {
                Apply();
            }
            else
            {
                form.HandleCreated += (_, _) => Apply();
            }
        }

        private static void EnsureLoaded()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                _logoBytes = LoadLogoBytes();
                if (_logoBytes != null && _logoBytes.Length > 0)
                {
                    using var bmp = CreateBitmapFromBytes(_logoBytes);
                    if (bmp != null)
                    {
                        _appIcon = CreateIconFromBitmap(bmp);
                    }
                }
            }
            catch
            {
                // Fallback to null if loading fails
            }
        }

        private static byte[]? LoadLogoBytes()
        {
            // 1. Try loading from Embedded Resources
            var asm = Assembly.GetExecutingAssembly();
            var resourceNames = asm.GetManifestResourceNames();
            var targetResource = resourceNames.FirstOrDefault(n => n.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase))
                                ?? resourceNames.FirstOrDefault(n => n.IndexOf("pokecutweb", StringComparison.OrdinalIgnoreCase) >= 0);

            if (!string.IsNullOrEmpty(targetResource))
            {
                using var stream = asm.GetManifestResourceStream(targetResource);
                if (stream != null)
                {
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }

            // 2. Try loading from output directory / Assets folder
            string[] possiblePaths =
            {
                Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "pokecutweb_1789365462537 - Copy.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "pokecutweb_1789365462537 - Copy.png"),
                @"C:\Users\ACER\Downloads\pokecutweb_1789365462537 - Copy.png"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(path);
                        if (bytes.Length > 0) return bytes;
                    }
                    catch
                    {
                        // continue to next candidate
                    }
                }
            }

            return null;
        }

        private static Icon? CreateIconFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            try
            {
                using var iconBmp = new Bitmap(48, 48);
                using (var g = Graphics.FromImage(iconBmp))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    float scale = Math.Min(48f / bitmap.Width, 48f / bitmap.Height);
                    int nw = Math.Max(1, (int)(bitmap.Width * scale));
                    int nh = Math.Max(1, (int)(bitmap.Height * scale));
                    int nx = (48 - nw) / 2;
                    int ny = (48 - nh) / 2;
                    g.DrawImage(bitmap, nx, ny, nw, nh);
                }

                // Pack into a valid, standalone Windows .ICO stream with PNG payload
                using var pngStream = new MemoryStream();
                iconBmp.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
                byte[] pngBytes = pngStream.ToArray();

                using var icoStream = new MemoryStream();
                using var writer = new BinaryWriter(icoStream);

                // ICONDIR header (6 bytes)
                writer.Write((short)0); // Reserved. Must always be 0.
                writer.Write((short)1); // Specifies image type: 1 for icon (.ICO) image.
                writer.Write((short)1); // Specifies number of images in the file.

                // ICONDIRENTRY (16 bytes)
                writer.Write((byte)48); // Specifies image width in pixels.
                writer.Write((byte)48); // Specifies image height in pixels.
                writer.Write((byte)0);  // Specifies number of colors in the color palette (0 if no palette).
                writer.Write((byte)0);  // Reserved. Must be 0.
                writer.Write((short)1); // Specifies color planes. Should be 0 or 1.
                writer.Write((short)32);// Specifies bits per pixel.
                writer.Write((int)pngBytes.Length); // Specifies the size of the image's data in bytes.
                writer.Write((int)22);  // Specifies the offset of BMP/PNG data from the beginning of the ICO/CUR file (6 + 16 = 22).

                // Image Data (PNG bytes)
                writer.Write(pngBytes);
                writer.Flush();

                icoStream.Position = 0;
                return new Icon(icoStream);
            }
            catch
            {
                try
                {
                    IntPtr hIcon = bitmap.GetHicon();
                    return Icon.FromHandle(hIcon);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
