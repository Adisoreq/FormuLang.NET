using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Abstractions.Processing
{
    public interface ICleanUpResult : IOperationResult
    {
        public string Result { get; }
    }
}
