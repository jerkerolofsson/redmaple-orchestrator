using System.Text;

using RedMaple.AnsiEscapeCodes.EscapeReaders;
using RedMaple.AnsiEscapeCodes.Tokens;

namespace RedMaple.AnsiEscapeCodes
{
    /// <summary>
    /// Provides functionality to tokenize a string containing ANSI escape sequences into a sequence of tokens
    /// representing text and control characters.
    /// </summary>
    /// <remarks>The AnsiTokenizer class processes input text character by character, yielding tokens for text
    /// segments, line feeds, carriage returns, replacement characters, and recognized escape sequences. It is useful
    /// for parsing terminal output or other streams that include ANSI formatting codes, enabling structured handling of
    /// both printable text and control sequences. The tokenizer maintains internal state to accurately detect and read
    /// escape sequences, ensuring that the output reflects the logical structure of the input. This class is not
    /// thread-safe.</remarks>
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

        /// <summary>
        /// Tokenizes the specified input string into a sequence of text and control character tokens, including escape
        /// sequences, line feeds, carriage returns, and replacement characters.
        /// </summary>
        /// <remarks>This method processes the input incrementally, yielding tokens as they are
        /// identified. It recognizes and separates text segments, line feeds, carriage returns, replacement characters,
        /// and escape sequences for further handling. This approach allows efficient processing of large or streaming
        /// text inputs.</remarks>
        /// <param name="text">The input string to be parsed. May contain plain text, control characters, and ANSI escape sequences.</param>
        /// <returns>An enumerable collection of AnsiToken instances, each representing a segment of text or a recognized control
        /// character from the input.</returns>
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
