using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Abstractions.Structures
{
    /// <summary>
    /// Environment structure is a layer of abstraction between programmer and user, providing data and context to process the formula.
    /// </summary>
    public interface IEnvironment : IObject
    {
        /// <summary>
        /// List of supported and implemented standards by environment
        /// </summary>
        public IEnumerable<string> Standards { get; }

        /// <summary>
        /// Determines whether the environment supports and implements provided standard
        /// </summary>
        /// <param name="standard">Standard signature</param>
        /// <returns>True if standard is supported, false if not</returns>
        public bool Supports(string standard);

        /// <summary>
        /// List of extended types of primitives
        /// </summary>
        public IEnumerable<string> ExtendedTypes { get; }

        /// <summary>
        /// Abstract tree defining inheritance hierarchy of extended types
        /// </summary>
        public IAbstractTree<string> ExtendedTypeTree { get; }

        /// <summary>
        /// Named functions and procedures of the supported standards
        /// </summary>
        public IEnumerable<IFunction> Functions { get; }

        /// <summary>
        /// Finds a named function or procedure
        /// </summary>
        /// <returns>The function, or null if no supported standard defines it</returns>
        public IFunction? GetFunction(string name);
    }
}
