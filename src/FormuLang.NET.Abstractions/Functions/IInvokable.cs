using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Abstractions.Functions
{
    public interface IInvokable
    {
        /// <param name="context">Context of the current execution. Functions read environment variables from it
        /// (e.g. <c>array_indexing</c>), because a formula can override them for one execution.</param>
        public IValue Invoke(IEnumerable<IValue> parameters, IContext context);
    }
}
