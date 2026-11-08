using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Functions;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Standards;
using FormuLang.NET.Core.Utils;
using FormuLang.NET.Core.Validation;

namespace FormuLang.NET.Core.Processing
{
    /// <summary>
    /// Parsing stage required by the Core standard (docs/standards/core/semantics/parsing.md): a Pratt parser that
    /// splits the tokens into sequences, builds one tree per sequence and translates every operator into a function call.
    /// </summary>
    public class Parser : IParser
    {
        /// <summary>
        /// Operator translation (docs/standards/core/semantics/parsing.md, "Operator translation").
        /// </summary>
        public static IReadOnlyDictionary<string, string> InfixTranslation { get; } = new Dictionary<string, string>
        {
            ["+"] = "ADD", ["-"] = "SUB", ["*"] = "MUL", ["/"] = "DIV", ["%"] = "MOD", ["^"] = "POW",
            ["=="] = "EQU", ["!="] = "NEQ", ["<>"] = "NEQ", [">"] = "GRE", ["<"] = "LES", [">="] = "GEQ", ["<="] = "LEQ",
            ["&&"] = "AND", ["||"] = "OR",
            ["="] = "SET", [":="] = "INIT", ["?="] = "COND_SET",
        };

        private static readonly HashSet<string> Comparisons = [">", "<", ">=", "<=", "==", "<>", "!="];
        private static readonly HashSet<string> Assignments = ["=", ":=", "?="];

        private readonly IReadOnlyDictionary<string, int> _procedures;

        /// <summary>
        /// Parser for the Core standard.
        /// </summary>
        public Parser() : this(CoreProcedures.All) { }

        /// <param name="functions">Functions of the active standards; their procedures can be called without parentheses.</param>
        public Parser(IEnumerable<IFunction> functions)
        {
            _procedures = functions.OfType<Procedure>().ToDictionary(p => p.Name, p => p.ParameterCount);
        }

        public static Parser ForEnvironment(IEnvironment environment) => new(environment.Functions);

        public ParsingResult Parse(IEnumerable<IToken> tokens)
        {
            try
            {
                var sequences = new List<Sequence>();

                foreach (List<IToken> sequenceTokens in SplitIntoSequences(tokens))
                {
                    var state = new State(sequenceTokens, _procedures);
                    ExpressionNode root = state.ParseExpression(0);

                    if (state.Peek() is IToken unexpected)
                        throw new ParseException($"Unexpected {Describe(unexpected)} after a complete expression.");

                    sequences.Add(new Sequence(root));
                }

                return new ParsingResult(sequences, Errors.Success());
            }
            catch (ParseException ex)
            {
                return ParsingResult.Failed(Errors.SyntaxError(ex.Message));
            }
        }

        IParsingResult IParser.Parse(IEnumerable<IToken> tokens) => Parse(tokens);

        // Splitting into sequences

        private static List<List<IToken>> SplitIntoSequences(IEnumerable<IToken> tokens)
        {
            IToken[] all = tokens.ToArray();
            var sequences = new List<List<IToken>>();
            var current = new List<IToken>();
            var delimiters = new Stack<string>();

            void Close()
            {
                if (current.Count > 0)
                    sequences.Add(current);
                current = [];
            }

            for (int i = 0; i < all.Length; i++)
            {
                IToken token = all[i];

                if (IsOperator(token, "\""))
                {
                    if (i + 2 >= all.Length || all[i + 1].Type != TokenType.Text || !IsOperator(all[i + 2], "\""))
                        throw new ParseException("A text literal must be enclosed by quotation marks.");

                    current.AddRange(all[i..(i + 3)]);
                    i += 2;
                }
                else if (IsOperator(token, "(") || IsOperator(token, "[") || IsOperator(token, "{"))
                {
                    delimiters.Push(token.Value);
                    current.Add(token);
                }
                else if (IsOperator(token, ")") || IsOperator(token, "]") || IsOperator(token, "}"))
                {
                    if (delimiters.Count == 0 || delimiters.Pop() != Opening(token.Value))
                        throw new ParseException($"Unmatched \"{token.Value}\".");

                    current.Add(token);
                }
                else if (IsOperator(token, "\n") || IsOperator(token, ";"))
                {
                    if (delimiters.Count == 0)
                        Close();
                    else if (token.Value == ";")
                        throw new ParseException("Unexpected \";\" inside a delimiter.");
                    // A new line inside a delimiter is ignored
                }
                else if (token.Type == TokenType.Text)
                {
                    throw new ParseException("A text literal must be enclosed by quotation marks.");
                }
                else
                {
                    current.Add(token);
                }
            }

            if (delimiters.Count > 0)
                throw new ParseException($"Unclosed \"{delimiters.Peek()}\".");

            Close();
            return sequences;
        }

