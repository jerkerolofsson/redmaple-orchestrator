using RedMaple.AnsiEscapeCodes.Html;
using static MudBlazor.Colors;

namespace RedMaple.Orchestrator.Controller.Components.Views.Logging
{
    public partial class LogView
    {
        private AnsiCodeHtmlRenderer _ansiHtmlRenderer = new AnsiCodeHtmlRenderer();

        private string AnsiToHtml(string line)
        {
            return _ansiHtmlRenderer.RenderHtml(line);
        }
    }
}
