using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Abstractions.Processing
{
    /// <summary>
    /// Result of the whole execution, returned by finalization (docs/semantics/execution.md).
    /// </summary>
    public interface IExecutionResult : IOperationResult
    {
        /// <summary>
        /// Result of the formula. If the execution was aborted, the Error that aborted it.
        /// </summary>
        public IValue Value { get; }

        /// <summary>
        /// All errors found by validation; empty if validation passed.
        /// </summary>
        public IReadOnlyList<IError> ValidationErrors { get; }

        /// <summary>
        /// Context the formula was executed in; keep it to execute the next formula in a persistent context.
        /// </summary>
        public IContext Context { get; }
    }
}
