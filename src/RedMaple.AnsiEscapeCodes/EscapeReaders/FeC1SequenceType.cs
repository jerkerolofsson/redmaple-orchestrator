using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.EscapeReaders
{
    public enum FeC1SequenceType
    {
        SingleShiftTwo = 0x8E,
        SingleShiftThree = 0x8F,
        DeviceControlString = 0x90,
        ControlSequenceIntroducer = 0x9B,
        StringTerminator = 0x9C,
        OperatingSystemCommand = 0x9D,

        StartOfString = 0x98,
        PrivacyMessage = 0x9E,
        ApplicationProgramCommand = 0x9F,
    }
}
