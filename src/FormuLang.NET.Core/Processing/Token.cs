using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Abstractions.Operators;

namespace FormuLang.NET.Core.Processing
{
    internal class Token : IToken
    {
        public TokenType Type { get; private set; }
        public string Value { get; private set; }

        internal Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }

        private string DisplayValue => 
            Value.Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");

        public override string ToString()
        {
            return $"({Type}: '{DisplayValue}')";
        }
    }
}
