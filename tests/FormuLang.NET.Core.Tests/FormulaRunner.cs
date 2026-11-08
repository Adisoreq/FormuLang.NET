using FormuLang.NET.Core.Processing;
using Env = FormuLang.NET.Core.Structures.Environment;

namespace FormuLang.NET.Core.Tests
{
    internal static class FormulaRunner
    {
        /// <summary>
        /// Executes the formula in a new Core environment and returns the serialized result.
        /// </summary>
        public static string Run(string formula) => Execute(formula).Value.ToString()!;

        public static ExecutionResult Execute(string formula) => Executor.Create().Execute(formula, Env.Create());
    }
}
