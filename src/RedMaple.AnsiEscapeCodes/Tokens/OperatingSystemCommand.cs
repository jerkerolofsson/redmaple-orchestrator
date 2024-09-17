using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{
    public class OperatingSystemCommand : AnsiToken
    {
        private string _text;

        public OperatingSystemCommand(string text)
        {
            _text = text;
        }
    }
}
