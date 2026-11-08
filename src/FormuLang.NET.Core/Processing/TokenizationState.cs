using FormuLang.NET.Abstractions.Processing;
using System.Text;

namespace FormuLang.NET.Core.Processing
{
    /// <summary>
    /// Tokenization state (docs/semantics/tokenization.md, "Tokenization state").
    /// </summary>
    internal class TokenizationState
    {
        public List<Token> Tokens { get; } = new();

        public TokenType Mode { get; private set; } = TokenType.None;

        private readonly StringBuilder _current = new();
        public string CurrentToken => _current.ToString();
        public int CurrentLength => _current.Length;
        public bool IsTokenOpen => Mode != TokenType.None;

        // Number flags
        public bool DotPresent { get; set; }
        public bool ExponentPresent { get; set; }
        public bool Hexadecimal { get; set; }

        // Text flags
        public bool EscapeSequence { get; set; }

        public void Open(TokenType mode)
        {
            if (IsTokenOpen)
                throw new InvalidOperationException("A token is already open.");

            Mode = mode;
            _current.Clear();
        }

        public void Append(char c)
        {
            if (!IsTokenOpen)
                throw new InvalidOperationException("No token is open.");

            _current.Append(c);
        }

        /// <summary>
        /// Ends the current token, adds it to the tokens and resets the mode and flags.
        /// </summary>
        public Token Close()
        {
            if (!IsTokenOpen)
                throw new InvalidOperationException("No token is open.");

            var token = new Token(Mode, _current.ToString());
            Tokens.Add(token);

            Mode = TokenType.None;
            _current.Clear();
            DotPresent = false;
            ExponentPresent = false;
            Hexadecimal = false;
            EscapeSequence = false;

            return token;
        }

        public void Insert(TokenType type, string value) => Tokens.Add(new Token(type, value));
    }
}
