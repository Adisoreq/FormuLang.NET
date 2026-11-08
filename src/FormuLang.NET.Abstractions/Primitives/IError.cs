namespace FormuLang.NET.Abstractions.Primitives
{
    public interface IError
    {
        public uint Code { get; }
        public string Name { get; }
        public string Message { get; }

        public bool IsSuccess();
        public bool IsFailed();
    }
}