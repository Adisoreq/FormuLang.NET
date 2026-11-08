using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Core.Processing
{
    public record struct CleanUpResult : ICleanUpResult
    {
        public string Result { get; set; } = string.Empty;
        public IError Error { get; set; }

        public CleanUpResult(string result, IError error)
        {
            Result = result;
            Error = error;
        }
    }
}
