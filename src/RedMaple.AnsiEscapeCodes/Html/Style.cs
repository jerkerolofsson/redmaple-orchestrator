using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedMaple.AnsiEscapeCodes.Tokens.Csi.Sgr;

namespace RedMaple.AnsiEscapeCodes.Html
{
    public record class Style
    {
        public string? Color { get; set; }
        public string? BackgroundColor { get; set; }
        public FontWeight? FontWeight { get; set; }
        public bool? Italic { get; set; }
        public bool? Underline { get; set; }

        public string ToCssStyle()
        {
            var sb = new StringBuilder();

            if (Color is not null)
            {
                sb.Append($"color: {Color};");
            }
            if (BackgroundColor is not null)
            {
                sb.Append($"background: {BackgroundColor};");
            }

            if (FontWeight is not null)
            {
                sb.Append($"font-weight: {(int)FontWeight};");
            }
            if (Italic == true)
            {
                sb.Append($"font-style: italic;");
            }
            if (Underline == true)
            {
                sb.Append($"text-decoration: underline;");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Sets a color from rgb in 5 levels
        /// </summary>
        /// <param name="r">0-5</param>
        /// <param name="g">0-5</param>
        /// <param name="b">0-5</param>
        /// <param name="index"></param>
        public void SetRgbColor(int r, int g, int b, int index)
        {
            r = r > 0 ? r * 40 + 55 : 0;
            g = g > 0 ? g * 40 + 55 : 0;
            b = b > 0 ? b * 40 + 55 : 0;

            Color = AnsiColors.ToRgbHexString(r, g, b);
        }
    }
}
