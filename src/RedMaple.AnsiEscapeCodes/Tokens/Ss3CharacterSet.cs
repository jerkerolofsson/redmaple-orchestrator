using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{

    public class Ss3CharacterSet : AnsiToken
    {
        public Ss3CharacterSet(char characterSet)
        {
            CharacterSet = characterSet;
        }

        public char CharacterSet { get; }
    }
}
