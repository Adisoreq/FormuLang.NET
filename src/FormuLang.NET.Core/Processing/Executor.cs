using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Structures;

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
            IParsingResult parsing = parser.Parse(tokenization.Tokens);
            if (parsing.Error.IsFailed())
                return Fail(parsing.Error, formula, context);

            // 5. Evaluation
            IEvaluationResult evaluation = _evaluator.Evaluate(parsing, context);

            // 6. Finalization
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

        public override string ToString() => Value.ToString() ?? string.Empty;
    }
}
