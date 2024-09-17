using RedMaple.AnsiEscapeCodes.Tokens;
using RedMaple.AnsiEscapeCodes.Tokens.Csi;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMaple.AnsiEscapeCodes.EscapeReaders
{
    public class FeEscapeReader : EscapeReader
    {
        private char? _sequenceType = null;
        private StringBuilder _chars = new StringBuilder();

        public FeEscapeReader() : base((char)0x40, (char)0x5F)
        {
        }

        public override bool TryRead(char c, [NotNullWhen(true)] out AnsiToken? token)
        {
            if(_sequenceType is null)
            {
                _sequenceType = c;
                switch (_sequenceType)
                {
                    case FeSequenceType.OperatingSystemCommand:
                        SetEndChars([
                            (char)FeSequenceType.StringTerminator,
                            (char)0x1B,
                            (char)0x5C,
                            ]);
                        break;
                    case FeSequenceType.DeviceControlString:
                    case FeSequenceType.StartOfString:
                    case FeSequenceType.ApplicationProgramCommand:
                    case FeSequenceType.PrivacyMessage:
                        SetEndChar((char)FeSequenceType.StringTerminator);
                        break;

                    case FeSequenceType.ControlSequenceIntroducer:
                        SetEndCharRange((char)0x40, (char)0x7E);
                        break;

                    case FeSequenceType.SingleShiftTwo:
                    case FeSequenceType.SingleShiftThree:
                        break;
                }
            }
            else
            {
                _chars.Append(c);

                switch (_sequenceType)
                {
                    case FeSequenceType.SingleShiftTwo:
                        token = new Ss2CharacterSet(c);
                        return true;
                    case FeSequenceType.SingleShiftThree:
                        token = new Ss3CharacterSet(c);
                        return true;

                    case FeSequenceType.ControlSequenceIntroducer:
                        if (IsTerminator(c))
                        {
                            token = CsiTokenFactory.Create(_chars.ToString());
                            return true;
                        }
                        break;
                    case FeSequenceType.DeviceControlString:
                        if (IsTerminator(c))
                        {
                            token = new DeviceControlString(_chars.ToString());
                            return true;
                        }
                        break;
                    case FeSequenceType.StartOfString:
                        if (IsTerminator(c))
                        {
                            token = new StartOfString(_chars.ToString());
                            return true;
                        }
                        break;
                    case FeSequenceType.OperatingSystemCommand:
                        if (IsTerminator(c) || c == 0x07) // XTerm BEL
                        {
                            token = new OperatingSystemCommand(_chars.ToString());
                            return true;
                        }
                        break;
                    case FeSequenceType.PrivacyMessage:
                        if (IsTerminator(c))
                        {
                            token = new PrivacyMessage(_chars.ToString());
                            return true;
                        }
                        break;
                    case FeSequenceType.ApplicationProgramCommand:
                        if (IsTerminator(c))
                        {
                            token = new ApplicationProgramCommand(_chars.ToString());
                            return true;
                        }
                        break;
                }
            }

            token = null;
            return false;
        }
    }
}
