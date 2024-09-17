using RedMaple.AnsiEscapeCodes.Tokens;
using RedMaple.AnsiEscapeCodes.Tokens.Csi.Sgr;

namespace RedMaple.AnsiEscapeCodes.Tests
{
    public class TokenizationTests
    {
        [Fact]
        public void Tokenize_CSI_BlackBackground_OneSingleToken()
        {
            string escape = "\u001b[40m";

            var tokenizer = new AnsiTokenizer();
            var tokens = tokenizer.Tokenize(escape).ToList();
            Assert.Single(tokens);
        }

        [Fact]
        public void Tokenize_CSI_BlackBackground_TokenIsSgr()
        {
            string escape = "\u001b[40m";

            var tokenizer = new AnsiTokenizer();
            var tokens = tokenizer.Tokenize(escape).ToList();
            Assert.Single(tokens);
            Assert.IsType<SgrToken>(tokens.First());
        }
        [Fact]
        public void Tokenize_CSI_BlackBackground_ThreeCharsAddedToCsi()
        {
            string escape = "\u001b[40m";

            var tokenizer = new AnsiTokenizer();
            var tokens = tokenizer.Tokenize(escape).ToList();
            Assert.Single(tokens);
            var token = tokens.First() as ControlSequenceIntroducer;
            Assert.Equal(3, token!.Text.Length);
        }

        [Fact]
        public void Tokenize_CSI_BlackBackground_TerminatorCharIsSet()
        {
            string escape = "\u001b[40m";

            var tokenizer = new AnsiTokenizer();
            var tokens = tokenizer.Tokenize(escape).ToList();
            Assert.Single(tokens);
            var token = tokens.First();
            Assert.Equal('m', token.TerminatorChar);
        }

    }
}