        private static string Opening(string closing) => closing switch { ")" => "(", "]" => "[", _ => "{" };

        internal static bool IsOperator(IToken? token, string symbol)
            => token != null && token.Type == TokenType.Operator && token.Value == symbol;

        internal static string Describe(IToken token) => token.Type switch
        {
            TokenType.Operator => token.Value == "\n" ? "new line" : $"\"{token.Value}\"",
            TokenType.Text => "text",
            _ => $"{token.Type.ToString().ToLowerInvariant()} \"{token.Value}\"",
        };

        // Parser state of one sequence

        private sealed class State(List<IToken> tokens, IReadOnlyDictionary<string, int> procedures)
        {
            private int _position;
            private readonly Stack<IReadOnlyList<string>> _lambdaScopes = new();

            public IToken? Peek(int offset = 0)
                => _position + offset < tokens.Count ? tokens[_position + offset] : null;

            private IToken Next()
                => _position < tokens.Count
                    ? tokens[_position++]
                    : throw new ParseException("Missing operand at the end of the expression.");

            private void Expect(string symbol)
            {
                IToken? token = Peek();
                if (!IsOperator(token, symbol))
                    throw new ParseException(token == null
                        ? $"Expected \"{symbol}\" at the end of the expression."
                        : $"Expected \"{symbol}\" instead of {Describe(token)}.");
                _position++;
            }

            /// <summary>
            /// Parses the longest expression whose operators all bind more tightly than <paramref name="rbp"/>.
            /// </summary>
            public ExpressionNode ParseExpression(int rbp)
            {
                ExpressionNode left = ParsePrefix();

                while (Peek() is IToken token && token.Type == TokenType.Operator && InfixPower(token.Value) > rbp)
                    left = ParseInfix(left, Next());

                if (left is ParameterList)
                    throw new ParseException("A lambda parameter list must be followed by \"=>\".");

                return left;
            }

            // Prefix step

            private ExpressionNode ParsePrefix()
            {
                IToken token = Next();

                switch (token.Type)
                {
                    case TokenType.Number:
                        if (!NumberValidator.TryParse(token.Value, out double number))
                            throw new ParseException($"Invalid number literal \"{token.Value}\".");
                        return new LiteralNode(Value.Number(number, token.Value), token.Value);

                    case TokenType.Identifier:
                        return ParseIdentifier(token.Value);

                    case TokenType.Operator:
                        return token.Value switch
                        {
                            "\"" => ParseText(),
                            "$" => ParseVariable(VariableScope.Context),
                            "@" => ParseVariable(VariableScope.Environment),
                            "(" => ParseParenthesis(),
                            "[" => new CallNode("ARRAY", ParseList("]")),
                            "{" => ParseObject(),
                            "!" => new CallNode("NOT", [ParseExpression(Operators.PrefixUnaryPower)]),
                            "-" => new CallNode("NEG", [ParseExpression(Operators.PrefixUnaryPower)]),
                            _ => throw new ParseException($"Unexpected {Describe(token)} where an operand is expected."),
                        };

                    default:
                        throw new ParseException($"Unexpected {Describe(token)}.");
                }
            }

            private ExpressionNode ParseText()
            {
                string content = Next().Value;
                Expect("\"");
                return TextLiteral(content);
            }

