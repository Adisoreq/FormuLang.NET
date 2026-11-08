using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Primitives;
using System.Globalization;
using System.Text;

namespace FormuLang.NET.Core.Utils
{
    /// <summary>
    /// Serialization of values (docs/standards/core/semantics/serialization.md) with the default parameters:
    /// <c>decimal_place = -1</c>, <c>split_array = false</c>, <c>split_object = false</c>, <c>include_object_class = false</c>.
    /// </summary>
    public static class StringConverter
    {
        private const string NullStr = "null";
        private const string UndefinedStr = "undefined";

        public static string ValueToString(IValue value) => Serialize(value, nested: false);

        /// <param name="nested">Text inside an array or object is written as a quoted literal.</param>
        private static string Serialize(IValue value, bool nested)
        {
            switch (value.Type.Base)
            {
                case Types.NullName:
                    return NullStr;

                case Types.UndefinedName:
                    return UndefinedStr;

                case Types.BooleanName:
                    return BooleanToString(value.As<bool>());

                case Types.NumberName:
                    return NumberToString(value.As<double>());

                case Types.TextName:
                    string text = value.As<string>() ?? string.Empty;
                    return nested ? TextToLiteral(text) : text;

                case Types.ErrorName:
                    return ErrorToString(value.As<IError>());

                case Types.ArrayName:
                    return ArrayToString(value.As<IValue[]>() ?? []);

                case Types.ObjectName:
                    return ObjectToString(value.As<IObject>());

                case Types.DelegateName:
                    return DelegateToString(value.As<IFunction>());

                default:
                    throw new NotSupportedException($"Unsupported value type: {value.Type.Base}");
            }
        }

        public static string BooleanToString(bool value) => value ? "true" : "false";

        /// <summary>
        /// Decimal notation, cut at the last non-zero digit (<c>decimal_place = -1</c>), with full round-trip precision.
        /// </summary>
        public static string NumberToString(double value)
        {
            string shortest = value.ToString("R", CultureInfo.InvariantCulture);

            // The shortest round-trip form uses exponent notation for very large and very small numbers
            return shortest.Contains('E')
                ? value.ToString("0." + new string('#', 339), CultureInfo.InvariantCulture)
                : shortest;
        }

        public static string TextToString(string value) => value;

        /// <summary>
        /// Text as a literal, with quotation marks and escape sequences.
        /// </summary>
        public static string TextToLiteral(string value)
        {
            var sb = new StringBuilder(value.Length + 2).Append('"');

            foreach (char c in value)
            {
                sb.Append(c switch
                {
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    '\n' => "\\n",
                    '\t' => "\\t",
                    _ => c.ToString(),
                });
            }

            return sb.Append('"').ToString();
        }

        public static string ErrorToString(IError? value)
            => value == null ? "[UNKNOWN_ERROR]" : $"[{value.Name} ({value.Code})] {value.Message}";

        public static string ArrayToString(IValue[] value)
            => "[" + string.Join(", ", value.Select(v => Serialize(v, nested: true))) + "]";

        public static string ObjectToString(IObject? value)
        {
            if (value == null || value.IsEmpty())
                return "{ }";

            var pairs = value.Keys.Select(key => $"{key}: {Serialize(value.GetProperty(key).GetValue(), nested: true)}");
            return "{ " + string.Join(", ", pairs) + " }";
        }

        public static string DelegateToString(IFunction? function)
        {
            if (function == null)
                return "() => any";

            return function is ILambda lambda
                ? $"({string.Join(", ", lambda.Parameters)}) => any"
                : $"{function.Name}(args) => any";
        }
    }
}
