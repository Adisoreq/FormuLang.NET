using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Functions;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Utils;
using FormuLang.NET.Core.Validation;
using System.Globalization;
using static FormuLang.NET.Core.Standards.FunctionHelpers;

namespace FormuLang.NET.Core.Standards
{
    /// <summary>
    /// Built-in functions of the Core standard (docs/standards/core/functions). Each function follows the
    /// <b>Algorithm</b> and <b>Errors</b> sections of its document.
    /// </summary>
    public static class CoreFunctions
    {
        public static IReadOnlyList<Function> All { get; } = Create();

        private static List<Function> Create() =>
        [
            // 1. Arithmetic
            Fn("ADD",
                Overload((a, _) => N(Num(a[0]) + Num(a[1])), NumberArg("a"), NumberArg("b")),
                Overload((a, _) => T(Txt(a[0]) + Txt(a[1])), TextArg("text1"), TextArg("text2"))),
            Fn("SUB", Overload((a, _) => N(Num(a[0]) - Num(a[1])), NumberArg("a"), NumberArg("b"))),
            Fn("MUL", Overload((a, _) => N(Num(a[0]) * Num(a[1])), NumberArg("a"), NumberArg("b"))),
            Fn("DIV", Overload((a, _) => Num(a[1]) == 0
                ? OperationError("Division by zero.")
                : N(Num(a[0]) / Num(a[1])), NumberArg("a"), NumberArg("b"))),
            Fn("MOD", Overload((a, _) => Num(a[1]) == 0
                ? OperationError("Division by zero.")
                : N(Num(a[0]) % Num(a[1])), NumberArg("a"), NumberArg("b"))),
            Fn("POW", Overload((a, _) => Pow(Num(a[0]), Num(a[1])), NumberArg("base"), NumberArg("exp"))),
            Fn("EXP", Overload((a, _) => N(Math.Exp(Num(a[0]))), NumberArg("base"))),
            Fn("ROOT", Overload((a, _) => Root(Num(a[0]), Num(a[1])), NumberArg("num"), NumberArg("n"))),
            Fn("SQRT", Overload((a, _) => Num(a[0]) < 0
                ? OperationError("Square root of a negative number.")
                : N(Math.Sqrt(Num(a[0]))), NumberArg("num"))),
            Fn("LOG", Overload((a, _) => Log(Num(a[0]), Num(a[1])), NumberArg("value"), NumberArg("base"))),
            Fn("LN", Overload((a, _) => Num(a[0]) <= 0
                ? OperationError("Logarithm of a number that is not greater than 0.")
                : N(Math.Log(Num(a[0]))), NumberArg("value"))),
            Fn("ABS", Overload((a, _) => N(Math.Abs(Num(a[0]))), NumberArg("num"))),
            Fn("NEG", Overload((a, _) => N(0 - Num(a[0])), NumberArg("num"))),
            Fn("CEIL", Overload((a, _) => Round(Num(a[0]), Num(a[1]), Math.Ceiling, Math.Ceiling), NumberArg("number"), NumberArg("precision"))),
            Fn("FLOOR", Overload((a, _) => Round(Num(a[0]), Num(a[1]), Math.Floor, Math.Floor), NumberArg("number"), NumberArg("precision"))),
            Fn("ROUND", Overload((a, _) => Round(Num(a[0]), Num(a[1]),
                d => Math.Round(d, MidpointRounding.AwayFromZero),
                d => Math.Round(d, MidpointRounding.AwayFromZero)), NumberArg("number"), NumberArg("precision"))),
            Fn("CLAMP", Overload((a, _) => Num(a[1]) > Num(a[2])
                ? OperationError("\"min\" is greater than \"max\".")
                : N(Math.Clamp(Num(a[0]), Num(a[1]), Num(a[2]))), NumberArg("value"), NumberArg("min"), NumberArg("max"))),

            // 2. Logic and comparison
            Fn("AND", Overload((a, _) => B(Bool(a[0]) && Bool(a[1])), BooleanArg("a"), BooleanArg("b"))),
            Fn("OR", Overload((a, _) => B(Bool(a[0]) || Bool(a[1])), BooleanArg("a"), BooleanArg("b"))),
            Fn("XOR", Overload((a, _) => B(Bool(a[0]) ^ Bool(a[1])), BooleanArg("a"), BooleanArg("b"))),
            Fn("NOT", Overload((a, _) => B(!Bool(a[0])), BooleanArg("val"))),
            Fn("EQU", Overload((a, _) => B(ValuesEqual(a[0], a[1])), AnyArg("a"), AnyArg("b"))),
            Fn("NEQ", Overload((a, _) => B(!ValuesEqual(a[0], a[1])), AnyArg("a"), AnyArg("b"))),
            Fn("GRE", Overload((a, _) => B(Num(a[0]) > Num(a[1])), NumberArg("a"), NumberArg("b"))),
            Fn("LES", Overload((a, _) => B(Num(a[0]) < Num(a[1])), NumberArg("a"), NumberArg("b"))),
            Fn("GEQ", Overload((a, _) => B(Num(a[0]) >= Num(a[1])), NumberArg("a"), NumberArg("b"))),
            Fn("LEQ", Overload((a, _) => B(Num(a[0]) <= Num(a[1])), NumberArg("a"), NumberArg("b"))),

            // 3. Control flow
            Fn("IF", Overload((a, _) => Bool(a[0]) ? a[1] : a[2], BooleanArg("condition"), AnyArg("then"), AnyArg("else"))),
            Fn("ERROR", Overload((a, _) => ErrorValue(Errors.CustomError(Txt(a[0]))), TextArg("message"))),

            // 4. Text
            Fn("CONCAT", Overload((a, _) => T(Txt(a[0]) + Txt(a[1])), TextArg("text1"), TextArg("text2"))),
            Fn("TRIM", Overload((a, _) => T(Txt(a[0]).Trim(' ', '\t', '\n', '\r')), TextArg("text"))),
            Fn("UPPERCASE", Overload((a, _) => T(Txt(a[0]).ToUpperInvariant()), TextArg("text"))),
            Fn("LOWERCASE", Overload((a, _) => T(Txt(a[0]).ToLowerInvariant()), TextArg("text"))),
            Fn("JOIN", Overload((a, _) => Join(Arr(a[0]), a.Count > 1 ? Txt(a[1]) : ""), ArrayArg("array"), TextArg("separator", optional: true))),
            Fn("SPLIT", Overload((a, _) => Split(Txt(a[0]), Txt(a[1])), TextArg("text"), TextArg("separator"))),

            // 5. Arrays and collections
            Fn("ARRAY", Overload((a, _) => A(a), ArgumentTemplate.Any("elements", optional: true, multiple: true))),
            Fn("GET",
                Overload((a, c) => TryResolveIndex(Num(a[1]), Arr(a[0]).Length, c, out int i, out var error) ? Arr(a[0])[i] : error!,
                    ArrayArg("array"), NumberArg("index")),
                Overload((a, _) => GetKey(Obj(a[0]), Txt(a[1])), ObjectArg("object"), TextArg("key"))),
            Fn("SUM", Overload((a, _) => N(Arr(a[0]).Sum(Num)), NumberArrayArg("array"))),
            Fn("PROD", Overload((a, _) => N(Arr(a[0]).Aggregate(1.0, (p, e) => p * Num(e))), NumberArrayArg("array"))),
            Fn("AVG", Overload((a, _) => Arr(a[0]).Length == 0
                ? OperationError("The array is empty.")
                : N(Arr(a[0]).Average(Num)), NumberArrayArg("array"))),
            Fn("LEN", Overload((a, _) => N(Arr(a[0]).Length), ArrayArg("array"))),
            Extreme("MAX", (x, y) => x >= y),
            Extreme("MIN", (x, y) => x <= y),
            Fn("RANGE", Overload((a, c) => Range(Arr(a[0]), Num(a[1]), Num(a[2]), c),
                ArrayArg("array"), NumberArg("first_index"), NumberArg("last_index"))),
            Fn("MAP", Overload((a, c) => Map(Arr(a[0]), Del(a[1]), c), ArrayArg("array"), DelegateArg("lambda"))),
            Fn("FILTER", Overload((a, c) => Filter(Arr(a[0]), Del(a[1]), c), ArrayArg("array"), DelegateArg("lambda"))),

            // 6. Types
            IsType("ISNULL", Types.NullName),
            IsType("ISBOOL", Types.BooleanName),
            IsType("ISNUM", Types.NumberName),
            IsType("ISTEXT", Types.TextName),
            IsType("ISERR", Types.ErrorName),
            IsType("ISARR", Types.ArrayName),
            IsType("ISOBJ", Types.ObjectName),
            IsType("ISDEL", Types.DelegateName),

            // 7. Conversion
            Fn("BOOL",
                Overload((_, _) => B(false), ArgumentTemplate.Of("value", Types.Null)),
                Overload((a, _) => a[0], BooleanArg("value")),
                Overload((a, _) => B(Num(a[0]) != 0), NumberArg("value")),
                Overload((a, _) => B(Txt(a[0]).Length > 0), TextArg("value")),
                Overload((a, _) => B(Arr(a[0]).Length > 0), ArrayArg("value")),
                Overload((a, _) => B(!Obj(a[0]).IsEmpty()), ObjectArg("value"))),
            Fn("NUMBER",
                Overload((_, _) => N(0), ArgumentTemplate.Of("value", Types.Null)),
                Overload((a, _) => N(Bool(a[0]) ? 1 : 0), BooleanArg("value")),
                Overload((a, _) => a[0], NumberArg("value")),
                Overload((a, _) => N(ParseNumber(Txt(a[0]))), TextArg("value"))),
            Fn("TEXT", new[] { Types.Null, Types.Boolean, Types.Number, Types.Text, Types.Array, Types.Object, Types.Delegate }
                .Select(type => Overload((a, _) => T(StringConverter.ValueToString(a[0])), ArgumentTemplate.Of("value", type)))
                .ToArray()),

            // Target of the object literal translation: { k1: a, "k2": b } is OBJECT("k1",a,"k2",b)
            Fn("OBJECT", Overload((a, _) => CreateObject(a), ArgumentTemplate.Any("entries", optional: true, multiple: true))),
        ];

