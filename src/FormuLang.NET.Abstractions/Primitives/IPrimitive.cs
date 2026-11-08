namespace FormuLang.NET.Abstractions.Primitives
{
    public interface IPrimitive
    {
        public PrimitiveType PrimitiveType { get; }
        public IType Type { get; }
        public string Expression { get; }

        public bool CanAccessValue { get; }
        public IValue GetValue();

        public IError CanUpdateValue { get; }
        public IError SetValue(IValue value);
    }
}