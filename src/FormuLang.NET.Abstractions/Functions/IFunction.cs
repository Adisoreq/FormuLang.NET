using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Abstractions.Functions
{
    public interface IFunction : IPrimitive, INamedStructure, IInvokable
    {
        public FunctionType FunctionType { get; }

        /// <summary>
        /// Overloads of a named function. Empty for a lambda function.
        /// </summary>
        public IEnumerable<IFunctionOverload> Overloads { get; }
    }
}