        // Arithmetic

        private static IValue Pow(double b, double e)
        {
            if (b == 0 && e < 0)
                return OperationError("0 cannot be raised to a negative power.");

            if (b < 0 && !IsWhole(e))
                return OperationError("A negative number raised to a fractional power is not a real number.");

            return N(Math.Pow(b, e));
        }

        private static IValue Root(double num, double n)
        {
            if (n == 0)
                return OperationError("The 0-th root is not defined.");

            if (num < 0)
            {
                if (IsWhole(n) && Math.Abs(n % 2) == 1)
                    return N(-RealRoot(-num, n));

                return OperationError("Root of a negative number is not a real number.");
            }

            return N(RealRoot(num, n));
        }

        private static double RealRoot(double num, double n)
        {
            double result = n switch
            {
                2 => Math.Sqrt(num),
                3 => Math.Cbrt(num),
                _ => Math.Pow(num, 1 / n),
            };

            // Prefer the exact whole root when there is one, e.g. ROOT(16, 4) = 2
            double rounded = Math.Round(result);
            return IsWhole(n) && n > 0 && Math.Pow(rounded, n) == num ? rounded : result;
        }

        private static IValue Log(double value, double b)
        {
            if (value <= 0)
                return OperationError("Logarithm of a number that is not greater than 0.");

            if (b <= 0 || b == 1)
                return OperationError("The base of a logarithm must be greater than 0 and different from 1.");

            return N(b switch
            {
                10 => Math.Log10(value),
                2 => Math.Log2(value),
                _ => Math.Log(value) / Math.Log(b),
            });
        }

