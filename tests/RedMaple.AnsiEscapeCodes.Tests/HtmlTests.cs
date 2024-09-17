using RedMaple.AnsiEscapeCodes.Html;
using RedMaple.AnsiEscapeCodes.Tokens;
using RedMaple.AnsiEscapeCodes.Tokens.Csi.Sgr;

namespace RedMaple.AnsiEscapeCodes.Tests
{
    public class HtmlTests
    {
        [Fact]
        public void Tokenize_CSI_BlackBackground_OneSingleToken()
        {
            string escape = "\u001b[40m Line 1\n\u001b[41m Line 2";

            var renderer = new AnsiCodeHtmlRenderer();
            var html = renderer.RenderHtml(escape);
        }

    }
}