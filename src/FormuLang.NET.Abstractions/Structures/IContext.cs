using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Abstractions.Structures
{
    /// <summary>
    /// Data of a formula execution. The context itself stores the context ($) variables.
    /// </summary>
    public interface IContext : IObject
    {
        /// <summary>
        /// Environment related to the context.
        /// </summary>
        public IEnvironment Environment { get; }

        /// <summary>
        /// Copies of the environment (@) variables, made during pre-execution. A formula changes only these copies.
        /// </summary>
        public IObject EnvironmentVariables { get; }

        public ContextType ContextType { get; }
    }
}
