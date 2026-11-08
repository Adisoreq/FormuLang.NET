namespace FormuLang.NET.Core.Utils
{
    /// <summary>
    /// Character classification of the FormuLang standard (docs/syntax/symbols.md).
    /// The standard allows only ASCII characters, so these checks are ASCII-only on purpose.
    /// </summary>
    internal static class StringUtils
    {
        /// <summary>
        /// [Tab], [New line], [Space] and the printable ASCII characters 33-126.
        /// </summary>
        public static bool IsValidSymbol(char c)
        {
            return c == '\t' || c == '\n' || (c >= ' ' && c <= '~');
        }

        public static bool IsCharUppercaseLetter(char c) => c >= 'A' && c <= 'Z';

        public static bool IsCharLowercaseLetter(char c) => c >= 'a' && c <= 'z';

        public static bool IsCharLetter(char c) => IsCharUppercaseLetter(c) || IsCharLowercaseLetter(c);

        public static bool IsCharDigit(char c) => c >= '0' && c <= '9';

        public static bool IsCharHexDigit(char c)
            => IsCharDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        /// <summary>
        /// Word characters: letters, digits and underline.
        /// </summary>
        public static bool IsCharWordCharacter(char c) => IsCharLetter(c) || IsCharDigit(c) || c == '_';

        /// <summary>
        /// An identifier starts with a letter or an underline.
        /// </summary>
        public static bool IsCharIdentifierStart(char c) => IsCharLetter(c) || c == '_';

        /// <summary>
        /// White spaces inside a line: [Space] and [Tab].
        /// </summary>
        public static bool IsCharWhitespace(char c) => c == ' ' || c == '\t';

        /// <summary>
        /// Printable ASCII characters that are neither word characters nor white spaces.
        /// </summary>
        public static bool IsCharOperator(char c)
        {
            return c != '_' && (
                   (c >= '!' && c <= '/')   // !"#$%&'()*+,-./
                || (c >= ':' && c <= '@')   // :;<=>?@
                || (c >= '[' && c <= '`')   // [\]^`
                || (c >= '{' && c <= '~')); // {|}~
        }

        public static bool IsStringWhitespace(string s) => string.IsNullOrWhiteSpace(s);
    }
}
