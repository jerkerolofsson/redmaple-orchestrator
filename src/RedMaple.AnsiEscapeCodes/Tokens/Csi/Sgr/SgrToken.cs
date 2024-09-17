using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens.Csi.Sgr
{
    public class SgrToken : ControlSequenceIntroducer
    {
        private SelectGraphicRenditionCodes _code;

        public SelectGraphicRenditionCodes Code => _code;
        public string Args { get; }

        public SgrToken(string text, string args, SelectGraphicRenditionCodes code) : base(text)
        {
            Args = args;
            _code = code;
        }
    }
}
