using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Abstractions.Processing
{
    public interface ITokenizationResult : IOperationResult
    {
        public IEnumerable<IToken> Tokens { get; }
    }
}
