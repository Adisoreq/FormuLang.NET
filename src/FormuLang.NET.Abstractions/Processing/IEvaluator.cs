using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Abstractions.Processing
{
    /// <summary>
    /// Evaluation stage: resolves the parsed sequences into values, in the order of their parsing queues.
    /// </summary>
    public interface IEvaluator
    {
        IEvaluationResult Evaluate(IParsingResult formula, IContext context);
    }
}
