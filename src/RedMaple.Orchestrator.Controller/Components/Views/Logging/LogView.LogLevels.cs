using System.Text.RegularExpressions;

namespace RedMaple.Orchestrator.Controller.Components.Views.Logging
{
    public partial class LogView
    {
        private static readonly Regex _regexInfo = new Regex("info:");
        private static readonly Regex _regexCrit = new Regex("crit:");
        private static readonly Regex _regexDbug = new Regex("dbug:");
        private static readonly Regex _regexWarn = new Regex("warn:");
        private static readonly Regex _regexFail = new Regex("fail:");

        protected string ColorizeLogLevels(string html)
        {
            html = _regexInfo.Replace(html, $"<span style='color: cyan'>info</span>");
            html = _regexCrit.Replace(html, $"<span style='color: hotpink'>crit</span>");
            html = _regexWarn.Replace(html, $"<span style='color: orange'>warn</span>");
            html = _regexFail.Replace(html, $"<span style='color: red'>fail</span>");
            html = _regexDbug.Replace(html, $"<span>dbug</span>");
            return html;
        }
    }
}
