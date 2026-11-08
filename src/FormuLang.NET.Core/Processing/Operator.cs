using FormuLang.NET.Abstractions.Operators;

namespace FormuLang.NET.Core.Processing
{
    public readonly record struct Operator : IOperator
    {
        public string Symbol { get; }
        public OperatorType Type { get; }
        public int Priority { get; }
        public Associativity Associativity { get; }

        public Operator(string symbol, OperatorType type, int priority, Associativity associativity = Associativity.Left)
        {
            Symbol = symbol;
            Type = type;
            Priority = priority;
            Associativity = associativity;
        }
    }
}
