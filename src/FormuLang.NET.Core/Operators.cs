using FormuLang.NET.Abstractions.Operators;
using FormuLang.NET.Core.Processing;
using System.Collections.Frozen;

namespace FormuLang.NET.Core
{
    /// <summary>
    /// Operator tokens of the Core standard.
    /// Spellings follow docs/syntax/operators.md, binding powers and associativity follow
    /// docs/semantics/operator-parsing.md (higher priority binds more tightly).
    /// </summary>
    public static class Operators
    {
        // Binding powers (operator-parsing.md, "Binding power")

        public const int NoBindingPower = 0;
        public const int AssignmentPower = 1;
        public const int LambdaPower = 2;
        public const int OrPower = 3;
        public const int AndPower = 4;
        public const int ComparisonPower = 5;
        public const int AdditivePower = 6;
        public const int MultiplicativePower = 7;
        public const int ExponentialPower = 8;
        public const int PrefixUnaryPower = 9;
        public const int PostfixPower = 10;
        public const int SigilPower = 11;

        // Sequence boundaries (not expression operators)

        public static readonly Operator NewLine
            = new("\n", OperatorType.Boundary, NoBindingPower);

        public static readonly Operator Terminator
            = new(";", OperatorType.Boundary, NoBindingPower);

        // Scope sigils

        public static readonly Operator EnvironmentSigil
            = new("@", OperatorType.Prefix, SigilPower, Associativity.Right);

        public static readonly Operator ContextSigil
            = new("$", OperatorType.Prefix, SigilPower, Associativity.Right);

        // Assignment

        public static readonly Operator SetVariable
            = new("=", OperatorType.Separator, AssignmentPower, Associativity.Right);

        public static readonly Operator InitVariable
            = new(":=", OperatorType.Separator, AssignmentPower, Associativity.Right);

        public static readonly Operator ConditionalSetVariable
            = new("?=", OperatorType.Separator, AssignmentPower, Associativity.Right);

        // Lambda definition

        public static readonly Operator Lambda
            = new("=>", OperatorType.Separator, LambdaPower, Associativity.Right);

        // Delimiters. ( and [ in an operator position are a function call and an index expression.

        public static readonly Operator OpenCircleBracket
            = new("(", OperatorType.OpenBracket | OperatorType.Suffix, PostfixPower);

        public static readonly Operator CloseCircleBracket
            = new(")", OperatorType.CloseBracket, PostfixPower);

        public static readonly Operator OpenSquareBracket
            = new("[", OperatorType.OpenBracket | OperatorType.Suffix, PostfixPower);

        public static readonly Operator CloseSquareBracket
            = new("]", OperatorType.CloseBracket, PostfixPower);

        public static readonly Operator OpenCurlyBracket
            = new("{", OperatorType.OpenBracket, NoBindingPower);

        public static readonly Operator CloseCurlyBracket
            = new("}", OperatorType.CloseBracket, NoBindingPower);

        public static readonly Operator TextDelimiter
            = new("\"", OperatorType.OpenBracket | OperatorType.CloseBracket, NoBindingPower);

        // Structural separators

        public static readonly Operator Comma
            = new(",", OperatorType.ItemSeparator, NoBindingPower);

        public static readonly Operator Colon
            = new(":", OperatorType.ItemSeparator, NoBindingPower);

        // Logical

        public static readonly Operator Or
            = new("||", OperatorType.Separator, OrPower);

        public static readonly Operator And
            = new("&&", OperatorType.Separator, AndPower);

        // Comparison (non-associative)

        public static readonly Operator Equal
            = new("==", OperatorType.Separator, ComparisonPower, Associativity.None);

        public static readonly Operator NotEqualA
            = new("!=", OperatorType.Separator, ComparisonPower, Associativity.None);

        public static readonly Operator NotEqualB
            = new("<>", OperatorType.Separator, ComparisonPower, Associativity.None);

        public static readonly Operator Lesser
            = new("<", OperatorType.Separator, ComparisonPower, Associativity.None);

        public static readonly Operator LesserOrEqual
            = new("<=", OperatorType.Separator, ComparisonPower, Associativity.None);

        public static readonly Operator Greater
            = new(">", OperatorType.Separator, ComparisonPower, Associativity.None);

        public static readonly Operator GreaterOrEqual
            = new(">=", OperatorType.Separator, ComparisonPower, Associativity.None);

        // Additive

        public static readonly Operator Add
            = new("+", OperatorType.Separator, AdditivePower);

        public static readonly Operator Subtract
            = new("-", OperatorType.Separator, AdditivePower);

        // Multiplicative

        public static readonly Operator Multiply
            = new("*", OperatorType.Separator, MultiplicativePower);

        public static readonly Operator Divide
            = new("/", OperatorType.Separator, MultiplicativePower);

        public static readonly Operator Modulo
            = new("%", OperatorType.Separator, MultiplicativePower);

        // Exponential

        public static readonly Operator Power
            = new("^", OperatorType.Separator, ExponentialPower, Associativity.Right);

        // Prefix unary

        public static readonly Operator Negate
            = new("-", OperatorType.Prefix, PrefixUnaryPower, Associativity.Right);

        public static readonly Operator Not
            = new("!", OperatorType.Prefix, PrefixUnaryPower, Associativity.Right);

        // Member access

        public static readonly Operator Dot
            = new(".", OperatorType.Separator, PostfixPower);

        // Collections

        public static readonly IReadOnlyList<Operator> OperatorList = [
            NewLine, Terminator,
            EnvironmentSigil, ContextSigil,
            SetVariable, InitVariable, ConditionalSetVariable,
            Lambda,
            OpenCircleBracket, CloseCircleBracket, OpenSquareBracket, CloseSquareBracket, OpenCurlyBracket, CloseCurlyBracket,
            TextDelimiter,
            Comma, Colon, Dot,
            Or, And,
            Equal, NotEqualA, NotEqualB, Lesser, LesserOrEqual, Greater, GreaterOrEqual,
            Add, Subtract,
            Multiply, Divide, Modulo,
            Power,
            Negate, Not
        ];

        /// <summary>
        /// Operators grouped by spelling. A spelling can have several roles (e.g. "-" is subtraction and negation).
        /// </summary>
        public static readonly FrozenDictionary<string, IReadOnlyList<Operator>> OperatorMap =
            OperatorList
                .GroupBy(op => op.Symbol)
                .ToFrozenDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<Operator>)g.ToList()
                );

        /// <summary>
        /// Operator spellings recognized by the tokenizer (docs/syntax/operators.md, "Operator length").
        /// The new line is a boundary token emitted by the tokenizer itself, so it is not included.
        /// </summary>
        public static readonly FrozenSet<string> TokenSpellings =
            OperatorList.Select(op => op.Symbol).Where(s => s != "\n").ToFrozenSet();

        public static bool IsOperatorSpelling(string symbol) => TokenSpellings.Contains(symbol);
    }
}
