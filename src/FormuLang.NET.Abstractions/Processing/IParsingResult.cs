namespace FormuLang.NET.Abstractions.Processing
{
    public interface IParsingResult : IOperationResult
    {
        /// <summary>
        /// Sequences in formula order, each with its own tree and parsing queue.
        /// </summary>
        public IReadOnlyList<ISequence> Sequences { get; }

        /// <summary>
        /// Primitive map shared by the whole formula: every unique expression, by its canonical key.
        /// Lambda bodies have their own maps and are not included.
        /// </summary>
        public IReadOnlyDictionary<string, IExpressionNode> PrimitiveMap { get; }
    }
}