        /// <summary>
        /// Rounds to <paramref name="precision"/> decimal places; a negative precision rounds to tens, hundreds, etc.
        /// Uses decimal arithmetic when possible, so 4.2 stays 4.2 and is not rounded up to 4.3.
        /// </summary>
        private static IValue Round(double number, double precision, Func<decimal, decimal> decimalOp, Func<double, double> doubleOp)
        {
            if (!IsWhole(precision))
                return OperationError("\"precision\" is not a whole number.");

            if (Math.Abs(precision) <= 20)
            {
                try
                {
                    int p = (int)precision;
                    decimal factor = 1;
                    for (int i = 0; i < Math.Abs(p); i++)
                        factor *= 10;

                    decimal d = (decimal)number;
                    decimal scaled = p >= 0 ? d * factor : d / factor;
                    decimal rounded = decimalOp(scaled);
                    return N((double)(p >= 0 ? rounded / factor : rounded * factor));
                }
                catch (OverflowException)
                {
                    // Out of the decimal range: fall back to double arithmetic
                }
            }

            double f = Math.Pow(10, precision);
            return N(doubleOp(number * f) / f);
        }

        // Comparison

        /// <summary>
        /// docs/standards/core/functions/EQU.md
        /// </summary>
        public static bool ValuesEqual(IValue a, IValue b)
        {
            if (a.Type.Base != b.Type.Base)
                return false;

            switch (a.Type.Base)
            {
                case Types.NullName:
                    return true;

                case Types.BooleanName:
                case Types.NumberName:
                case Types.TextName:
                    return Equals(a.Raw, b.Raw);

                case Types.ArrayName:
                    IValue[] x = Arr(a), y = Arr(b);
                    return x.Length == y.Length && x.Zip(y).All(pair => ValuesEqual(pair.First, pair.Second));

                case Types.ObjectName:
                    IObject o1 = Obj(a), o2 = Obj(b);
                    return a.Type.Extended == b.Type.Extended
                        && o1.Keys.Order().SequenceEqual(o2.Keys.Order())
                        && o1.Keys.All(k => ValuesEqual(o1.GetProperty(k).GetValue(), o2.GetProperty(k).GetValue()));

                case Types.DelegateName:
                    return ReferenceEquals(a.Raw, b.Raw);

                case Types.ErrorName:
                    IError e1 = (IError)a.Raw!, e2 = (IError)b.Raw!;
                    return e1.Code == e2.Code && e1.Message == e2.Message;

                default:
                    return false;
            }
        }

