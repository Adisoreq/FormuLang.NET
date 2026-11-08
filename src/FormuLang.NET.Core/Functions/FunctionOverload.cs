using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Utils;

namespace FormuLang.NET.Core.Functions
{
    /// <param name="parameters">Arguments that match the overload's template.</param>
    /// <param name="context">Context of the current execution.</param>
    public delegate IValue FunctionImplementation(IReadOnlyList<IValue> parameters, IContext context);

    public class FunctionOverload : IFunctionOverload
    {
        private readonly FunctionImplementation _implementation;

        public IEnumerable<IArgumentTemplate> Template { get; }

        /// <param name="template">Parameter positions, in order.</param>
        /// <param name="implementation">Computes the result. Called only with arguments that match <paramref name="template"/>.</param>
        /// <exception cref="ArgumentException">The template breaks the argument collection rules.</exception>
        public FunctionOverload(IEnumerable<IArgumentTemplate> template, FunctionImplementation implementation)
        {
            Template = template.ToArray();
            _implementation = implementation;

            var validation = ArgumentUtils.ValidateCollection(Template);
            if (validation.IsFailed())
                throw new ArgumentException(validation.Message, nameof(template));
        }

        public IValue Invoke(IEnumerable<IValue> parameters, IContext context)
        {
            return _implementation(parameters as IReadOnlyList<IValue> ?? parameters.ToArray(), context);
        }

        public bool Matches(IEnumerable<IValue> parameters, IContext context)
        {
            return ArgumentUtils.MatchCollection(parameters, Template, context.Environment);
        }

        public override string ToString() => $"({ArgumentUtils.PrintCollection(Template)})";
    }
}
