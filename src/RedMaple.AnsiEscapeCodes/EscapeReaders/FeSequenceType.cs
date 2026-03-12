namespace RedMaple.AnsiEscapeCodes.EscapeReaders
{
    internal class FeSequenceType
    {
        public  const char SingleShiftTwo = 'N';
        public  const char SingleShiftThree = 'O';
        public  const char DeviceControlString = 'P';
        public  const char ControlSequenceIntroducer = '[';
        public  const char StringTerminator = '\\';
        public  const char OperatingSystemCommand = ']';

        public  const char StartOfString = 'X';
        public  const char PrivacyMessage = '^';
        public  const char ApplicationProgramCommand = '_';
    }
}
