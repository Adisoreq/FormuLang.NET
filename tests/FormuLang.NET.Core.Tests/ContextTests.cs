using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Processing;
using FormuLang.NET.Core.Structures;
using Env = FormuLang.NET.Core.Structures.Environment;

namespace FormuLang.NET.Core.Tests
{
    /// <summary>
    /// Context types chosen by the host application (docs/semantics/pre-execution.md, docs/core/context.md).
    /// </summary>
    public class ContextTests
    {
        /// <summary>
        /// Example of a context type with its own constants, like a future stock indicator context:
        /// <c>CLOSE</c> (closing prices), <c>N</c> (their count) and <c>I</c> (the current iteration).
        /// </summary>
        private sealed class CandleContext : Context
        {
            private readonly double[] _close;

            public int LoadCount { get; private set; }

            public CandleContext(IEnvironment environment, double[] close)
                : base(environment, ContextType.Persistent)
            {
                _close = close;
            }

            protected override void LoadContextData()
            {
                LoadCount++;
                SetConstant("CLOSE", Value.Array(_close.Select(c => (IValue)Value.Number(c, c.ToString())), "CLOSE"));
                SetConstant("N", Value.Number(_close.Length, "N"));
                SetConstant("I", Value.Number(0, "I"));
            }

            public void MoveTo(int iteration) => SetConstant("I", Value.Number(iteration, "I"));
        }

        private static readonly double[] Close = [10, 12, 11, 15];

        [Fact]
        public void ContextConstants_AreLoadedBeforeTheFirstExecution()
        {
            var context = new CandleContext(Env.Create(), Close);

            var result = Executor.Create().Execute("CLOSE[N - 1] - CLOSE[0]", context);

            Assert.Equal("5", result.Value.ToString());
        }

        [Fact]
        public void LoopContext_KeepsVariablesWhileTheHostMovesTheIteration()
        {
            var context = new CandleContext(Env.Create(), Close);
            var executor = Executor.Create();
            var results = new List<string>();

            for (int i = 0; i < Close.Length; i++)
            {
                context.MoveTo(i);
                results.Add(executor.Execute("$sum := 0\n$sum = $sum + CLOSE[I]\n$sum / (I + 1)", context).Value.ToString()!);
            }

            Assert.Equal(["10", "11", "11", "12"], results);
            Assert.Equal(1, context.LoadCount);
        }

        [Fact]
        public void FormulaCannotChangeAContextConstant()
        {
            var context = new CandleContext(Env.Create(), Close);

            // A constant is not a variable: "$N" is a separate context variable, and SET cannot target "N"
            var result = Executor.Create().Execute("SET(N, 1)", context);

            Assert.Equal(Errors.ArgumentErrorCode, result.Error.Code);
            Assert.Equal("4", context.GetProperty("N").GetValue().ToString());
        }

        [Fact]
        public void ConstantsOfAnotherContextType_AreUnknown()
        {
            var result = Executor.Create().Execute("CLOSE[0]", Env.Create());

            Assert.Equal(Errors.UnknownSymbolErrorCode, result.Error.Code);
        }

        [Fact]
        public void SetConstant_ReplacesTheValue()
        {
            var obj = new Primitives.Object();
            obj.SetConstant("X", Value.Number(1, "1"));
            obj.SetConstant("X", Value.Text("a", "\"a\""));

            Assert.Equal(ContainerType.Constant, obj.GetProperty("X").ContainerType);
            Assert.Equal("a", obj.GetProperty("X").GetValue().ToString());
        }
    }
}
