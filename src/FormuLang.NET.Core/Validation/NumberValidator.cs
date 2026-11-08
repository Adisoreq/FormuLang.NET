using System.Globalization;
using System.Text.RegularExpressions;

namespace FormuLang.NET.Core.Validation
{
    /// <summary>
    /// Validation and conversion of Number literals (docs/syntax/literals.md, "Number").
    /// </summary>
    public static partial class NumberValidator
    {
        [GeneratedRegex(@"^\d+(?:_\d+)*$", RegexOptions.CultureInvariant)]
        private static partial Regex IntegerPattern();

        [GeneratedRegex(@"^(?:\d+\.\d*|\d*\.\d+|\d+)$", RegexOptions.CultureInvariant)]
        private static partial Regex FractionPattern();

        [GeneratedRegex(@"^\d+(?:\.\d+)?[eE][+-]?\d+$", RegexOptions.CultureInvariant)]
        private static partial Regex ScientificPattern();

        [GeneratedRegex(@"^0[xX][0-9a-fA-F]+$", RegexOptions.CultureInvariant)]
        private static partial Regex HexadecimalPattern();

        /// <summary>
        /// Integer, optionally with digits separated by underlines: 42, 042, 1_000_000.
        /// </summary>
        public static bool IsValidIntegerExpression(string expression)
            => IntegerPattern().IsMatch(expression);

        /// <summary>
        /// Number with an optional fraction part: 12.5, .45, 1.
        /// </summary>
        public static bool IsValidRealNumberExpression(string expression)
            => FractionPattern().IsMatch(expression);

        /// <summary>
        /// Scientific notation: 1.2E+3, 5e-3.
        /// </summary>
        public static bool IsValidScientificNotationExpression(string expression)
            => ScientificPattern().IsMatch(expression);

        /// <summary>
        /// Hexadecimal integer: 0xFF, 0X1a.
        /// </summary>
        public static bool IsValidHexadecimalExpression(string expression)
            => HexadecimalPattern().IsMatch(expression);

        public static bool IsValidNumber(string expression)
        {
            return IsValidIntegerExpression(expression)
                || IsValidRealNumberExpression(expression)
                || IsValidScientificNotationExpression(expression)
                || IsValidHexadecimalExpression(expression);
        }

        /// <summary>
        /// Converts a Number literal to its value.
        /// </summary>
        /// <returns>False if the expression is not a valid Number literal.</returns>
        public static bool TryParse(string expression, out double value)
        {
            value = 0;

            if (IsValidHexadecimalExpression(expression))
            {
                // Leading zeros after the prefix are ignored; the value can exceed long, so accumulate as double.
                foreach (char c in expression.AsSpan(2))
                    value = value * 16 + Convert.ToInt32(c.ToString(), 16);

                return true;
            }

            if (IsValidIntegerExpression(expression) || IsValidRealNumberExpression(expression) || IsValidScientificNotationExpression(expression))
            {
                string normalized = expression.Replace("_", string.Empty);
                return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }

            return false;
        }
    }
}
