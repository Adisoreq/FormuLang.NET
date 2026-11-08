using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;

namespace FormuLang.NET.Core.Structures
{
    /// <summary>
    /// Global layer between FormuLang and the host application (docs/core/environment.md).
    /// The environment is never changed by a formula: every execution works on a copy of its variables in a <see cref="Context"/>.
    /// </summary>
    public class Environment : Primitives.Object, IEnvironment
    {
        /// <summary>
        /// Identifier of the implemented Core standard version.
        /// </summary>
        public const string CoreStandard = FormuLang.NET.Core.Standards.CoreStandard.Identifier;

        private readonly List<string> _standards = [];
        private readonly List<string> _extendedTypes = [];
        private readonly AbstractTree<string> _typeTree = new(Types.ObjectName, Types.ObjectName);
        private readonly Dictionary<string, IFunction> _functions = new(StringComparer.Ordinal);

        public IEnumerable<string> Standards => _standards;

        public IEnumerable<string> ExtendedTypes => _extendedTypes;

        /// <summary>
        /// Inheritance tree of extended types. The root is <c>Object</c>; nodes are keyed by the extended type name.
        /// </summary>
        public IAbstractTree<string> ExtendedTypeTree => _typeTree;

        public IEnumerable<IFunction> Functions => _functions.Values;

        public bool Supports(string standard) => _standards.Contains(standard);

        public IFunction? GetFunction(string name) => _functions.GetValueOrDefault(name);

        protected Environment() { }

        /// <summary>
        /// Creates an environment that supports the Core standard: its functions, procedures, constants and variables
        /// (docs/standards/core/README.md, "Environment structure").
        /// </summary>
        public static Environment Create()
        {
            var env = new Environment();

            // Constants
            env.InitConstant("STANDARDS", Value.Array([], "STANDARDS"));
            env.InitConstant("E", Value.Number(Math.E, "E"));
            env.InitConstant("PI", Value.Number(Math.PI, "PI"));
            env.InitConstant("PHI", Value.Number((1 + Math.Sqrt(5)) / 2, "PHI"));

            // Keyword constants (docs/syntax/literals.md: "pre-initialized environment constants")
            env.InitConstant("TRUE", Value.Boolean(true, "TRUE"));
            env.InitConstant("FALSE", Value.Boolean(false, "FALSE"));
            env.InitConstant("NULL", Value.Null("NULL"));

            // Variables
            env.InitVariable("array_indexing", Value.Number(0, "0"));
            env.InitVariable("suppress_errors", Value.Boolean(false, "false"));

            env.RegisterStandard(FormuLang.NET.Core.Standards.CoreStandard.Instance);

            return env;
        }

        /// <summary>
        /// Adds a standard: its identifier, extended types and named functions.
        /// </summary>
        /// <exception cref="InvalidOperationException">A function of the standard is already defined.</exception>
        public void RegisterStandard(IStandard standard)
        {
            foreach (string type in standard.ExtendedTypes)
            {
                var node = standard.ExtendedTypeTree.GetNode(type);
                string? parent = node?.Parent?.Key;
                RegisterExtendedType(type, parent == null || parent == Types.ObjectName ? null : parent);
            }

            foreach (IFunction function in standard.NamedFunctions)
                RegisterFunction(function);

            RegisterStandard($"{standard.ID}-{standard.Version.Version}");
        }

        /// <summary>
        /// Adds a standard identifier, e.g. <c>stock-0.1</c>, to <c>STANDARDS</c>.
        /// </summary>
        public void RegisterStandard(string standard)
        {
            if (_standards.Contains(standard))
                return;

            _standards.Add(standard);

            SetConstant("STANDARDS", Value.Array(_standards.Select(s => Value.Text(s, $"\"{s}\"")), "STANDARDS"));
        }

        /// <summary>
        /// Adds a named function or procedure.
        /// </summary>
        /// <exception cref="InvalidOperationException">A function with the same name is already defined.</exception>
        public void RegisterFunction(IFunction function)
        {
            if (!_functions.TryAdd(function.Name, function))
                throw new InvalidOperationException($"Function \"{function.Name}\" is already defined.");
        }

        /// <summary>
        /// Registers an extended type, e.g. <c>RegisterExtendedType("Stock.Candle")</c>.
        /// </summary>
        /// <param name="parentType">Extended type it derives from; <c>null</c> to derive directly from Object.</param>
        public void RegisterExtendedType(string extendedType, string? parentType = null)
        {
            _typeTree.AddNode(extendedType, extendedType, parentType ?? Types.ObjectName);
            _extendedTypes.Add(extendedType);
        }
    }
}
