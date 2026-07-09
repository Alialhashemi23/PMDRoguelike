using Microsoft.Xna.Framework;
using System.Globalization;

namespace PMDRoguelike.Core
{
    public static class ColorUtil
    {
        /// <summary>Parse "#RRGGBB" (leading # optional). Falls back to magenta on bad input.</summary>
        public static Color FromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return Color.Magenta;

            string s = hex.TrimStart('#');
            if (s.Length != 6 || !int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
                return Color.Magenta;

            return new Color((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }
    }
}
