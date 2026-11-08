using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Core.Processing
{
    public class TokenizationResult : ITokenizationResult
    {
        public IEnumerable<IToken> Tokens { get; set; } = Array.Empty<IToken>();
        public IError Error { get; set; } = Errors.Success();
    }
}
