namespace FormuLang.NET.Abstractions.Processing
{
    /// <summary>
    /// One top-level expression of a formula (docs/core/formula.md, "Formula sequences").
    /// </summary>
    public interface ISequence
    {
        public IExpressionNode Root { get; }

        /// <summary>
        /// Parsing queue: the nodes ordered bottom-up (highest level first, left to right within a level). The root is last.
        /// </summary>
        public IReadOnlyList<IExpressionNode> Queue { get; }
    }
}
