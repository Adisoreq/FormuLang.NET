namespace FormuLang.NET.Abstractions.Operators
{
    [Flags] public enum OperatorType
    {
        /// <summary>
        /// Undefined operator type, should not be used.
        /// </summary>
        None = 0,

        /// <summary>
        /// Separates operands: A [operator] B
        /// </summary>
        Separator = 1,

        /// <summary>
        /// Unary operator that appears before its operand: [operator] A
        /// </summary>
        Prefix = 2,

        /// <summary>
        /// Unary operator that appears after its operand: A [operator]
        /// </summary>
        Suffix = 4,

        /// <summary>
        /// Open bracket for grouping expressions.
        /// </summary>
        OpenBracket = 8,

        /// <summary>
        /// Close bracket for grouping expressions.
        /// </summary>
        CloseBracket = 16,

        /// <summary>
        /// Sequence boundary (new line, semicolon). Not an expression operator.
        /// </summary>
        Boundary = 32,

        /// <summary>
        /// Structural separator inside a delimiter context (comma, colon). Never forms an expression.
        /// </summary>
        ItemSeparator = 64,
    }
}
