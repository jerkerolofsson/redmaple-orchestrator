using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{

    public class TextToken : AnsiToken
    {
        public TextToken(string data)
        {
            Data = data;
        }

        public string Data { get; set; }

        public override bool IsEmpty => Data.Length == 0;

    }
}
