using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Core.Primitives
{
    /// <summary>
    /// Constant whose value is computed at the moment it is read, e.g. the <c>I</c> constant of the Iteration Context,
    /// which follows the current value of <c>@array_indexing</c> (docs/core/context.md, "Iteration Context").
    /// </summary>
    public class ComputedConstant : Constant
    {
        private readonly Func<IValue> _compute;

        /// <param name="type">Type of every value returned by <paramref name="compute"/>.</param>
        public ComputedConstant(string name, IType type, Func<IValue> compute)
            : base(name, new Value(null, type, name))
        {
            _compute = compute;
        }

        public override bool CanAccessValue => true;

        public override IValue GetValue() => _compute();

        public override string ToString() => GetValue().ToString() ?? string.Empty;
    }
}
