using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Standards;

namespace FormuLang.NET.Core.Structures
{
    /// <summary>
    /// Core <b>Iteration Context</b> (docs/core/context.md, "Iteration Context"): the host application executes the same
    /// formula once for every element of a collection. Constants: <c>I</c> (index of the current iteration, follows
    /// <c>@array_indexing</c>), <c>N</c> (number of iterations) and <c>PREV</c> (result of the previous iteration).
    /// </summary>
    /// <remarks>
    /// Run the iterations with <see cref="Processing.Executor.ExecuteIterations"/>, or call <see cref="BeginIteration"/>
    /// before every execution. Every iteration starts with no context variables and with the environment variables of
    /// the environment; the only state passed from one iteration to the next is <c>PREV</c>.
    /// </remarks>
    public class IterationContext : Context
    {
        public const string IterationConstant = "I";
        public const string CountConstant = "N";
        public const string PreviousConstant = "PREV";

        /// <summary>
        /// Number of iterations (<c>N</c>).
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 0-based index of the current iteration. <c>I</c> is this value plus <c>@array_indexing</c>.
        /// </summary>
        public int Iteration { get; private set; }

        /// <summary>
        /// Result of the previous iteration (<c>PREV</c>); <c>null</c> in the first iteration.
        /// </summary>
        public IValue Previous { get; private set; } = Value.Null(PreviousConstant);

        protected IterationContext(IEnvironment environment, int count)
            : base(environment, ContextType.Persistent)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            Count = count;
        }

        /// <summary>
        /// Creates an Iteration Context for <paramref name="count"/> iterations, positioned at the first one.
        /// </summary>
        public static IterationContext Create(IEnvironment environment, int count)
        {
            var context = new IterationContext(environment, count);
            context.PreExecute();
            return context;
        }

        protected override void LoadContextData()
        {
            SetConstant(CountConstant, Value.Number(Count, CountConstant));
            SetContainer(new ComputedConstant(IterationConstant, Types.Number,
                () => Value.Number(Iteration + FunctionHelpers.ArrayIndexing(this), IterationConstant)));
            SetConstant(PreviousConstant, Previous);
        }

        /// <summary>
        /// Prepares the iteration with the 0-based <paramref name="iteration"/> index: removes the context variables and
        /// the overridden environment variables, sets <c>PREV</c>, and runs the pre-execution.
        /// </summary>
        /// <param name="previous">Result of the previous iteration; <c>null</c> (or an Error) is stored as <c>null</c>.</param>
        public void BeginIteration(int iteration, IValue? previous)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(iteration);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(iteration, Math.Max(Count, 1));

            PreExecute(); // loads the context constants before the first iteration
            ClearVariables();

            Iteration = iteration;
            Previous = previous == null || previous.Type.Base == Types.ErrorName ? Value.Null(PreviousConstant) : previous;
            SetConstant(PreviousConstant, Previous);

            PreExecute();
        }
    }
}
