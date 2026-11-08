using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Abstractions.Processing
{
    public interface IEvaluationResult : IOperationResult
    {
        /// <summary>
        /// Value of the last evaluated sequence, or the Error that aborted the execution.
        /// </summary>
        public IValue Value { get; }
    }
}
