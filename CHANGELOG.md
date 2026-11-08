<div align="center">

<img src="./raw/images/icons/monogram.svg" height="50" alt="FormuLang monogram">

# Changelog

</div>

All notable changes of **FormuLang.NET**. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); the library uses [Semantic Versioning](https://semver.org).

## [Unreleased]

Supported standards: `core-0.2` (see [Conformance](docs/conformance.md)).

### Added

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
