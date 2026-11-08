namespace FormuLang.NET.Abstractions.Operators
{
    public interface IOperator
    {
        public string Symbol { get; }
        public OperatorType Type { get; }

        /// <summary>
        /// Binding power. Higher binding power binds more tightly. 0 for tokens that do not compete for binding power.
        /// </summary>
        public int Priority { get; }

        public Associativity Associativity { get; }
    }
}
