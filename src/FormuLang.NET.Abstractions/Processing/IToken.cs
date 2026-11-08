using FormuLang.NET.Abstractions.Operators;

namespace FormuLang.NET.Abstractions.Processing
{
    public interface IToken
    {
        public TokenType Type { get; }
        public string Value { get; }

        public bool IsOperator() => Type == TokenType.Operator;
    }
}
