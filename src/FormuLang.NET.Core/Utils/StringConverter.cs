using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;
using System.Globalization;
using System.Text;

namespace FormuLang.NET.Core.Utils
{
    /// <summary>
    /// Serialization parameters (docs/standards/core/semantics/serialization.md). They are environment variables, so a
    /// formula can override them for the current execution, e.g. <c>@decimal_place = 2</c>.
    /// </summary>
    public sealed record SerializationOptions
    {
        public const string DecimalPlaceName = "decimal_place";
        public const string SplitArrayName = "split_array";
        public const string SplitObjectName = "split_object";
        public const string IncludeObjectClassName = "include_object_class";
        public const string MaxArrayPreviewName = "max_array_preview";

        /// <summary>
        /// Decimal place at which serialized numbers end. A negative value does not limit the notation.
        /// </summary>
        public int DecimalPlace { get; init; } = -1;

        /// <summary>
        /// Places every serialized array element in a new line.
        /// </summary>
        public bool SplitArray { get; init; }

        /// <summary>
        /// Places every serialized key-value pair in a new line.
        /// </summary>
        public bool SplitObject { get; init; }

        /// <summary>
        /// Precedes serialized objects with their class notation, e.g. <c>[Stock.Candle]</c>.
        /// </summary>
        public bool IncludeObjectClass { get; init; }

        /// <summary>
        /// Maximal number of serialized array elements. A negative value does not limit arrays.
        /// </summary>
        public int MaxArrayPreview { get; init; } = -1;

        /// <summary>
        /// Initial values of the Core serialization variables.
        /// </summary>
        public static SerializationOptions Default { get; } = new();

        /// <summary>
        /// Parameters of the current execution: the environment variables of the context (which the formula may have
        /// overridden), then the ones of the environment, then the default values.
        /// </summary>
        public static SerializationOptions FromContext(IContext context)
        {
            IValue? Read(string name)
            {
                if (context.EnvironmentVariables.HasKey(name))
                    return context.EnvironmentVariables.GetProperty(name).GetValue();

                if (context.Environment.HasKey(name))
                    return context.Environment.GetProperty(name).GetValue();

                return null;
            }

            return FromVariables(Read);
        }

        /// <summary>
        /// Parameters stored in the variables of the environment.
        /// </summary>
        public static SerializationOptions FromEnvironment(IEnvironment environment)
            => FromVariables(name => environment.HasKey(name) ? environment.GetProperty(name).GetValue() : null);

        private static SerializationOptions FromVariables(Func<string, IValue?> read)
        {
            int Whole(string name, int fallback)
                => read(name)?.Raw is double d && double.IsFinite(d) && Math.Floor(d) == d
                    ? (int)Math.Clamp(d, int.MinValue, int.MaxValue)
                    : fallback;

            bool Flag(string name, bool fallback) => read(name)?.Raw is bool b ? b : fallback;

            return new SerializationOptions
            {
                DecimalPlace = Whole(DecimalPlaceName, Default.DecimalPlace),
                SplitArray = Flag(SplitArrayName, Default.SplitArray),
                SplitObject = Flag(SplitObjectName, Default.SplitObject),
                IncludeObjectClass = Flag(IncludeObjectClassName, Default.IncludeObjectClass),
                MaxArrayPreview = Whole(MaxArrayPreviewName, Default.MaxArrayPreview),
            };
        }
    }

    /// <summary>
    /// Serialization of values (docs/standards/core/semantics/serialization.md).
    /// </summary>
    public static class StringConverter
    {
        private const string NullStr = "null";
        private const string UndefinedStr = "undefined";
        private const string Indent = "  ";
        private const string Omitted = "...";

        /// <summary>
        /// Serializes the value with the default parameters.
        /// </summary>
        public static string ValueToString(IValue value) => ValueToString(value, SerializationOptions.Default);

        public static string ValueToString(IValue value, SerializationOptions options) => Serialize(value, options, nested: false);

