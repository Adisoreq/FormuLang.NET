using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;

namespace FormuLang.NET.Core.Functions
{
    /// <summary>
    /// Named function or procedure with one or more overloads (docs/core/functions.md).
    /// </summary>
    public class Function : Primitive, IFunction
    {
        public string Name { get; }
        public FunctionType FunctionType { get; }
        public IEnumerable<IFunctionOverload> Overloads { get; }

        public Function(string name, FunctionType functionType, IEnumerable<IFunctionOverload> overloads)
            : base(PrimitiveType.Function, Types.Delegate, name)
        {
            Name = name;
            FunctionType = functionType;
            Overloads = overloads.ToArray();
        }

        public Function(string name, IEnumerable<IFunctionOverload> overloads)
            : this(name, FunctionType.Function, overloads) { }

        public override bool CanAccessValue => true;

        public override IError CanUpdateValue => Errors.InvalidIdentifierError($"Function \"{Name}\" cannot be redefined.");

        /// <summary>
        /// A function used as a value is a Delegate.
        /// </summary>
        public override IValue GetValue() => Value.Delegate(this, Name);

        public override IError SetValue(IValue value) => CanUpdateValue;

        /// <summary>
        /// Invokes the first overload that matches the arguments (docs/semantics/evaluation.md, "Function call").
        /// If no overload matches, the function is not executed and the call resolves to the leftmost Error argument,
        /// to the first Error element of an array that did not match an element type, or to <c>ARGUMENT_ERROR</c>.
        /// A procedure never accepts an Error argument.
        /// </summary>
        public IValue Invoke(IEnumerable<IValue> parameters, IContext context)
        {
            IValue[] args = parameters.ToArray();

            if (FunctionType == FunctionType.Procedure && FirstError(args) is IValue procedureError)
                return procedureError;

            foreach (var overload in Overloads)
            {
                if (overload.Matches(args, context))
                    return overload.Invoke(args, context);
            }

            if (FirstError(args) is IValue error)
                return error;

            if (FirstErrorElement(args) is IValue element)
                return element;

            string types = string.Join(", ", args.Select(a => a.Type.ToString()));
            return Value.Error(Errors.ArgumentError($"No overload of \"{Name}\" matches the arguments ({types})."), Name);
        }

        private static IValue? FirstError(IEnumerable<IValue> args)
            => args.FirstOrDefault(a => a.Type.Base == Types.ErrorName);

        /// <summary>
        /// First Error element of an array argument at a position where some overload requires an element type.
        /// </summary>
        private IValue? FirstErrorElement(IValue[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Raw is not IValue[] elements)
                    continue;

                bool elementTyped = Overloads.Any(o => o.Template.ElementAtOrDefault(i)?.ElementType != null);
                if (elementTyped && FirstError(elements) is IValue error)
                    return error;
            }

            return null;
        }

        public override string ToString() => $"{Name}(args) => any";
    }

    /// <summary>
    /// Function that changes the context or the environment (docs/standards/core/procedures/README.md).
    /// </summary>
    public class Procedure : Function
    {
        /// <summary>
        /// The first argument is the variable itself, not its value (SET, INIT, COND_SET).
        /// </summary>
        public bool TakesVariable { get; }

        /// <summary>
        /// Number of parameters; used to parse a call written without parentheses (<c>SET $x 100</c>).
        /// </summary>
        public int ParameterCount { get; }

        public Procedure(string name, int parameterCount, bool takesVariable, IEnumerable<IFunctionOverload> overloads)
            : base(name, FunctionType.Procedure, overloads)
        {
            ParameterCount = parameterCount;
            TakesVariable = takesVariable;
        }
    }
}
