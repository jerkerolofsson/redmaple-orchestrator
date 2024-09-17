using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{

    public class Ss2CharacterSet : AnsiToken
    {
        public Ss2CharacterSet(char characterSet)
        {
            CharacterSet = characterSet;
        }

        public char CharacterSet { get; }
    }
}
