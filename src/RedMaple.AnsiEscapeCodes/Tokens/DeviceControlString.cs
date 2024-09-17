using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.Tokens
{
    public class DeviceControlString : AnsiToken
    {
        private string _text;

        public DeviceControlString(string text)
        {
            _text = text;
        }
    }
}
