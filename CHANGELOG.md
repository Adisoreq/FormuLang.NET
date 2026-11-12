<div align="center">

<img src="./raw/images/icons/monogram.svg" height="50" alt="FormuLang.NET monogram">

# Changelog

</div>

All notable changes of **FormuLang.NET**. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); the library uses [Semantic Versioning](https://semver.org).

## [Unreleased]

Supported standards: `core-0.3`, `stock-0.1` (see [Conformance](docs/conformance.md)).

### Added

- Core `core-0.3` functions: `SIGN`, `TRUNC`, `SUBSTR`, `FIND`, `CONTAINS`, `STARTS`, `ENDS`, `REPLACE`, `REVERSE`, `LEN(text)`, `SEQ`, `REDUCE`, `ANY`, `ALL`, `SORT`, `HAS`, `KEYS` and the trigonometric functions `SIN`, `COS`, `TAN`, `ASIN`, `ACOS`, `ATAN`, `ATAN2`, `DEG`, `RAD`. `MAP` accepts a lambda with an index, `(e, i) => ...`.
- Serialization variables `decimal_place`, `split_array`, `split_object`, `include_object_class`, `max_array_preview` in the environment; `SerializationOptions` and `StringConverter.ValueToString(value, options)`. `TEXT`, `JOIN` and `ExecutionResult.ToString()` use the variables of the execution.
- Validation of the constants `NULL`, `TRUE`, `FALSE` and of the serialization variables.
- `IterationContext` (Core Iteration Context with `I`, `N`, `PREV`) and `Executor.ExecuteIterations`, which parses the formula once and evaluates it for every iteration. `ComputedConstant` for constants computed when they are read.
- `Object.Keys` keeps the order in which the keys were defined; `Object.SetContainer`.
- `AbstractTree` and `AbstractTreeNode` are public, so extension standards can define their type trees.
- `FormuLang.NET.Extensions.Stock` (`stock-0.1`): `Candle` / `Stock.Candle`, `CANDLE`, `INDICATOR` with an `IndicatorRegistry`, `StockContext`, `IndicatorContext`, `PatternContext`, `StrategyContext`, `StockExecutor` and `Environment.RegisterStock()`. Test project `FormuLang.NET.Extensions.Stock.Tests`.

### Changed

- The Core identifier is `core-0.3` (was `core-0.2`).


- Complete execution pipeline: `Validator` (all problems at once), `Parser` (Pratt parser with operator translation, parsing queues and primitive map), `Evaluator` (variable reading rules, sequence results, `suppress_errors`, lambdas), and `Executor`, which runs every stage and returns an `ExecutionResult`.
- `Context` with pre-execution (copies of environment variables), disposable and persistent contexts.
- All Core functions (`CoreFunctions`), the `OBJECT` function for object literals, and the procedures `SET`, `INIT`, `COND_SET`, `REQUIRE` (`CoreProcedures`); `CoreStandard`.
- Element types in argument templates (`ArgumentTemplate.ArrayOf`, `Array<Number>`).
- `Environment.RegisterFunction` and `RegisterStandard(IStandard)`; `Object.RemoveProperty`.
- Custom context types: `Context.LoadContextData()` (pre-execution step 2, run once per context) and `Object.SetConstant` for constants updated by the host, e.g. the iteration of a loop context.
- `CalculatorCLI` executes formulas (argument, file, standard input or interactive mode) and prints the result or the error.
- `FormuLang.NET.Abstractions`: interfaces of primitives, functions, operators, pipeline stages and structures.
- `FormuLang.NET.Core`: primitives (`Value`, `Variable`, `Constant`, `Object`, `Error`), Core errors, operator table, clean-up and tokenization.
- `Types` factory for base and extended types, `Value.Delegate`, `Value.Object` with an extended type.
- Public `ArgumentTemplate` and `FunctionOverload` constructors, so functions can be defined outside the library.
- `Environment.RegisterStandard` and `Environment.RegisterExtendedType`.
- `NumberValidator.TryParse` for all Number literal notations.
- `Associativity` of operators; `OperatorType.Boundary` and `OperatorType.ItemSeparator`.
- Conformance test project `FormuLang.NET.Core.Tests`.
- Documentation: README, getting started, architecture, conformance, development guide.
