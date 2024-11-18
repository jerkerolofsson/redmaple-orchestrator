using System;
using System.Text;

namespace RedMaple.Orchestrator.Node.Controllers
{
    public class ProgressResultBuilder : IProgress<string>
    {
        private readonly List<string> _lines = new();

        public IReadOnlyList<string> Lines => _lines;

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach(var line in Lines)
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        public void Report(string value)
        {
            _lines.Add(value);
        }
    }
}