        // Text

        private static IValue Join(IValue[] elements, string separator)
        {
            if (elements.FirstOrDefault(IsError) is IValue error)
                return error;

            return T(string.Join(separator, elements.Select(StringConverter.ValueToString)));
        }

        private static IValue Split(string text, string separator)
        {
            IEnumerable<string> parts = separator.Length == 0
                ? text.Select(c => c.ToString())
                : text.Split(separator);

            return A(parts.Select(T));
        }

        /// <summary>
        /// A valid Number literal, optionally with a sign; any other text is 0.
        /// </summary>
        private static double ParseNumber(string text)
        {
            string trimmed = text.Trim();
            double sign = 1;

            if (trimmed.StartsWith('-') || trimmed.StartsWith('+'))
            {
                sign = trimmed[0] == '-' ? -1 : 1;
                trimmed = trimmed[1..];
            }

            return NumberValidator.TryParse(trimmed, out double value) ? sign * value : 0;
        }

        // Arrays and objects

        private static IValue GetKey(IObject obj, string key)
        {
            if (!obj.HasKey(key))
                return Value.Error(Errors.InvalidKeyError($"Key \"{key}\" does not exist."), "");

            return obj.GetProperty(key).GetValue();
        }

        private static IValue Range(IValue[] array, double first, double last, Abstractions.Structures.IContext context)
        {
            if (!TryResolveIndex(first, array.Length, context, out int from, out var error) ||
                !TryResolveIndex(last, array.Length, context, out int to, out error))
                return error!;

            if (from > to)
                return InvalidIndexError("\"first_index\" is greater than \"last_index\".");

            return A(array[from..(to + 1)]);
        }

