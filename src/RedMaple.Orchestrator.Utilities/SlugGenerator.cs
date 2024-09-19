using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Utilities
{
    public class SlugGenerator
    {
        private readonly static Slugify.SlugHelper _helper = new Slugify.SlugHelper();
        private readonly static Hanzi _hanzi = new Hanzi();
        public static string Generate(string text)
        {
            text = _hanzi.HanziToPinyin(text);
            return _helper.GenerateSlug(text);
        }
    }
}
