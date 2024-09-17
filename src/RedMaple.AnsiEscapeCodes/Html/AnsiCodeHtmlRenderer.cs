using RedMaple.AnsiEscapeCodes.Html.Tags;
using RedMaple.AnsiEscapeCodes.Tokens;
using RedMaple.AnsiEscapeCodes.Tokens.Csi.Sgr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Html
{
    public class AnsiCodeHtmlRenderer
    {
        private Style _style = new();

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