            private static LiteralNode TextLiteral(string content)
            {
                string literal = StringConverter.TextToLiteral(content);
                return new LiteralNode(Value.Text(content, literal), literal);
            }

            private ExpressionNode ParseIdentifier(string name)
            {
                switch (name)
                {
                    case "null": return new LiteralNode(Value.Null("null"), "null");
                    case "true": return new LiteralNode(Value.Boolean(true, "true"), "true");
                    case "false": return new LiteralNode(Value.Boolean(false, "false"), "false");
                }

                if (IsOperator(Peek(), "("))
                {
                    _position++;
                    return new CallNode(name, ParseList(")"));
                }

                if (procedures.TryGetValue(name, out int parameterCount))
                    return ParseProcedureCall(name, parameterCount);

                if (_lambdaScopes.Any(scope => scope.Contains(name)))
                    return new ParameterNode(name);

                return new ConstantNode(name);
            }

            /// <summary>
            /// Procedure call without parentheses, e.g. <c>SET $x 100</c>: the first n-1 arguments are single operands.
            /// </summary>
            private ExpressionNode ParseProcedureCall(string name, int parameterCount)
            {
                var arguments = new List<ExpressionNode>();

                for (int i = 0; i < parameterCount - 1; i++)
                    arguments.Add(ParseExpression(Operators.PostfixPower));

                if (parameterCount > 0)
                    arguments.Add(ParseExpression(0));

                return new CallNode(name, arguments);
            }

            private ExpressionNode ParseVariable(VariableScope scope)
            {
                IToken? token = Peek();
                if (token?.Type != TokenType.Identifier)
                    throw new ParseException("A scope sigil must be directly followed by an identifier.");

                _position++;
                return new VariableNode(scope, token.Value);
            }

            /// <summary>
            /// Comma-separated expressions up to <paramref name="closing"/>; used by calls and array literals.
            /// </summary>
            private List<ExpressionNode> ParseList(string closing)
            {
                var items = new List<ExpressionNode>();

                if (IsOperator(Peek(), closing))
                {
                    _position++;
                    return items;
                }

                while (true)
                {
                    items.Add(ParseExpression(0));

                    IToken token = Next();
                    if (IsOperator(token, closing))
                        return items;

                    if (!IsOperator(token, ","))
                        throw new ParseException($"Expected \",\" or \"{closing}\" instead of {Describe(token)}.");
                }
            }

            private ExpressionNode ParseParenthesis()
            {
                if (IsParameterList())
                    return ParseParameterList();

                ExpressionNode inner = ParseExpression(0);
                Expect(")");
                return inner;
            }

            /// <summary>
            /// The "(" just consumed starts a lambda parameter list if its matching ")" is directly followed by "=>".
            /// </summary>
            private bool IsParameterList()
            {
                int depth = 1;

                for (int i = _position; i < tokens.Count; i++)
                {
                    if (IsOperator(tokens[i], "(")) depth++;
                    else if (IsOperator(tokens[i], ")") && --depth == 0)
                        return i + 1 < tokens.Count && IsOperator(tokens[i + 1], "=>");
                }

                return false;
            }

            private ParameterList ParseParameterList()
            {
                var names = new List<string>();

                while (true)
                {
                    IToken token = Next();
                    if (token.Type != TokenType.Identifier)
                        throw new ParseException($"Expected a lambda parameter name instead of {Describe(token)}.");

                    if (names.Contains(token.Value))
                        throw new ParseException($"Lambda parameter \"{token.Value}\" is defined more than once.");

                    names.Add(token.Value);

                    IToken separator = Next();
                    if (IsOperator(separator, ")"))
                        return new ParameterList(names);

                    if (!IsOperator(separator, ","))
                        throw new ParseException($"Expected \",\" or \")\" in a lambda parameter list instead of {Describe(separator)}.");
                }
            }

