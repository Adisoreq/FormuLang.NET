namespace FormuLang.NET.Abstractions.Functions
{
    /// <summary>
    /// Anonymous lambda function. It has no overloads: the number of arguments must equal the number of parameters.
    /// </summary>
    public interface ILambda : IFunction
    {
        public IReadOnlyList<string> Parameters { get; }
    }
}
