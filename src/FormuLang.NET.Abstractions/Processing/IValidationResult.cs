using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Abstractions.Processing
{
    public interface IValidationResult : IOperationResult
    {
        /// <summary>
        /// Errors in the order of detection; empty if the formula is valid. <see cref="IOperationResult.Error"/> is the first of them.
        /// </summary>
        public IReadOnlyList<IError> Errors { get; }
    }
}
