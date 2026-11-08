namespace FormuLang.NET.Abstractions.Primitives
{
    public interface IType
    {
        public string Base { get; }
        public string? Extended { get; }

        public bool IsNull();
        public bool IsUndefined();

        public bool IsSimple();
        public bool IsComplex();
        public bool HasExtension();
    }
}
