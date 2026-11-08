namespace FormuLang.NET.Abstractions.Primitives
{
    public enum PrimitiveType
    {
        /// <summary>
        /// Literal value, such as a number or string.
        /// </summary>
        Value,

        /// <summary>
        /// Container for a value, such as a variable or constant.
        /// </summary>
        Container,

        /// <summary>
        /// Structural relation transforming input set into determined output.
        /// </summary>
        Function,
    }
}