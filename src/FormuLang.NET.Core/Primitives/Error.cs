using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Core.Primitives
{
    public readonly record struct Error : IError
    {
        public readonly uint Code { get; }
        public readonly string Name { get; }
        public readonly string Message { get; }

        public Error(uint code, string name, string message)
        {
            Code = code;
            Name = name;
            Message = message;
        }

        public readonly bool IsSuccess() => Code == 0;
        public readonly bool IsFailed() => Code != 0;

        public override string ToString()
        {
            return $"[{Name} ({Code})] {Message}";
        }
    }
}