using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{
    public class PrivacyMessage : AnsiToken
    {
        private string _text;

        public PrivacyMessage(string text)
        {
            _text = text;
        }
    }
}
