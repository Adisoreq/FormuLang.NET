namespace FormuLang.NET.Abstractions.Primitives
{
    public interface IValue : IPrimitive
    {
        public object? Raw { get; }

        public T? As<T>();
        public bool TryAs<T>(out T? value);
    }
}