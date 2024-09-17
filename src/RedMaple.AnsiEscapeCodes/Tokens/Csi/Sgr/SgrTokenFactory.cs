using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens.Csi.Sgr
{
    internal class SgrTokenFactory
    {

        public static SgrToken Create(string text)
        {
            string codeString = text[0..^1];
            string args = string.Empty;
            int semiColorPosition = codeString.IndexOf(';');
            if(semiColorPosition != -1)
            {
                args = codeString[(semiColorPosition + 1)..];
                codeString = codeString[0..(semiColorPosition)];
            }


            if (int.TryParse(codeString, out var code))
            {
                return new SgrToken(text, args, (SelectGraphicRenditionCodes)code);
            }
            return new SgrToken(text, args,SelectGraphicRenditionCodes.Reset);
        }
    }
}
