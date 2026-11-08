namespace FormuLang.NET.Abstractions.Primitives
{
    public interface IObject
    {
        public IEnumerable<string> Keys { get; }

        public bool IsEmpty();
        public bool HasKey(string key);
        public IContainer GetProperty(string key);
        public IError SetProperty(string key, IValue value);

        public bool InitConstant(string key, IValue value);
        public bool InitVariable(string key, IValue value);
    }
}
