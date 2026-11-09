using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Structures;
using FormuLang.NET.Core.Utils;

namespace FormuLang.NET.Core.Processing
{
    /// <summary>
    /// Runs the whole execution (docs/semantics/execution.md):
    /// validation → clean-up → tokenization → parsing → evaluation → finalization.
    /// Every stage can be replaced with its own implementation.
    /// </summary>
    public class Executor
    {
        private IValidator? _validator = new Validator();
        private ICleaner _cleaner = new Cleaner();
        private ITokenizer _tokenizer = new Tokenizer();
        private IParser? _parser;
        private IEvaluator _evaluator = new Evaluator();

        /// <summary>
        /// Executor with the Core implementation of every stage.
        /// </summary>
        public static Executor Create() => new();

        /// <param name="validator">Validator, or <c>null</c> to skip validation.</param>
        public Executor WithValidator(IValidator? validator)
        {
            _validator = validator;
            return this;
        }

        public Executor WithCleaner(ICleaner cleaner)
        {
            _cleaner = cleaner;
            return this;
        }

        public Executor WithTokenizer(ITokenizer tokenizer)
        {
            _tokenizer = tokenizer;
            return this;
        }

        /// <param name="parser">Parser, or <c>null</c> to use the Core parser with the procedures of the context's environment.</param>
        public Executor WithParser(IParser? parser)
        {
            _parser = parser;
            return this;
        }

        public Executor WithEvaluator(IEvaluator evaluator)
        {
            _evaluator = evaluator;
            return this;
        }

        /// <summary>
        /// Shortcut: executes the formula in a new basic, disposable Core context (<see cref="Context.Create"/>).
        /// The host application chooses the context type; for any other type (a persistent context, or a context that
        /// provides its own constants) create the context and use <see cref="Execute(string, IContext)"/>.
        /// </summary>
        public ExecutionResult Execute(string formula, IEnvironment environment)
            => Execute(formula, Context.Create(environment));

        /// <summary>
        /// Executes the formula in the context chosen by the host application. Reuse the context to keep its variables
        /// between executions.
        /// </summary>
        public ExecutionResult Execute(string formula, IContext context)
        {
            // Pre-execution
            if (context is Context core)
                core.PreExecute();

            if (Prepare(formula, context, out IParsingResult? parsing) is ExecutionResult failure)
                return failure;

            return Evaluate(parsing!, context);
        }

        /// <summary>
        /// Executes the formula once for every iteration of an Iteration Context (docs/core/context.md, "Iteration
        /// Context"), from the first to the last. The formula is validated, tokenized and parsed only once.
        /// </summary>
        /// <param name="stopOnError">
        /// The host application decides whether the remaining iterations are executed after an iteration results in an
        /// Error: <c>true</c> stops at the first Error, <c>false</c> executes every iteration (<c>PREV</c> is then
        /// <c>null</c> after a failed iteration).
        /// </param>
        public IterationExecutionResult ExecuteIterations(string formula, IterationContext context, bool stopOnError = true)
        {
            var results = new List<ExecutionResult>(context.Count);

            if (context.Count == 0)
                return new IterationExecutionResult(results);

            context.BeginIteration(0, null);

            if (Prepare(formula, context, out IParsingResult? parsing) is ExecutionResult failure)
            {
                results.Add(failure);
                return new IterationExecutionResult(results);
            }

            IValue? previous = null;

            for (int i = 0; i < context.Count; i++)
            {
                if (i > 0)
                    context.BeginIteration(i, previous);

                ExecutionResult result = Evaluate(parsing!, context);
                results.Add(result);

                if (!result.Successful && stopOnError)
                    break;

                previous = result.Value;
            }

            return new IterationExecutionResult(results);
        }

        /// <summary>
        /// Stages 1-4: validation, clean-up, tokenization and parsing.
        /// </summary>
        /// <returns>The failed result, or <c>null</c> if <paramref name="parsing"/> is ready to be evaluated.</returns>
        private ExecutionResult? Prepare(string formula, IContext context, out IParsingResult? parsing)
        {
            parsing = null;

            // 1. Validation
            if (_validator != null)
            {
                IValidationResult validation = _validator.Validate(formula, context.Environment);
                if (validation.Errors.Count > 0)
                    return Finalize(Value.Error(validation.Errors[0], formula), context, validation.Errors);
            }

            // 2. Clean-up
            ICleanUpResult cleanUp = _cleaner.CleanUp(formula);
            if (cleanUp.Error.IsFailed())
                return Fail(cleanUp.Error, formula, context);

            // 3. Tokenization
            ITokenizationResult tokenization = _tokenizer.Tokenize(cleanUp.Result);
            if (tokenization.Error.IsFailed())
                return Fail(tokenization.Error, formula, context);

            // 4. Parsing
            IParser parser = _parser ?? Parser.ForEnvironment(context.Environment);
            parsing = parser.Parse(tokenization.Tokens);
            if (parsing.Error.IsFailed())
                return Fail(parsing.Error, formula, context);

            return null;
        }

        /// <summary>
        /// Stages 5-6: evaluation and finalization.
        /// </summary>
        private ExecutionResult Evaluate(IParsingResult parsing, IContext context)
        {
            IEvaluationResult evaluation = _evaluator.Evaluate(parsing, context);
            return Finalize(evaluation.Value, context, []);
        }

        private static ExecutionResult Fail(IError error, string formula, IContext context)
            => Finalize(Value.Error(error, formula), context, []);

        /// <summary>
        /// Finalization: wraps the computed value into the result returned to the host application.
        /// </summary>
        private static ExecutionResult Finalize(IValue value, IContext context, IReadOnlyList<IError> validationErrors)
            => new(value, validationErrors, context);
    }

    public sealed class ExecutionResult : IExecutionResult
    {
        public IValue Value { get; }
        public IReadOnlyList<IError> ValidationErrors { get; }
        public IContext Context { get; }

        /// <summary>
        /// The Error that aborted the execution, or <c>SUCCESS</c>.
        /// </summary>
        public IError Error { get; }

        public bool Successful => Error.IsSuccess();

        public ExecutionResult(IValue value, IReadOnlyList<IError> validationErrors, IContext context)
        {
            Value = value;
            ValidationErrors = validationErrors;
            Context = context;
            Error = value.Type.Base == Types.ErrorName && value.Raw is IError error && error.IsFailed()
                ? error
                : Errors.Success();
        }

        /// <summary>
        /// The serialized value, with the serialization parameters of the execution (e.g. <c>@decimal_place = 2</c>).
        /// </summary>
        public override string ToString() => StringConverter.ValueToString(Value, SerializationOptions.FromContext(Context));
    }

    /// <summary>
    /// Results of the iterations of an Iteration Context, in order (<see cref="Executor.ExecuteIterations"/>).
    /// </summary>
    public sealed class IterationExecutionResult
    {
        /// <summary>
        /// One result for every executed iteration. If the formula cannot be parsed, there is only the failed result.
        /// </summary>
        public IReadOnlyList<ExecutionResult> Results { get; }

        /// <summary>
        /// Values of the iterations, in order.
        /// </summary>
        public IEnumerable<IValue> Values => Results.Select(r => r.Value);

        /// <summary>
        /// The first Error that aborted an iteration, or <c>SUCCESS</c>.
        /// </summary>
        public IError Error => Results.FirstOrDefault(r => !r.Successful)?.Error ?? Errors.Success();

        public bool Successful => Error.IsSuccess();

        public IterationExecutionResult(IReadOnlyList<ExecutionResult> results)
        {
            Results = results;
        }
    }
}
