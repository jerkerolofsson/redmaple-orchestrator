using RedMaple.AnsiEscapeCodes.EscapeReaders;
using RedMaple.AnsiEscapeCodes.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace RedMaple.AnsiEscapeCodes
{
    public class AnsiTokenizer
    {
        private const char ESCAPE = (char)0x1B;

        private State _state = State.Text;

        public enum State
        {
            Text,
            DetectEscapeSequence,
            ReadEscapeSequence
        }

        public IEnumerable<AnsiToken> Tokenize(string text)
        {
            _state = State.Text;

            var data = new StringBuilder();
            EscapeReader? reader = null;
            foreach (char c in text)
            {
                if (_state == State.Text)
                {
                    switch(c)
                    {
                        case ESCAPE:
                            if (data.Length > 0)
                            {
                                yield return new TextToken(data.ToString());
                                data.Clear();
                            }
                            _state = State.DetectEscapeSequence;
                            continue;
                        case '\n':
                            if (data.Length > 0)
                            {
                                yield return new TextToken(data.ToString());
                                data.Clear();
                            }
                            yield return new LineFeed();
                            break;
                        case '\uFFFD':
                            if (data.Length > 0)
                            {
                                yield return new TextToken(data.ToString());
                                data.Clear();
                            }
                            yield return new ReplacementChar();
                            break;

                        case '\r':
                            if (data.Length > 0)
                            {
                                yield return new TextToken(data.ToString());
                                data.Clear();
                            }
                            yield return new CarriageReturn();
                            break;
                        default:
                            if (!IsNonPrintableChar(c))
                            {
                                data.Append(c);
                            }
                            break;
                    }
                }

                if (_state == State.DetectEscapeSequence)
                {
                    if (c >= 0x40 && c <= 0x5F)
                    {
                        reader = new FeEscapeReader();
                        _state = State.ReadEscapeSequence;
                    }
                    else if (c >= 0x60 && c <= 0x7E)
                    {
                        //reader = new FsEscapeReader();
                        //_state = State.ReadEscapeSequence;
                        _state = State.Text;
                        continue;
                    }
                    else if (c >= 0x30 && c <= 0x3F)
                    {
                        //reader = new FpEscapeReader();
                        //_state = State.ReadEscapeSequence;
                        _state = State.Text;
                        continue;
                    }
                    else if (c >= 0x20 && c <= 0x2F)
                    {
                        //reader = new nFEscapeReader();
                        //_state = State.ReadEscapeSequence;
                        _state = State.Text;
                        continue;
                    }
                    else
                    {
                        // Unknown
                        _state = State.Text;
                        continue;
                    }
                }

                if (_state == State.ReadEscapeSequence && reader is not null)
                {
                    if(reader.TryRead(c, out var token))
                    {
                        // End of escape sequence
                        token.TerminatorChar = c;
                        yield return token;
                        reader = null;
                        _state = State.Text;
                    }
                }

            }
            if (data.Length > 0)
            {
                yield return new TextToken(data.ToString());
                data.Clear();
            }
        }

        private bool IsNonPrintableChar(char c)
        {
            return c <= 0x8;
        }
    }
}
