using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedMaple.Orchestrator.Utilities
{
    internal class Hanzi
    {
        private FrozenDictionary<string, List<string>>? mMapping = null;

        public string HanziToPinyin(string text)
        {
            Load();
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                if (mMapping.TryGetValue(c.ToString(), out var candidates) && candidates.Count > 0)
                {
                    sb.Append(candidates.First());
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        [MemberNotNull(nameof(mMapping))]
        private void Load()
        {
            if (mMapping is not null)
            {
                return;
            }
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RedMaple.Orchestrator.Utilities.Data.hanzi.json");
            if (stream is not null)
            {
                var mapping = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(stream) ?? new();
                mMapping = mapping.ToFrozenDictionary();
            }
            else
            {
                mMapping = new Dictionary<string, List<string>>().ToFrozenDictionary();
            }

        }
    }
}
