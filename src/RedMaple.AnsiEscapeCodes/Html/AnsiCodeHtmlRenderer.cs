using System.Text;

using RedMaple.AnsiEscapeCodes.Html.Tags;
using RedMaple.AnsiEscapeCodes.Tokens;
using RedMaple.AnsiEscapeCodes.Tokens.Csi.Sgr;

namespace RedMaple.AnsiEscapeCodes.Html
{
    /// <summary>
    /// Provides functionality to convert ANSI encoded text into HTML, translating ANSI escape codes into styled HTML
    /// elements for web display.
    /// </summary>
    /// <remarks>Use this class to render ANSI text with formatting such as colors, font weight, and line
    /// breaks in HTML. It supports a range of ANSI styles, including foreground and background colors, bold text, and
    /// resets. This is useful for displaying terminal output or logs in web applications while preserving their
    /// original formatting.</remarks>
    public class AnsiCodeHtmlRenderer
    {
        /// <summary>
        /// Current style
        /// </summary>
        private readonly Style _style = new();

        /// <summary>
        /// Converts the specified ANSI text to its HTML representation by processing ANSI escape codes and generating
        /// corresponding HTML elements.
        /// </summary>
        /// <remarks>Use this method to display ANSI-formatted text in web environments. The conversion
        /// preserves formatting such as colors and styles defined by ANSI escape codes. Ensure that the input text is
        /// properly formatted to achieve the desired HTML output.</remarks>
        /// <param name="ansiText">The ANSI text to be converted. This string may contain ANSI escape codes, which will be interpreted and
        /// transformed into HTML elements.</param>
        /// <returns>A string containing the HTML representation of the provided ANSI text.</returns>
        public string RenderHtml(string ansiText)
        {
            var tokens = new AnsiTokenizer().Tokenize(ansiText).ToList();
            var html = new StringBuilder();

            foreach(var htmlElement in ToHtmlElements(tokens))
            {
                html.Append(htmlElement);
            }
            return html.ToString();
        }

        /// <summary>
        /// Converts a sequence of ANSI tokens into a collection of HTML elements, applying styles based on the token
        /// types encountered.
        /// </summary>
        /// <remarks>This method processes text, line feed, and graphic rendition tokens to produce styled
        /// HTML elements. Styles such as font weight, foreground color, and background color are adjusted according to
        /// the graphic rendition codes. The method yields elements in the order they appear in the input
        /// sequence.</remarks>
        /// <param name="tokens">The collection of ANSI tokens to be rendered as HTML elements. Each token determines the structure and
        /// styling of the resulting HTML output.</param>
        /// <returns>An enumerable collection of HtmlElement instances representing the HTML elements generated from the provided
        /// tokens.</returns>
        private IEnumerable<HtmlElement> ToHtmlElements(IEnumerable<AnsiToken> tokens)
        { 
            foreach (var token in tokens)
            {
                if (token is TextToken text)
                {
                    yield return new SpanOpen(_style);
                    yield return new InnerText(text.Data.ToString());
                    yield return new SpanClose();
                }
                else if (token is LineFeed _)
                {
                    yield return new Br();
                }
                else if (token is SgrToken sgr)
                {
                    switch(sgr.Code)
                    {
                        case SelectGraphicRenditionCodes.NormalIntensity:
                            _style.FontWeight = FontWeight.Regular;
                            break;
                        case SelectGraphicRenditionCodes.Bold:
                            _style.FontWeight = FontWeight.Bold;
                            break;
                        case SelectGraphicRenditionCodes.SetForegroundColor0:
                            _style.Color = AnsiColors.GetDefaultForegroundColor(0);
                            break;
                        case SelectGraphicRenditionCodes.SetForegroundColor1:
                            _style.Color = AnsiColors.GetDefaultForegroundColor(1);
                            break;
                        case SelectGraphicRenditionCodes.SetForegroundColor2:
                            _style.Color = AnsiColors.GetDefaultForegroundColor(2);
                            break;
                        case SelectGraphicRenditionCodes.SetForegroundColor3:
                            _style.Color = AnsiColors.GetDefaultForegroundColor(3);
                            break;
                        case SelectGraphicRenditionCodes.SetForegroundColor4:
                            _style.Color = AnsiColors.GetDefaultForegroundColor(4);
                            break;
                        case SelectGraphicRenditionCodes.SetForegroundColor5:
                            _style.Color = AnsiColors.GetDefaultForegroundColor(5);
                            break;
                        case SelectGraphicRenditionCodes.SetForegroundColor6:
                            _style.Color = AnsiColors.GetDefaultForegroundColor(6);
                            break;
                        case SelectGraphicRenditionCodes.SetForegroundColor7:
                            _style.Color = AnsiColors.GetDefaultForegroundColor(7);
                            break;
                        case SelectGraphicRenditionCodes.SetBackgroundColor0:
                            _style.BackgroundColor = AnsiColors.GetDefaultBackgroundColor(0);
                            break;
                        case SelectGraphicRenditionCodes.SetBackgroundColor1:
                            _style.BackgroundColor = AnsiColors.GetDefaultBackgroundColor(1);
                            break;
                        case SelectGraphicRenditionCodes.SetBackgroundColor2:
                            _style.BackgroundColor = AnsiColors.GetDefaultBackgroundColor(2);
                            break;
                        case SelectGraphicRenditionCodes.SetBackgroundColor3:
                            _style.BackgroundColor = AnsiColors.GetDefaultBackgroundColor(3);
                            break;
                        case SelectGraphicRenditionCodes.SetBackgroundColor4:
                            _style.BackgroundColor = AnsiColors.GetDefaultBackgroundColor(4);
                            break;
                        case SelectGraphicRenditionCodes.SetBackgroundColor5:
                            _style.BackgroundColor = AnsiColors.GetDefaultBackgroundColor(5);
                            break;
                        case SelectGraphicRenditionCodes.SetBackgroundColor6:
                            _style.BackgroundColor = AnsiColors.GetDefaultBackgroundColor(6);
                            break;
                        case SelectGraphicRenditionCodes.SetBackgroundColor7:
                            _style.BackgroundColor = AnsiColors.GetDefaultBackgroundColor(7);
                            break;
                        case SelectGraphicRenditionCodes.Reset:
                            _style.FontWeight = null;
                            _style.Color = null;
                            _style.BackgroundColor = null;
                            _style.Italic = null;
                            _style.Underline = null;
                            break;
                    }
                }
            }
        }
    }
}
