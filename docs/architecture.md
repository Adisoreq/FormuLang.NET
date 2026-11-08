# Architecture

## Modules

```
FormuLang.NET.Abstractions  ◄──  FormuLang.NET.Core  ◄──  FormuLang.NET.Extensions.*
        (interfaces)              (Core standard)           (extension standards)
```

| Project | Depends on | Responsibility |
| :------ | :--------- | :------------- |
| `FormuLang.NET.Abstractions` | - | Contracts shared by every module. A host application or an extension can depend only on this project. |
| `FormuLang.NET.Core` | Abstractions | Implementation of the [Core standard](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/README.md) and of the execution pipeline. |
| `FormuLang.NET.Extensions.Stock` *(planned)* | Abstractions, Core | Implementation of the Stock extension standard (extended types such as `Stock.Candle`, indicator functions). |
| `FormuLang.NET.Core.Tests` | Core | Conformance tests built from the examples of the standard (xUnit). |
| `CalculatorCLI` (sample) | Core | Executes formulas from the command line. |

Every extension standard gets its own `FormuLang.NET.Extensions.<Name>` project, so a host application references only the standards it supports.

## Namespaces

| Namespace | Abstractions | Core |
| :-------- | :----------- | :--- |
| `.Primitives` | `IPrimitive`, `IValue`, `IType`, `IContainer`, `IObject`, `IError`, `PrimitiveType`, `ContainerType`, `FunctionType` | `Primitive`, `Value`, `Types` (type factory), `Container`, `Variable`, `Constant`, `Object`, `Error`, `VariableReference` |
| `.Functions` | `IFunction`, `ILambda`, `IFunctionOverload`, `IArgumentTemplate`, `IInvokable` | `Function`, `Procedure`, `FunctionOverload`, `ArgumentTemplate` |
| `.Operators` | `IOperator`, `OperatorType`, `Associativity` | `Operators` (operator table, in the root namespace) |
| `.Processing` | Stage interfaces (`IValidator`, `ICleaner`, `ITokenizer`, `IParser`, `IEvaluator`), their results, `IToken`, `IExpressionNode`, `ISequence`, `IExecutionResult` | `Executor`, `Validator`, `Cleaner`, `Tokenizer`, `Parser`, `Evaluator`, `Lambda`, expression nodes |
| `.Structures` | `IEnvironment`, `IContext`, `ContextType`, `IStandard`, `IStandardVersion`, `IAbstractTree<T>` | `Environment`, `Context`, `Standard`, `StandardVersion`, `AbstractTree<T>` |
| `.Standards` | - | `CoreStandard`, `CoreFunctions`, `CoreProcedures` |
| `.Validation` | - | `IdentifierValidator`, `NumberValidator` |
| (root) | - | `Errors` (Core error factory), `Operators` |

The standard calls the whole process **execution**; **evaluation** is only its fifth stage. The stages live in the `Processing` namespace, and `Executor` runs them in order.

## Execution pipeline

| # | Stage | Interface | Core implementation | Output |
| :-: | :---- | :-------- | :------------------ | :----- |
| | [Pre-execution](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/pre-execution.md) | - | `Context.Create`, `Context.PreExecute` | context with copies of the environment variables |
| 1 | [Validation](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/validation.md) | `IValidator` | `Validator` | every error found, at once |
| 2 | [Clean-up](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/cleanup.md) | `ICleaner` | `Cleaner` | cleaned text |
| 3 | [Tokenization](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/tokenization.md) | `ITokenizer` | `Tokenizer` | tokens |
| 4 | [Parsing](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/parsing.md) | `IParser` | `Parser` | one tree and parsing queue per sequence, primitive map |
| 5 | [Evaluation](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/evaluation.md) | `IEvaluator` | `Evaluator` | value of the last sequence, or the aborting Error |
| 6 | [Finalization](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/finalization.md) | - | `Executor` | `ExecutionResult` |

The pipeline stops at the first stage that fails. Validation is the only stage that reports every problem it finds.

### Parsing

`Parser` is a Pratt parser that follows [Core Parsing](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/semantics/parsing.md). Operators are translated while the tree is built, so the tree contains only these nodes:

| Node | Kind | Example key |
| :--- | :--- | :---------- |
| `LiteralNode` | Value | `4`, `"text"`, `true` |
| `VariableNode` | Variable | `$a`, `@array_indexing` |
| `ConstantNode` | Constant | `PI`, `ABS` (a function used as a Delegate) |
| `ParameterNode` | Parameter | `e` inside a lambda body |
| `CallNode` | FunctionCall | `ADD(POW($a,2),POW($b,2))` |
| `LambdaNode` | Lambda | `(e)=>MUL(e,2)`; its body is a separate `Sequence` |

The key of a node is its canonical form, used as the key of the primitive map.

### Evaluation

`Evaluator` traverses the parsing queue of every sequence bottom-up. The values are kept per node of the current queue, so a variable is read when its node is dequeued, as required by the standard. A call is resolved by name in the environment (`IEnvironment.GetFunction`); the first argument of `SET`, `INIT` and `COND_SET` is passed as a `VariableReference` instead of being read.

A lambda expression evaluates to a `Lambda` delegate that captures the scope it was created in, so its body can read context variables and the parameters of enclosing lambdas.

Functions receive the `IContext` of the execution, because the environment variables a formula changes (e.g. `@array_indexing = 1`) exist only in the context.

### Context types

The host application chooses the context a formula runs in, as in step 1 of [pre-execution](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/pre-execution.md); the formula does not select it. `Context` is the basic Core context. A context type with its own data (e.g. a stock indicator context with `CLOSE`, `N`, `I`) derives from it and overrides `LoadContextData()` (pre-execution step 2, run once per context). The host updates such constants between executions with `SetConstant`, which a formula cannot do. `Executor.Execute(formula, environment)` is only a shortcut for a new basic disposable context.

## Design rules

- **Errors are values.** Formula errors are returned as `IError` / `Value.Error(...)`, never thrown. Exceptions are reserved for programming errors of the host application (e.g. registering a function twice).
- **Abstractions first.** A new concept of the standard gets an interface in `Abstractions` before it gets an implementation in `Core`.
- **One standard, one module.** Code that belongs to an extension standard does not go into `Core`.
- **Standard identifiers come from the standard.** Error codes and names, operator spellings and binding powers, function signatures and standard identifiers (`core-0.2`) are copied from the standard documents; see [Development](development.md#keeping-the-library-in-sync-with-the-standard).
