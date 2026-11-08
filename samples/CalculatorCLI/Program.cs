using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Processing;
using FormuLang.NET.Core.Structures;
using Environment = FormuLang.NET.Core.Structures.Environment;

namespace CalculatorCLI
{
    /// <summary>
    /// Executes FormuLang formulas and prints the result, or the error that stopped the execution.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// <code>
    /// CalculatorCLI "2 + 2 * 2"          execute the formula given as an argument
    /// CalculatorCLI -f formula.fl        execute a formula file
    /// CalculatorCLI &lt; formula.fl          execute a formula read from the standard input
    /// CalculatorCLI                      interactive mode
    /// </code>
    /// </remarks>
    internal static class Program
    {
        // A basic Core environment: no constants or variables besides the ones required by the standard
        private static readonly Environment Environment = Environment.Create();
        private static readonly Executor Executor = Executor.Create();

        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length == 2 && args[0] is "-f" or "--file")
            {
                if (!File.Exists(args[1]))
                {
                    WriteError($"File not found: {args[1]}");
                    return 2;
                }

                return Run(File.ReadAllText(args[1]));
            }

            if (args.Length > 0 && args[0] is "-h" or "--help")
            {
                PrintHelp();
                return 0;
            }

            if (args.Length > 0)
                return Run(string.Join(' ', args));

            if (Console.IsInputRedirected)
                return Run(Console.In.ReadToEnd());

            Interactive();
            return 0;
        }

        /// <summary>
        /// Executes the formula in a new disposable context and prints its result.
        /// </summary>
        /// <returns>0 if the execution succeeded, 1 if it was aborted by an error.</returns>
        private static int Run(string formula)
        {
            ExecutionResult result = Executor.Execute(formula, Environment);

            if (result.Successful)
            {
                Console.WriteLine(result.Value);
                return 0;
            }

            // Validation reports every problem at once; any other stage stops at the first error
            IEnumerable<IError> errors = result.ValidationErrors.Count > 0 ? result.ValidationErrors : [result.Error];

            foreach (IError error in errors)
                WriteError(error.ToString()!);

            return 1;
        }

        private static void Interactive()
        {
            Console.WriteLine("FormuLang.NET calculator. Enter a formula; an empty line executes it. Type \"exit\" to quit.");

            var formula = new List<string>();

            while (true)
            {
                Console.Write(formula.Count == 0 ? "> " : ". ");
                string? line = Console.ReadLine();

                if (line == null || (formula.Count == 0 && line.Trim() == "exit"))
                    return;

                if (line.Length > 0)
                {
                    formula.Add(line);
                    continue;
                }

                if (formula.Count > 0)
                {
                    Run(string.Join('\n', formula));
                    formula.Clear();
                }
            }
        }

        private static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ResetColor();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("""
                Executes FormuLang formulas and prints the result.

                Usage:
                  CalculatorCLI "<formula>"      execute the formula given as an argument
                  CalculatorCLI -f <file>        execute a formula file
                  CalculatorCLI < <file>         execute a formula read from the standard input
                  CalculatorCLI                  interactive mode (an empty line executes the formula)

                Exit codes: 0 - success, 1 - the execution was aborted by an error, 2 - invalid usage.
                """);
        }
    }
}
