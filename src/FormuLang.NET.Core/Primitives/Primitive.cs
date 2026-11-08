using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Core.Primitives
{
    public abstract class Primitive : IPrimitive
    {
        public PrimitiveType PrimitiveType { get; protected set; }
        public IType Type { get; protected set; }
        public string Expression { get; protected set; } = string.Empty;

        protected Primitive(PrimitiveType primitiveType, IType type, string expression)
        {
            this.PrimitiveType = primitiveType;
            this.Type = type;
            this.Expression = expression;
        }

        public abstract bool CanAccessValue { get; }
        public abstract IError CanUpdateValue { get; }

        public abstract IValue GetValue();
        public abstract IError SetValue(IValue value);
    }
}
