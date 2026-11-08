using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Abstractions.Processing
{
    public interface IOperationResult
    {
        public IError Error { get; }
        public bool Successful => Error.Code == 0;
        public bool Failed => !Successful;
    }
}
