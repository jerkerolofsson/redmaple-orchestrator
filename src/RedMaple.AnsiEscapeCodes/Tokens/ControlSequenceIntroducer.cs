using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{
    public class ControlSequenceIntroducer : AnsiToken
    {
        private string _text;

        public string Text => _text;

        public ControlSequenceIntroducer(string text)
        {
            _text = text;
        }
    }
}
