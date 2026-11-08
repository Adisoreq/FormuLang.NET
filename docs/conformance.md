# Conformance

Status of the implementation against the FormuLang standard.

| | |
| :-- | :-- |
| **Target standard** | Core `core-0.2` (in progress) |
| **Standard revision checked** | [`Adisoreq/FormuLang@6243bf6`](https://github.com/Adisoreq/FormuLang/tree/6243bf6) (2026-11-07) |
| **Last review** | 2026-11-08 |
| **Conformance tests** | [tests/FormuLang.NET.Core.Tests](../tests/FormuLang.NET.Core.Tests) |

Update the revision and this document every time the library is synced with a newer revision of the standard (see [Development](development.md)).

Legend: ✅ conforms · 🟡 partial / deviates · ❌ not implemented

## Pipeline

| Stage | Status | Notes |
| :---- | :----: | :---- |
| [Pre-execution](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/pre-execution.md) | ✅ | `Context.Create` / `PreExecute` copy only the missing environment variables. |
| [Validation](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/semantics/validation.md) | ✅ | Environment checks and the formula scan, with the error messages of the standard. |
| [Clean-up](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/cleanup.md) | ✅ | |
| [Tokenization](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/tokenization.md) | ✅ | |
| [Parsing](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/semantics/parsing.md) | ✅ | See [Implementation choices](#implementation-choices) I8. |
| [Evaluation](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/evaluation.md) | ✅ | |
| [Finalization](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/finalization.md) | ✅ | `ExecutionResult`. |
| [Post-execution](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/post-execution.md) | ✅ | Handled by the host: a disposable context is dropped, a persistent one is passed to the next execution. |

## Core standard

| Area | Status | Notes |
| :--- | :----: | :---- |
| [Errors](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/errors/README.md) | ✅ | All 13 errors with the standard codes and names. |
| [Types](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/types/README.md) | ✅ | |
| [Serialization](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/semantics/serialization.md) | 🟡 | Default parameters only (`decimal_place`, `split_array`, `split_object`, `include_object_class` are not configurable). |
| [Environment](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/README.md#environment-structure) | ✅ | `core-0.2`, constants `STANDARDS`, `E`, `PI`, `PHI`, variables `array_indexing`, `suppress_errors`. |
| [Inheritance](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/inheritance.md) of extended types | ✅ | |
| [Argument templates](https://github.com/Adisoreq/FormuLang/blob/main/docs/implementation/argument_template.md) and overloads | ✅ | Including element types (`Array<Number>`) and the error propagation rules. |
| [Procedures](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/procedures/README.md) | ✅ | `SET`, `INIT`, `COND_SET`, `REQUIRE`, also without parentheses. |
| [Functions](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/functions/README.md) | ✅ | All 57 functions, tested with the examples of their documents. |
| Lambda functions | ✅ | |
| [Operators](https://github.com/Adisoreq/FormuLang/blob/main/docs/syntax/operators.md) | ✅ | |

## Known deviations

Remove an item when it is fixed and covered by a test.

| # | Deviation | Standard |
| :-: | :-------- | :------- |
| D1 | Serialization parameters cannot be changed. | [serialization.md](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/semantics/serialization.md) |
| D2 | The primitive map holds the classified expressions; the values computed during evaluation are kept per node of the current queue instead of being written into the map. The order of evaluation and the results are the same. | [evaluation.md § Result propagation](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/evaluation.md#4-result-propagation) |

## Implementation choices

Places where the standard leaves room for interpretation, and what the library does. Each is a candidate for clarification in the standard.

| # | Topic | Choice |
| :-: | :---- | :----- |
| I1 | A character that can begin a 2-character operator (`=`, `:`, `<`, `!`, ...) is also listed as a single-character operator, so step 2.2.6.1 of the tokenization algorithm would close it before the 2-character form is seen. | The tokenizer keeps the token open while it can still become a 2-character operator. |
| I2 | An operator token whose spelling is not in the operator table (`&`, `\|`, `?`, `~`, `'`, `` ` ``, `\` outside text). | `SYNTAX_ERROR` during tokenization, because a token must have a supported spelling ([token.md](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/token.md)). |
| I3 | A number token that is not a valid literal (`1_`, `1e`, `0x`). | `SYNTAX_ERROR` when the token is closed. |
| I4 | Changing a constant: the Core standard defines `ENVIRONMENT_ERROR` for environment constants ([SET](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/procedures/SET.md)), but no error for other constants. | `ENVIRONMENT_ERROR` for every constant. |
| I5 | `TRUE`, `FALSE` and `NULL` are described as environment constants in [literals.md](https://github.com/Adisoreq/FormuLang/blob/main/docs/syntax/literals.md), but are missing from the Core environment table. | They are defined in `Environment.Create()`. |
| I6 | Serialization of numbers with `decimal_place = -1` is limited by `double`. | Shortest round-trip decimal notation, invariant culture. |
| I7 | Clean-up of an empty or white-space-only formula. | `OPERATION_ERROR`. |
| I8 | `{ k: v }` is translated to `OBJECT("k",v)` ([Core parsing](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/semantics/parsing.md#operator-translation)), but `OBJECT` is not in the Core functions index. | `OBJECT(...entries? <Any>)` is a Core function taking key-value pairs. |
| I9 | An identifier without a sigil that names a function (`MAP($a, ABS)`, `TEXT(AVG)`). | It is a Delegate of that function. Constants are checked first. |
| I10 | The README of the standard calls a lambda stored in a variable as `$square(5)`, but Core parsing gives `(` an infix role only after an identifier. | Not supported (syntax error), following Core parsing. A lambda is invoked by the function it is passed to. |
| I11 | `NUMBER(text)`: the standard does not say whether a sign is allowed (`NUMBER("-5")`). | A leading `+` or `-` is accepted. |
| I12 | Lambda body scope: the standard does not say whether a body can read context variables. | It can (`MAP($arr, (e) => e - AVG($arr))` from the [common formulas](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/examples/common_formulas.md) needs it), and it can read the parameters of enclosing lambdas. |
| I13 | Choice of the context type, and the *loop context* ([context.md § 2.1](https://github.com/Adisoreq/FormuLang/blob/main/docs/core/context.md#21-loop-context)). | The host application chooses and creates the context ([pre-execution](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/pre-execution.md), step 1); a formula cannot select it. A context type loads its constants in `Context.LoadContextData()`. There is no separate `ContextType` value for a loop context: it is a persistent context whose iteration constants the host updates with `SetConstant`. To be specified together with the Stock extension. |

## Inconsistencies found in the standard

Found while writing the conformance tests. The tests follow the algorithms, not these examples.

| Document | Problem |
| :------- | :------ |
| [number_examples.md](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/examples/number_examples.md), example 4 | The formula uses `$c = 2.5e+2` (250), but the table and the result `-21` use `2.5e-1` (0.25). |
| [array_examples.md](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/examples/array_examples.md), [text_examples.md](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/examples/text_examples.md) | Results are written as `[ [ 1, 2, 3 ], [ a, b, c ] ]`; [serialization](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/semantics/serialization.md) gives `[[1, 2, 3], ["a", "b", "c"]]`. |
| [context_examples.md](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/examples/context_examples.md), example 6 | The result is written as `TYPE_ERROR: ...` instead of `[TYPE_ERROR (4)] ...`. |
| [environment_examples.md](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/examples/environment_examples.md), example 4 | `REQUIRE "math-1.x"` fails in a Core environment, so the shown result `12.5663706144` is never reached. |
| [README.md](https://github.com/Adisoreq/FormuLang/blob/main/README.md) of the standard | `$squared = $square(5)` (see I10). |
