using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens.Csi.Sgr
{
    public class AnsiColors
    {
        private static readonly IReadOnlyDictionary<int, string> _defaultColors = new Dictionary<int, string>
        {
            [0] = "#000",
            [1] = "#A00",
            [2] = "#0A0",
            [3] = "#A50",
            [4] = "#00A",
            [5] = "#A0A",
            [6] = "#0AA",
            [7] = "#AAA",

            [8] = "#555",
            [9] = "#F55",
            [10] = "#5F5",
            [11] = "#FF5",
            [12] = "#55F",
            [13] = "#F5F",
            [14] = "#5FF",
            [15] = "#FFF"
        };

        public static string GetDefaultBackgroundColor(int index)
        {
            // Note: inclusive ranges
            return _defaultColors[index];
        }
        public static string GetDefaultForegroundColor(int index)
        {
            return _defaultColors[index];

        }


        /// <summary>
        /// Formats colors as a html color hex string #RRGGBB
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string ToRgbHexString(int r, int g, int b)
        {
            return $"#{r.ToString("x2")}{g.ToString("x2")}{b.ToString("x2")}";
        }
    }
}
