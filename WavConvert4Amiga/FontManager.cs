using System;
using System.Drawing;
using System.Linq;

namespace WavConvert4Amiga
{
    public static class FontManager
    {
        private static readonly string[] MonospaceFonts = new[]
        {
            "Consolas",
            "Courier New",
            "Lucida Console",
            "DejaVu Sans Mono",
            "Monaco",
            "Microsoft Sans Serif" // Fallback if no monospace fonts are available
        };

        private static string cachedFontFamily;

        public static Font GetMainFont(float size = 9f, FontStyle style = FontStyle.Regular)
        {
            if (string.IsNullOrEmpty(cachedFontFamily))
            {
                // Find the first available font from our list
                cachedFontFamily = MonospaceFonts.FirstOrDefault(IsFontInstalled) ?? "Microsoft Sans Serif";
            }

            return new Font(cachedFontFamily, size, style);
        }

        private static bool IsFontInstalled(string fontName)
        {
            try
            {
                using (var testFont = new Font(fontName, 8))
                {
                    return testFont.Name.Equals(fontName, StringComparison.InvariantCultureIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetFontFamily()
        {
            if (string.IsNullOrEmpty(cachedFontFamily))
            {
                cachedFontFamily = MonospaceFonts.FirstOrDefault(IsFontInstalled) ?? "Microsoft Sans Serif";
            }
            return cachedFontFamily;
        }
    }
}