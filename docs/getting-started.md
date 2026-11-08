# Getting started

> [!IMPORTANT]
> FormuLang.NET is in the prototyping stage. The API can change in any `0.x` version.

## Requirements

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) or newer.

## Building

```bash
git clone https://github.com/Adisoreq/FormuLang.NET.git
```

```bash
dotnet build FormuLang.NET.sln
```

The library is not published on NuGet yet. To use it in your project, add project references:

```xml
<ItemGroup>
  <ProjectReference Include="..\FormuLang.NET\src\FormuLang.NET.Abstractions\FormuLang.NET.Abstractions.csproj" />
  <ProjectReference Include="..\FormuLang.NET\src\FormuLang.NET.Core\FormuLang.NET.Core.csproj" />
</ItemGroup>
```

## CalculatorCLI

[CalculatorCLI](../samples/CalculatorCLI) executes a formula in a basic Core environment and prints the result, or the error that stopped the execution.

```bash
dotnet run --project samples/CalculatorCLI -- "2 + 2 * 2"
```

```bash
dotnet run --project samples/CalculatorCLI -- -f samples/CalculatorCLI/examples/statistics.fl
```

Without arguments it starts an interactive mode: type a formula (it may have several lines) and press Enter on an empty line to execute it. A formula can also be piped through the standard input. The exit code is `0` on success and `1` when the execution was aborted by an error.

## Executing a formula

`Executor` runs the whole [execution](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/execution.md): validation, clean-up, tokenization, parsing, evaluation and finalization.

```csharp
using FormuLang.NET.Core.Processing;
using Environment = FormuLang.NET.Core.Structures.Environment;

var environment = Environment.Create();
var executor = Executor.Create();

ExecutionResult result = executor.Execute("$r := 2.5\nPI * $r ^ 2", environment);

if (result.Successful)
    Console.WriteLine(result.Value);            // 19.634954084936208
else
    Console.WriteLine(result.Error);            // e.g. [UNDEFINED_ERROR (12)] Variable "r" has no value.
```

| Property | Description |
| :------- | :---------- |
| `Value` | Result of the last sequence, or the **Error** that aborted the execution. |
| `Error` | The **Error** that aborted the execution, or `SUCCESS`. |
| `Successful` | `true` if the execution was not aborted. |
| `ValidationErrors` | Every problem found by validation; validation reports all of them at once. |
| `Context` | The context the formula was executed in. |

### Errors are values

A formula never throws. Every problem is an **Error** value with a code from the [Core error list](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/errors/README.md). The execution is aborted when an **Error** reaches the root of a sequence, unless the formula sets `@suppress_errors = true`.

## Environment and context

The **environment** is the global configuration of the host application. `Environment.Create()` supports the Core standard: all its functions and procedures, the constants `STANDARDS`, `E`, `PI`, `PHI`, `TRUE`, `FALSE`, `NULL`, and the variables `array_indexing` and `suppress_errors`.

A **context** holds the data of an execution: the context (`$`) variables, and copies of the environment (`@`) variables, so a formula never changes the environment itself.

```csharp
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Structures;

// Data provided by the host application
var context = Context.Create(environment);
context.InitVariable("price", Value.Number(120, "120"));
context.InitConstant("TAX", Value.Number(0.23, "0.23"));

executor.Execute("ROUND($price * (1 + TAX), 2)", context).Value;   // 147.6
```

`Execute(formula, environment)` creates a new **disposable** context for every execution. To keep variables between executions, create a **persistent** context and pass it every time:

```csharp
var counter = Context.Create(environment, ContextType.Persistent);

executor.Execute("$n := 0\n$n = $n + 1", counter);
executor.Execute("$n := 0\n$n = $n + 1\n$n", counter).Value;      // 2
```

### Custom context types

The host application chooses the type of the context, because it is the host that has the data a formula works on (docs of the standard: [pre-execution](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/pre-execution.md)). A context type that provides its own constants derives from `Context` and loads them in `LoadContextData()`, which runs once, before the first execution:

```csharp
using FormuLang.NET.Abstractions.Primitives;

class CandleContext(IEnvironment environment, double[] close) : Context(environment, ContextType.Persistent)
{
    protected override void LoadContextData()
    {
        SetConstant("CLOSE", Value.Array(close.Select(c => (IValue)Value.Number(c, "")), "CLOSE"));
        SetConstant("N", Value.Number(close.Length, "N"));
        SetConstant("I", Value.Number(0, "I"));
    }

    // Loop context: the host moves the iteration; a formula can never change a constant
    public void MoveTo(int i) => SetConstant("I", Value.Number(i, "I"));
}
```

```csharp
double[] close = [10, 12, 11, 15];
var candles = new CandleContext(environment, close);

for (int i = 0; i < close.Length; i++)
{
    candles.MoveTo(i);
    var average = executor.Execute("$sum := 0\n$sum = $sum + CLOSE[I]\n$sum / (I + 1)", candles).Value;
    Console.WriteLine(average);                                   // 10, 11, 11, 12
}
```

A formula that uses `CLOSE` in a basic Core context results in `UNKNOWN_SYMBOL_ERROR`. To check the supported standards at the start of a formula, use `REQUIRE`.

## Custom functions

A named function has one or more overloads, each with an argument template and an implementation. Register it in the environment to make it available to formulas:

```csharp
using FormuLang.NET.Core.Functions;

var hypot = new Function("HYPOT",
[
    new FunctionOverload(
        [ArgumentTemplate.Of("a", Types.Number), ArgumentTemplate.Of("b", Types.Number)],
        (args, context) => Value.Number(Math.Sqrt(Math.Pow(args[0].As<double>(), 2) + Math.Pow(args[1].As<double>(), 2)), "")),
]);

environment.RegisterFunction(hypot);
executor.Execute("HYPOT(3, 4)", environment).Value;               // 5
```

The arguments are matched against the overloads in order. If none matches, the call returns the leftmost **Error** argument, or an `ARGUMENT_ERROR` value, and the implementation is not called.

An extension standard implements `IStandard` and is registered with `environment.RegisterStandard(standard)`, which adds its identifier to `STANDARDS`, its extended types and its functions.

## Running single stages

Every stage is an interface in `FormuLang.NET.Abstractions.Processing` with a Core implementation in `FormuLang.NET.Core.Processing`. They can be used on their own, or replaced in the executor (`WithValidator`, `WithCleaner`, `WithTokenizer`, `WithParser`, `WithEvaluator`):

```csharp
ValidationResult validation = Validator.Validate("ADD(1, 2]", environment);   // all problems at once
CleanUpResult cleaned = Cleaner.CleanUp("$x = 2 # comment");
TokenizationResult tokens = Tokenizer.Tokenize(cleaned.Result);
ParsingResult parsed = Parser.ForEnvironment(environment).Parse(tokens.Tokens);

foreach (var sequence in parsed.Sequences)
    Console.WriteLine(sequence.Root.Key);                                      // SET($x,2)
```

A `ParsingResult` can be evaluated many times, e.g. in a loop over a persistent context, without repeating the earlier stages:

```csharp
var evaluator = new Evaluator();
EvaluationResult evaluation = evaluator.Evaluate(parsed, context);
```
