using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{

    public class AnsiToken
    {
        public virtual bool IsEmpty => false;

        public char TerminatorChar { get; internal set; }
    }
}
