using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Core.Primitives
{
    public class Variable : Container
    {
        public Variable(string name, IValue value) :
            base(name, value, ContainerType.Variable, true) { }

        /// <summary>
        /// Declares a variable with no value (Undefined).
        /// </summary>
        public Variable(string name) : this(name, Value.Undefined(name)) { }

        public override IError CanUpdateValue => Errors.Success();

        /// <summary>
        /// Stores the value. A variable with a value can only be overridden with a value of the same base type.
        /// </summary>
        public override IError SetValue(IValue value)
        {
            if (!StoredValue.Type.IsUndefined() && StoredValue.Type.Base != value.Type.Base)
            {
                string err = $"Trying to override {StoredValue.Type.Base.ToUpperInvariant()} value with {value.Type.Base.ToUpperInvariant()}.";
                return Errors.TypeError(err);
            }

            Store(value);
            return Errors.Success();
        }
    }
}
