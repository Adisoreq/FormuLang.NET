namespace FormuLang.NET.Abstractions.Processing
{
    /// <summary>
    /// Node of an expression (reference) tree.
    /// </summary>
    public interface IExpressionNode
    {
        /// <summary>
        /// Canonical form of the expression, e.g. <c>ADD(POW($a,2),POW($b,2))</c>. Key of the primitive map.
        /// </summary>
        public string Key { get; }

        public ExpressionKind Kind { get; }

        /// <summary>
        /// Depth of the node: 1 for the root of a sequence.
        /// </summary>
        public int Level { get; }

        public IReadOnlyList<IExpressionNode> Children { get; }
    }
}