        /// <param name="nested">Text inside an array or object is written as a quoted literal.</param>
        private static string Serialize(IValue value, SerializationOptions options, bool nested)
        {
            switch (value.Type.Base)
            {
                case Types.NullName:
                    return NullStr;

                case Types.UndefinedName:
                    return UndefinedStr;

                case Types.BooleanName:
                    return BooleanToString(value.Raw is true);

                case Types.NumberName:
                    return NumberToString(value.Raw is double d ? d : 0, options.DecimalPlace);

                case Types.TextName:
                    string text = value.Raw as string ?? string.Empty;
                    return nested ? TextToLiteral(text) : text;

                case Types.ErrorName:
                    return ErrorToString(value.Raw as IError);

                case Types.ArrayName:
                    return ArrayToString(value.Raw as IValue[] ?? [], options);

                case Types.ObjectName:
                    return ObjectToString(value.Raw as IObject, value.Type.Extended, options);

                case Types.DelegateName:
                    return DelegateToString(value.Raw as IFunction);

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

        /// <summary>
        /// Decimal notation filled or rounded (half away from zero) at the <paramref name="decimalPlace"/>-th decimal
        /// place. A negative <paramref name="decimalPlace"/> does not limit the notation.
        /// </summary>
        public static string NumberToString(double value, int decimalPlace)
        {
            if (decimalPlace < 0)
                return NumberToString(value);

            string result;

            if (decimalPlace <= 28 && Math.Abs(value) < 7.9e27)
            {
                decimal rounded = Math.Round((decimal)value, decimalPlace, MidpointRounding.AwayFromZero);
                result = rounded.ToString("F" + decimalPlace, CultureInfo.InvariantCulture);
            }
            else
            {
                result = value.ToString("F" + Math.Min(decimalPlace, 100), CultureInfo.InvariantCulture);
                if (decimalPlace > 100)
                    result += new string('0', decimalPlace - 100);
            }

            // A number rounded to zero is never written with a sign
            return result.StartsWith('-') && result.Skip(1).All(c => c == '0' || c == '.') ? result[1..] : result;
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

        public static string ArrayToString(IValue[] value) => ArrayToString(value, SerializationOptions.Default);

        public static string ArrayToString(IValue[] value, SerializationOptions options)
        {
            if (value.Length == 0)
                return "[]";

            IEnumerable<string> elements;
            int k = options.MaxArrayPreview;

            if (k < 0 || value.Length <= k)
            {
                elements = value.Select(v => Serialize(v, options, nested: true));
            }
            else
            {
                int head = (k + 1) / 2, tail = k / 2;

                elements = value.Take(head).Select(v => Serialize(v, options, nested: true))
                    .Append(Omitted)
                    .Concat(value.Skip(value.Length - tail).Select(v => Serialize(v, options, nested: true)));
            }

            return Enclose("[", elements, "]", options.SplitArray, padding: "");
        }

        public static string ObjectToString(IObject? value) => ObjectToString(value, null, SerializationOptions.Default);

        /// <param name="extendedType">Extended type of the object value, used by the class notation.</param>
        public static string ObjectToString(IObject? value, string? extendedType, SerializationOptions options)
        {
            string prefix = options.IncludeObjectClass ? $"[{extendedType ?? Types.ObjectName}] " : "";

            if (value == null || value.IsEmpty())
                return prefix + "{ }";

            var pairs = value.Keys.Select(key => $"{key}: {Serialize(value.GetProperty(key).GetValue(), options, nested: true)}");
            return prefix + Enclose("{", pairs, "}", options.SplitObject, padding: " ");
        }

        /// <summary>
        /// Joins the parts with commas, either in one line or one part per line, indented.
        /// </summary>
        private static string Enclose(string open, IEnumerable<string> parts, string close, bool split, string padding)
        {
            if (!split)
                return open + padding + string.Join(", ", parts) + padding + close;

            var lines = parts.Select(part => Indent + part.Replace("\n", "\n" + Indent));
            return open + "\n" + string.Join(",\n", lines) + "\n" + close;
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
