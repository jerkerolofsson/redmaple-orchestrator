using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{
    public class StartOfString : AnsiToken
    {
        private string _text;

        public StartOfString(string text)
        {
            _text = text;
        }
    }
}
