namespace FormuLang.NET.Abstractions.Primitives
{
    public enum FunctionType
    {
        /// <summary>
        /// A standard function that takes arguments and returns a value.
        /// </summary>
        Function,

        /// <summary>
        /// Error-type function operating on environment.
        /// </summary>
        Procedure,

        /// <summary>
        /// Temporary anonymous function used for lambda expressions.
        /// </summary>
        Lambda,
    }
}