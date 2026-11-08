using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Abstractions.Processing
{
    /// <summary>
    /// Validation stage: a fast, shallow check of the raw formula and the environment that reports every problem at once.
    /// </summary>
    public interface IValidator
    {
        IValidationResult Validate(string formula, IEnvironment environment);
    }
}