            private ExpressionNode ParseObject()
            {
                var arguments = new List<ExpressionNode>();
                var keys = new HashSet<string>(StringComparer.Ordinal);

                if (IsOperator(Peek(), "}"))
                {
                    _position++;
                    return new CallNode("OBJECT", arguments);
                }

                while (true)
                {
                    IToken keyToken = Next();
                    string key;

                    if (keyToken.Type == TokenType.Identifier)
                        key = keyToken.Value;
                    else if (IsOperator(keyToken, "\""))
                    {
                        key = Next().Value;
                        Expect("\"");
                    }
                    else
                        throw new ParseException($"Expected an object key instead of {Describe(keyToken)}.");

                    if (!IdentifierValidator.Validate(key))
                        throw new ParseException($"Object key \"{key}\" is not a valid identifier.");

                    if (!keys.Add(key))
                        throw new ParseException($"Object key \"{key}\" is defined more than once.");

                    Expect(":");

                    arguments.Add(TextLiteral(key));
                    arguments.Add(ParseExpression(0));

                    IToken token = Next();
                    if (IsOperator(token, "}"))
                        return new CallNode("OBJECT", arguments);

                    if (!IsOperator(token, ","))
                        throw new ParseException($"Expected \",\" or \"}}\" instead of {Describe(token)}.");
                }
            }

            // Infix and postfix step

            private static int InfixPower(string symbol) => symbol switch
            {
                "=" or ":=" or "?=" => Operators.AssignmentPower,
                "=>" => Operators.LambdaPower,
                "||" => Operators.OrPower,
                "&&" => Operators.AndPower,
                ">" or "<" or ">=" or "<=" or "==" or "<>" or "!=" => Operators.ComparisonPower,
                "+" or "-" => Operators.AdditivePower,
                "*" or "/" or "%" => Operators.MultiplicativePower,
                "^" => Operators.ExponentialPower,
                "." or "[" => Operators.PostfixPower,
                _ => -1,
            };

            /// <summary>
            /// Binding power for the right operand: the priority, decreased by 1 for right-associative operators.
            /// </summary>
            private static int RightBindingPower(string symbol)
            {
                int power = InfixPower(symbol);
                return symbol is "=" or ":=" or "?=" or "=>" or "^" ? power - 1 : power;
            }

            private ExpressionNode ParseInfix(ExpressionNode left, IToken token)
            {
                string symbol = token.Value;

                if (Assignments.Contains(symbol))
                {
                    if (left is not VariableNode)
                        throw new ParseException($"The left operand of \"{symbol}\" must be a variable.");

                    return new CallNode(InfixTranslation[symbol], [left, ParseExpression(RightBindingPower(symbol))]);
                }

                if (symbol == "=>")
                {
                    if (left is not ParameterList parameters)
                        throw new ParseException("The left operand of \"=>\" must be a lambda parameter list.");

                    _lambdaScopes.Push(parameters.Names);
                    ExpressionNode body = ParseExpression(RightBindingPower(symbol));
                    _lambdaScopes.Pop();

                    return new LambdaNode(parameters.Names, new Sequence(body));
                }

                if (symbol == ".")
                {
                    IToken? key = Peek();
                    if (key?.Type != TokenType.Identifier)
                        throw new ParseException("\".\" must be followed by an identifier.");

                    _position++;
                    return new CallNode("GET", [left, TextLiteral(key.Value)]);
                }

                if (symbol == "[")
                {
                    ExpressionNode index = ParseExpression(0);
                    Expect("]");
                    return new CallNode("GET", [left, index]);
                }

                ExpressionNode right = ParseExpression(RightBindingPower(symbol));

                if (Comparisons.Contains(symbol) && Peek() is IToken next && next.Type == TokenType.Operator && Comparisons.Contains(next.Value))
                    throw new ParseException("Comparison operators cannot be chained.");

                return new CallNode(InfixTranslation[symbol], [left, right]);
            }
        }

        /// <summary>
        /// Parse-time node; valid only as the left operand of "=>".
        /// </summary>
        private sealed class ParameterList(IReadOnlyList<string> names) : ExpressionNode
        {
            public IReadOnlyList<string> Names { get; } = names;
            public override string Key => $"({string.Join(",", Names)})";
            public override ExpressionKind Kind => ExpressionKind.Lambda;
        }

        private sealed class ParseException(string message) : Exception(message);
    }
}
