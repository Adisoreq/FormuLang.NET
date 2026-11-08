using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Utils;

namespace FormuLang.NET.Core.Primitives
{
    public class Value : Primitive, IValue
    {
        public object? Raw { get; private set; }

        public Value(object? value, IType type, string expression)
            : base(PrimitiveType.Value, type, expression)
        {
            this.Raw = value;
        }

        public override bool CanAccessValue => true;

        public override IError CanUpdateValue => Errors.Success();

        public override IValue GetValue() => this;

        /// <summary>
        /// A value can be overridden by a value of the same type, or by any value if it is Undefined.
        /// </summary>
        public IError CheckOverridability(IValue value)
        {
            bool result = Types.AreEqual(this.Type, value.Type) || this.Type.IsUndefined();
            return result ? Errors.Success() : Errors.TypeError($"Cannot override value of type {this.Type} with value of type {value.Type}.");
        }

        public override IError SetValue(IValue value)
        {
            IError result = CheckOverridability(value);

            if (result.IsSuccess())
            {
                Raw = value.Raw;
                Type = value.Type;
                Expression = value.Expression;
            }

            return result;
        }

        public T? As<T>()
        {
            return Raw is T value ? value : default;
        }

        public bool? AsBoolean() => Raw is bool v ? v : null;
        public double? AsNumber() => Raw is double v ? v : null;
        public string? AsText() => As<string>();
        public IError? AsError() => As<IError>();
        public IValue[]? AsArray() => As<IValue[]>();
        public IObject? AsObject() => As<IObject>();
        public IFunction? AsDelegate() => As<IFunction>();

        public bool TryAs<T>(out T? value)
        {
            if (Raw is T v)
            {
                value = v;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryAsBoolean(out bool value) => TryAs(out value);
        public bool TryAsNumber(out double value) => TryAs(out value);
        public bool TryAsText(out string? value) => TryAs(out value);
        public bool TryAsError(out IError? value) => TryAs(out value);
        public bool TryAsArray(out IValue[]? value) => TryAs(out value);
        public bool TryAsObject(out IObject? value) => TryAs(out value);
        public bool TryAsDelegate(out IFunction? value) => TryAs(out value);

        public static Value Null(string expression = "")
            => new Value(null, Types.Null, expression);

        /// <summary>
        /// State of a variable that is declared but has no value. It is never a value of an expression.
        /// </summary>
        public static Value Undefined(string expression = "")
            => new Value(null, Types.Undefined, expression);

        public static Value Boolean(bool boolean, string expression)
            => new Value(boolean, Types.Boolean, expression);

        public static Value Number(double number, string expression)
            => new Value(number, Types.Number, expression);

        public static Value Text(string text, string expression)
            => new Value(text, Types.Text, expression);

        public static Value Error(IError error, string expression)
            => new Value(error, Types.Error, expression);

        public static Value Array(IEnumerable<IValue> array, string expression)
            => new Value(array.ToArray(), Types.Array, expression);

        public static Value Object(IObject obj, string expression)
            => new Value(obj, Types.Object, expression);

        /// <summary>
        /// Object value with an extended type, e.g. <c>Stock.Candle</c>.
        /// </summary>
        public static Value Object(IObject obj, string extendedType, string expression)
            => new Value(obj, Types.Of(Types.ObjectName, extendedType), expression);

        public static Value Delegate(IFunction function, string expression)
            => new Value(function, Types.Delegate, expression);

        public override string ToString()
        {
            return StringConverter.ValueToString(this);
        }
    }
}
