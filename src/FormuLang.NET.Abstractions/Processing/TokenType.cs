namespace FormuLang.NET.Abstractions.Processing
{
    public enum TokenType
    {
        /// <summary>
        /// Undefined token type.
        /// </summary>
        None,

        /// <summary>
        /// An operator token, such as +, -, *, /, etc.
        /// </summary>
        Operator,

        /// <summary>
        /// A number token, representing numeric literals like 42, 3.14, etc.
        /// </summary>
        Number,

        /// <summary>
        /// A text token, representing string literals like "Hello, World!".
        /// </summary>
        Text,

        /// <summary>
        /// An identifier token, representing variable names, function names, etc.
        /// </summary>
        Identifier
    }
}
