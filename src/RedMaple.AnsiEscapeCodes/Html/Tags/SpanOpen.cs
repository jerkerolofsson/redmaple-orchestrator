using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Html.Tags
{
    public class SpanOpen : Tag
    {
        private readonly Style _style;

        public SpanOpen(Style style)
        {
            _style = style;
        }

        public override string ToString()
        {
            return $"<span style='{_style.ToCssStyle()}'>";
        }
    }
}
