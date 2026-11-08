using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Utils;
using FormuLang.NET.Core.Validation;

namespace FormuLang.NET.Core.Processing
{
    /// <summary>
    /// Tokenization stage (docs/semantics/tokenization.md).
    /// </summary>
    /// <remarks>
    /// A text literal is emitted as three tokens: an opening <c>"</c> operator, a Text token with the processed content
    /// (escape sequences resolved) and a closing <c>"</c> operator. Lines are separated by a <c>\n</c> operator token.
    /// </remarks>
    public class Tokenizer : ITokenizer
    {
        private const char TextDelimiter = '"';
        private const char EscapeSymbol = '\\';

        /// <summary>
        /// Thrown internally to stop the process with a syntax error.
        /// </summary>
        private sealed class TokenizationException(IError error) : Exception(error.Message)
        {
            public IError Error { get; } = error;
        }

        private sealed class Run(string line, int lineNumber, TokenizationState state)
        {
            private int _index;

            private char Current => line[_index];
            private char? Previous => _index > 0 ? line[_index - 1] : null;
            private char? Next => _index + 1 < line.Length ? line[_index + 1] : null;

            private TokenizationException SyntaxError(string message)
                => new(Errors.SyntaxError($"{message} (line {lineNumber}, column {_index + 1})."));

            public void Execute()
            {
                for (_index = 0; _index < line.Length; _index++)
                {
                    char c = Current;

                    if (!StringUtils.IsValidSymbol(c))
                        throw SyntaxError($"Invalid symbol U+{(int)c:X4}");

                    switch (state.Mode)
                    {
                        case TokenType.None: Start(c); break;
                        case TokenType.Operator: ContinueOperator(c); break;
                        case TokenType.Identifier: ContinueIdentifier(c); break;
                        case TokenType.Number: ContinueNumber(c); break;
                        case TokenType.Text: ContinueText(c); break;
                    }
                }

                // End of the line
                if (state.Mode == TokenType.Text)
                    throw SyntaxError("Unclosed text literal; a text literal must be a single-line character sequence");

                if (state.IsTokenOpen)
                    Close();
            }

            /// <summary>
            /// Handles a character when no token is open (mode None).
            /// </summary>
            private void Start(char c)
            {
                if (StringUtils.IsCharWhitespace(c))
                    return;

                if (StringUtils.IsCharDigit(c))
                {
                    state.Open(TokenType.Number);
                    state.Append(c);
                }
                else if (StringUtils.IsCharIdentifierStart(c))
                {
                    state.Open(TokenType.Identifier);
                    state.Append(c);
                }
                else if (c == TextDelimiter)
                {
                    state.Insert(TokenType.Operator, c.ToString());
                    state.Open(TokenType.Text);
                }
                else if (c == '.' && Next.HasValue && StringUtils.IsCharDigit(Next.Value))
                {
                    state.Open(TokenType.Number);
                    state.Append(c);
                    state.DotPresent = true;
                }
                else
                {
                    state.Open(TokenType.Operator);
                    state.Append(c);

                    // A character that cannot begin a 2-character operator is closed immediately
                    if (!CanBeginTwoCharacterOperator(c))
                        Close();
                }
            }

            private static bool CanBeginTwoCharacterOperator(char c)
                => Operators.TokenSpellings.Any(s => s.Length == 2 && s[0] == c);

            private void ContinueOperator(char c)
            {
                string candidate = state.CurrentToken + c;

                if (Operators.IsOperatorSpelling(candidate))
                {
                    state.Append(c);
                    Close();
                    return;
                }

                Close();
                Start(c);
            }

            private void ContinueIdentifier(char c)
            {
                if (StringUtils.IsCharWordCharacter(c))
                {
                    state.Append(c);
                    return;
                }

                Close();
                Start(c);
            }

            private void ContinueNumber(char c)
            {
                if (StringUtils.IsCharDigit(c) || c == '_')
                {
                    state.Append(c);
                }
                else if (c == '.')
                {
                    if (state.DotPresent || state.Hexadecimal || state.ExponentPresent)
                        throw SyntaxError($"Unexpected '.' in number literal '{state.CurrentToken}'");

                    state.Append(c);
                    state.DotPresent = true;
                }
                else if (StringUtils.IsCharLetter(c) && ExtendsNumber(c))
                {
                    state.Append(c);
                }
                else if ((c == '+' || c == '-') && (Previous == 'e' || Previous == 'E') && !state.Hexadecimal
                         && Next.HasValue && StringUtils.IsCharDigit(Next.Value))
                {
                    // Sign of the exponent
                    state.Append(c);
                }
                else if (c == TextDelimiter)
                {
                    throw SyntaxError($"Unexpected quotation mark after number literal '{state.CurrentToken}'");
                }
                else
                {
                    // Letter, white space or operator character
                    Close();
                    Start(c);
                }
            }

            /// <summary>
            /// Appending the letter keeps the token a prefix of a valid Number literal.
            /// Updates the number flags when it does.
            /// </summary>
            private bool ExtendsNumber(char c)
            {
                if (state.Hexadecimal)
                    return StringUtils.IsCharHexDigit(c);

                if ((c == 'x' || c == 'X') && state.CurrentToken == "0")
                {
                    state.Hexadecimal = true;
                    return true;
                }

                if ((c == 'e' || c == 'E') && !state.ExponentPresent)
                {
                    state.ExponentPresent = true;
                    return true;
                }

                return false;
            }

            private void ContinueText(char c)
            {
                if (state.EscapeSequence)
                {
                    char output = c switch
                    {
                        '\\' => '\\',
                        '"' => '"',
                        'n' => '\n',
                        't' => '\t',
                        _ => throw SyntaxError($"Invalid escape sequence '\\{c}'"),
                    };

                    state.Append(output);
                    state.EscapeSequence = false;
                }
                else if (c == TextDelimiter)
                {
                    state.Close();
                    state.Insert(TokenType.Operator, c.ToString());
                }
                else if (c == EscapeSymbol)
                {
                    state.EscapeSequence = true;
                }
                else
                {
                    state.Append(c);
                }
            }

            /// <summary>
            /// Closes the current token and checks that it is valid.
            /// </summary>
            private void Close()
            {
                TokenType mode = state.Mode;
                string value = state.CurrentToken;

                if (mode == TokenType.Number && !NumberValidator.IsValidNumber(value))
                    throw SyntaxError($"Invalid number literal '{value}'");

                if (mode == TokenType.Operator && !Operators.IsOperatorSpelling(value))
                    throw SyntaxError($"Unknown operator '{value}'");

                state.Close();
            }
        }

        public static TokenizationResult Tokenize(string expression)
        {
            var state = new TokenizationState();

            string[] lines = expression
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');

            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    new Run(lines[i], i + 1, state).Execute();

                    if (i < lines.Length - 1)
                        state.Insert(TokenType.Operator, "\n");
                }
            }
            catch (TokenizationException ex)
            {
                return new TokenizationResult
                {
                    Tokens = state.Tokens,
                    Error = ex.Error
                };
            }

            return new TokenizationResult
            {
                Tokens = state.Tokens,
                Error = Errors.Success()
            };
        }

        ITokenizationResult ITokenizer.Tokenize(string expression) => Tokenize(expression);
    }
}
