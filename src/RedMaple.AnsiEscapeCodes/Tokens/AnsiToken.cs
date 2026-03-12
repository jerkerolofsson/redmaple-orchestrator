using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{
    /// <summary>
    /// Base class for tokens representing segments of text or control characters in ANSI escape sequences. This class provides common properties and methods for all token types, such as determining if a token is empty and identifying the terminator character for escape sequences. Derived classes will implement specific token types, such as text tokens, line feed tokens, and various escape sequence tokens, each with their own unique properties and behaviors.
    /// </summary>
    public class AnsiToken
    {
        public virtual bool IsEmpty => false;

        public char TerminatorChar { get; internal set; }
    }
}
