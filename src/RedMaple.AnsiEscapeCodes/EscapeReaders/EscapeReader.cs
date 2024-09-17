using RedMaple.AnsiEscapeCodes.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.EscapeReaders
{
    public class EscapeReader
    {
        private List<char> _chars = new List<char>();
        private char? _endCharStart;
        private char? _endCharEnd;
        private List<char> _endChars;

        public EscapeReader(char endCharStart, char endCharEnd)
        {
            _endCharStart = endCharStart;
            _endCharEnd = endCharEnd;
        }

        protected void SetEndCharRange(char start, char end)
        {
            _endCharStart = start;
            _endCharEnd = end;
        }
        protected void SetEndChars(char[] chars)
        {
            _endChars = [.. chars];
        }
        protected void SetEndChar(char c)
        {
            _endChars = [c];
        }
        protected void AddEndChar(char c)
        {
            _endChars.Add(c);
        }

        private bool IsValidEscapeChar(char c)
        {
            return c < 0x20 || c > 0x7f;
        }

        protected bool IsTerminator(char c)
        {
            if (_endChars?.Contains(c) == true)
            {
                return true;
            }
            if (_endCharStart is not null && _endCharEnd is not null)
            {
                if (c >= _endCharStart && c <= _endCharEnd)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual bool TryRead(char c, [NotNullWhen(true)] out AnsiToken? token)
        {
            token = null;
            if (!IsTerminator(c))
            {
                return false;
            }
            _chars.Add(c);
            token = new NullToken();
            return true;
        }
    }
}