        private static IValue Map(IValue[] array, IFunction lambda, Abstractions.Structures.IContext context)
        {
            if (CheckHandler(lambda, "MAP") is IValue error)
                return error;

            var results = new List<IValue>(array.Length);

            foreach (IValue element in array)
            {
                IValue result = Call(lambda, element, context);
                if (IsError(result))
                    return result;

                results.Add(result);
            }

            return A(results);
        }

        private static IValue Filter(IValue[] array, IFunction lambda, Abstractions.Structures.IContext context)
        {
            if (CheckHandler(lambda, "FILTER") is IValue error)
                return error;

            var results = new List<IValue>();

            foreach (IValue element in array)
            {
                IValue result = Call(lambda, element, context);
                if (IsError(result))
                    return result;

                if (result.Raw is not bool keep || result.Type.Base != Types.BooleanName)
                    return OperationError("The lambda passed to \"FILTER\" must return a Boolean.");

                if (keep)
                    results.Add(element);
            }

            return A(results);
        }

        /// <summary>
        /// MAX and MIN: (a, b), (...numbers), (numbers &lt;Array&lt;Number&gt;&gt;), (elements &lt;Array&gt;, handler &lt;Delegate&gt;).
        /// </summary>
        /// <param name="better">Returns true if the first number should be kept.</param>
        private static Function Extreme(string name, Func<double, double, bool> better)
        {
            double Pick(IEnumerable<double> numbers) => numbers.Aggregate((x, y) => better(x, y) ? x : y);

            return Fn(name,
                Overload((a, _) => N(Pick([Num(a[0]), Num(a[1])])), NumberArg("a"), NumberArg("b")),
                Overload((a, _) => N(Pick(a.Select(Num))), ArgumentTemplate.Of("numbers", Types.Number, multiple: true)),
                Overload((a, _) => Arr(a[0]).Length == 0
                    ? OperationError("The array is empty.")
                    : N(Pick(Arr(a[0]).Select(Num))), NumberArrayArg("numbers")),
                Overload((a, c) =>
                {
                    IValue[] elements = Arr(a[0]);
                    IFunction handler = Del(a[1]);

                    if (CheckHandler(handler, name) is IValue error)
                        return error;

                    if (elements.Length == 0)
                        return OperationError("The array is empty.");

                    var numbers = new List<double>(elements.Length);

                    foreach (IValue element in elements)
                    {
                        IValue result = Call(handler, element, c);
                        if (IsError(result))
                            return result;

                        if (result.Raw is not double number)
                            return OperationError($"The handler passed to \"{name}\" must return a Number.");

                        numbers.Add(number);
                    }

                    return N(Pick(numbers));
                }, ArrayArg("elements"), DelegateArg("handler")));
        }

        private static IValue CreateObject(IReadOnlyList<IValue> entries)
        {
            if (entries.Count % 2 != 0)
                return ArgumentError("\"OBJECT\" expects key-value pairs.");

            var obj = new Primitives.Object();

            for (int i = 0; i < entries.Count; i += 2)
            {
                if (entries[i].Raw is not string key || !IdentifierValidator.Validate(key))
                    return ArgumentError("An object key must be a Text that is a valid identifier.");

                if (!obj.InitVariable(key, entries[i + 1]))
                    return ArgumentError($"Key \"{key}\" is defined more than once.");
            }

            return Value.Object(obj, "");
        }

        // Types

        private static Function IsType(string name, string baseType)
            => Fn(name, Overload((a, _) => B(a[0].Type.Base == baseType), AnyArg("value")));
    }
}
