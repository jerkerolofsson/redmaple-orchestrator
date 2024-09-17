using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{
    public class ApplicationProgramCommand : AnsiToken
    {
        private string _text;

        public string Text => _text;

        public ApplicationProgramCommand(string text)
        {
            _text = text;
        }
    }
}
