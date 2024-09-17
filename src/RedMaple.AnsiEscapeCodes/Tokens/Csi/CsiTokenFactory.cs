using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens.Csi
{
    internal class CsiTokenFactory
    {
        private static readonly Regex _cursorUp = new Regex("\\d.*A");
        private static readonly Regex _cursorDown = new Regex("\\d.*B");
        private static readonly Regex _cursorDorward = new Regex("\\d.*C");
        private static readonly Regex _cursorBack = new Regex("\\d.*D");
        private static readonly Regex _cursorNextLine = new Regex("\\d.*E");
        private static readonly Regex _cursorPrevLine = new Regex("\\d.*F");
        private static readonly Regex _cursorHorizontalAbsolute = new Regex("\\d.*G");

        private static readonly Regex _cursorPosition = new Regex("\\d.*;\\d.*H");
        private static readonly Regex _cursorEraseDisplay = new Regex("(\\d.*|)J");
        private static readonly Regex _cursorEraseInLine = new Regex("(\\d.*|)K");
        private static readonly Regex _cursorScrollUp = new Regex("(\\d.*|)S");
        private static readonly Regex _cursorScrollDown = new Regex("(\\d.*|)T");

        private static readonly Regex _sgr = new Regex("(\\d.*|)m");

        public static ControlSequenceIntroducer Create(string text)
        {
            var sgr = _sgr.Match(text);
            if (sgr.Success)
            {
                return Sgr.SgrTokenFactory.Create(text);
            }

            return new ControlSequenceIntroducer(text);
        }
    }
}
