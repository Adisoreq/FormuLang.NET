namespace FormuLang.NET.Abstractions.Operators
{
    public enum Associativity
    {
        /// <summary>
        /// Operators of equal binding power group from the left: a - b - c = (a - b) - c.
        /// </summary>
        Left,

        /// <summary>
        /// Operators of equal binding power group from the right: a ^ b ^ c = a ^ (b ^ c).
        /// </summary>
        Right,

        /// <summary>
        /// Operators cannot be chained: a &lt; b &lt; c is a syntax error.
        /// </summary>
        None,
    }
}
