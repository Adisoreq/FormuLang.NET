# Development

The [FormuLang standard](https://github.com/Adisoreq/FormuLang) and this library are developed in parallel. This document describes how to keep them in sync.

## Keeping the library in sync with the standard

### 1. Pin the standard revision

The library always targets one revision of the standard. The revision is recorded in [Conformance](conformance.md) (`Target standard` and `Standard revision checked`). When the standard changes:

1. Read the standard's [CHANGELOG](https://github.com/Adisoreq/FormuLang/blob/main/CHANGELOG.md) and [Core CHANGELOG](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/CHANGELOG.md) since the pinned revision.
2. Add every relevant change to [Conformance](conformance.md) as a deviation (it is a deviation until the library implements it).
3. Update the pinned revision.

This way the conformance document is always an honest list of the differences, even when the library lags behind the standard.

### 2. Copy identifiers, do not invent them

Values defined by the standard are copied from it, never chosen in the library:

- error codes and names ([Core errors](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/errors/README.md)),
- operator spellings, binding powers and associativity ([Operator parsing](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/operator-parsing.md#binding-power)),
- standard identifiers (`core-0.2`), environment constants and variables ([Core](https://github.com/Adisoreq/FormuLang/blob/main/docs/standards/core/README.md)).

If the implementation needs something the standard does not define (for example a new error), propose it in the standard first, then implement it.

### 3. Turn the standard's examples into tests

Most standard documents contain examples with an input and an expected result (the tokenization table in [Tokenization](https://github.com/Adisoreq/FormuLang/blob/main/docs/semantics/tokenization.md#example), the [Core examples](https://github.com/Adisoreq/FormuLang/tree/main/docs/standards/core/examples), the **Algorithm** and **Errors** sections of each function). They are the conformance test suite:

- one test per example, named after the document it comes from, e.g. `Tokenization_Example_FormatCall`;
- a test that fails because the standard changed is a deviation: add it to [Conformance](conformance.md) instead of changing the expected result.

A discrepancy found while implementing is often a gap in the standard itself (an undefined edge case, two documents that disagree). Fix it in the standard, so every implementation gets the same answer.

### 4. Change workflow

For a change of the language:

1. Change the standard (syntax / semantics document and changelog).
2. Add the example of the change as a test in the library. It fails.
3. Implement the change. The test passes.
4. Update [Conformance](conformance.md) and [CHANGELOG](../CHANGELOG.md).

For a change of the library only (refactoring, performance, API), the standard is not involved.

## Versioning

| | Scheme | Example |
| :-- | :----- | :------ |
| Standard (Core) | `<name>-<major>.<minor>` | `core-0.2` |
| Library | [SemVer](https://semver.org) | `0.1.0` |

The library version is independent of the standard version: a library release can fix bugs without any change of the standard. Every release lists the standards it supports in the [CHANGELOG](../CHANGELOG.md), and `IEnvironment.Standards` reports the same identifiers at runtime.

While the library is `0.x`, any release can contain breaking API changes.

## Extensions

An extension standard (e.g. **Stock**) is implemented in its own project, `FormuLang.NET.Extensions.<Name>`, with its own section in [Conformance](conformance.md). Add the project only when the extension has a first version of its specification in the standard repository; until then keep it in the README as *planned*.

## Building and running

```bash
dotnet build FormuLang.NET.sln
```

```bash
dotnet run --project samples/CalculatorCLI
```
