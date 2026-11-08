using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Core.Primitives
{
    public abstract class Container : Primitive, IContainer
    {
        private IValue _value;

        public string Name { get; protected set; }

        public ContainerType ContainerType { get; protected set; }
        public bool Mutable { get; protected set; }

        protected Container(string name, IValue value, ContainerType containerType, bool mutable)
            : base(PrimitiveType.Container, value.Type, value.Expression)
        {
            _value = value;
            Name = name;
            ContainerType = containerType;
            Mutable = mutable;
        }

        /// <summary>
        /// False if the container is declared but has no value (Undefined).
        /// </summary>
        public override bool CanAccessValue => !_value.Type.IsUndefined();

        /// <summary>
        /// The stored value, or an <c>UNDEFINED_ERROR</c> value if the container has no value.
        /// </summary>
        public override IValue GetValue()
        {
            if (_value.Type.IsUndefined())
                return Value.Error(Errors.UndefinedError($"Variable \"{Name}\" has no value."), Name);

            return _value;
        }

        /// <summary>
        /// Replaces the stored value and keeps <see cref="Primitive.Type"/> and <see cref="Primitive.Expression"/> in sync with it.
        /// </summary>
        protected void Store(IValue value)
        {
            _value = value;
            Type = value.Type;
            Expression = value.Expression;
        }

        protected IValue StoredValue => _value;

        public override string ToString() => _value.ToString() ?? string.Empty;
    }
}
