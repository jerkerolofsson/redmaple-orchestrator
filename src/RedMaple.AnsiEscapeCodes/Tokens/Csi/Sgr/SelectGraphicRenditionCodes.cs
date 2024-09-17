namespace RedMaple.AnsiEscapeCodes.Tokens.Csi.Sgr
{
    public enum SelectGraphicRenditionCodes
    {
        Reset = 0,
        Bold = 1,

        /// <summary>
        /// Semi bold
        /// </summary>
        Faint = 2,
        Italic = 3,
        Underline = 4,
        SlowBlink = 5,
        RapidBlink = 6,
        Invert = 7,
        Conceal = 8,
        Strike = 9,
        Primary = 10,
        Alternative1 = 11,
        Alternative2 = 12,
        Alternative3 = 13,
        Alternative4 = 14,
        Alternative5 = 15,
        Alternative6 = 16,
        Alternative7 = 17,
        Alternative8 = 18,
        Alternative9 = 19,

        /// <summary>
        /// Gothic
        /// </summary>
        Faktur = 20,

        DoublyUnderlinedOrNotBold = 21,

        /// <summary>
        /// Neither bold nor faint
        /// </summary>
        NormalIntensity = 22,

        NeitherItalicNorBlackLetter = 23,
        NotUnderlined = 24,
        NotBlinking = 25,
        ProportionalSpacing = 26,
        NotReveresed = 27,
        Reveal = 28,
        NotCrossedOut = 29,
        SetForegroundColor0 = 30,
        SetForegroundColor1 = 31,
        SetForegroundColor2 = 32,
        SetForegroundColor3 = 33,
        SetForegroundColor4 = 34,
        SetForegroundColor5 = 35,
        SetForegroundColor6 = 36,
        SetForegroundColor7 = 37,
        SetForegroundColorRgb = 38,
        SetDefaultForegroundColor = 39,

        SetBackgroundColor0 = 40,
        SetBackgroundColor1 = 41,
        SetBackgroundColor2 = 42,
        SetBackgroundColor3 = 43,
        SetBackgroundColor4 = 44,
        SetBackgroundColor5 = 45,
        SetBackgroundColor6 = 46,
        SetBackgroundColor7 = 47,
        SetBackgroundColorRgb = 48,
        SetDefaultBackgroundColor = 49,

        DisableProportionalSpacing = 50,
        Framed = 51,
        Encircled = 52,
        Overlined = 53,
        NeighterFramedNorEncircled = 54,
        NotOverlined = 55,

        /// <summary>
        /// 	Not in standard; implemented in Kitty, VTE, mintty, and iTerm2.[24][25] Next arguments are 5;n or 2;r;g;b.
        /// </summary>
        SetUnderlineColor = 58,
        SetDefaultUnderlineColor = 59,

        Superscript = 73,
        Subscript = 74,
        NeitherSuperscriptNorSubscript = 75,

        SetBrightForegroundColor0 = 90,
        SetBrightForegroundColor1 = 91,
        SetBrightForegroundColor2 = 92,
        SetBrightForegroundColor3 = 93,
        SetBrightForegroundColor4 = 94,
        SetBrightForegroundColor5 = 95,
        SetBrightForegroundColor6 = 96,
        SetBrightForegroundColor7 = 98,

        SetBrightBackgroundColor0 = 100,
        SetBrightBackgroundColor1 = 101,
        SetBrightBackgroundColor2 = 102,
        SetBrightBackgroundColor3 = 103,
        SetBrightBackgroundColor4 = 104,
        SetBrightBackgroundColor5 = 105,
        SetBrightBackgroundColor6 = 106,
        SetBrightBackgroundColor7 = 108,
    }
}
