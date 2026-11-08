using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Core.Primitives
{
    public enum VariableScope
    {
        /// <summary>Context variable, <c>$name</c>.</summary>
        Context,

        /// <summary>Environment variable, <c>@name</c>.</summary>
        Environment,
    }

    /// <summary>
    /// The variable itself (not its value), passed as the first argument of SET, INIT and COND_SET.
    /// </summary>
    public sealed record VariableReference(VariableScope Scope, string Name)
    {
        public string Sigil => Scope == VariableScope.Context ? "$" : "@";

        public override string ToString() => Sigil + Name;

        public IValue ToValue() => new Value(this, Types.Variable, ToString());
    }
}
