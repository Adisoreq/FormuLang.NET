using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Processing;
using Env = FormuLang.NET.Core.Structures.Environment;

namespace FormuLang.NET.Core.Tests
{
    /// <summary>
    /// docs/standards/core/semantics/validation.md
    /// </summary>
    public class ValidatorTests
    {
        private static string[] Validate(string formula, Env? environment = null)
            => Validator.Validate(formula, environment ?? Env.Create()).Errors.Select(e => e.ToString()!).ToArray();

        [Theory]
        [InlineData("$x = 4 # ok ; ( \"")]
        [InlineData("$x = \"Item #1\"")]
        [InlineData("$x = (1 +\n 2)")]
        [InlineData("$x = \"a\\\"b\\\\\"")]
        public void Validation_Example_NoErrors(string formula)
        {
            Assert.Empty(Validate(formula));
        }

        [Theory]
        [InlineData("MUL(2, 3", "[SYNTAX_ERROR (2)] Unclosed \"(\" at line 1, column 4.")]
        [InlineData("ADD(1, 2))", "[SYNTAX_ERROR (2)] Unmatched \")\" at line 1, column 10.")]
        [InlineData("ADD(1, 2]", "[SYNTAX_ERROR (2)] \"]\" does not match the opening delimiter at line 1, column 9.")]
        [InlineData("$x = \"a\\qb\"", "[SYNTAX_ERROR (2)] Invalid escape sequence \"\\q\" at line 1, column 9.")]
        [InlineData("SUM([1; 2])", "[SYNTAX_ERROR (2)] Unexpected \";\" inside a delimiter at line 1, column 7.")]
        [InlineData("$x = 1 € 2", "[SYNTAX_ERROR (2)] Invalid symbol \"€\" at line 1, column 8.")]
        public void Validation_Example_OneError(string formula, string expected)
        {
            Assert.Equal([expected], Validate(formula));
        }

        [Fact]
        public void Validation_Example_AllErrorsAreReported()
        {
            Assert.Equal(
            [
                "[SYNTAX_ERROR (2)] Text literal is not closed at line 2, column 6.",
                "[SYNTAX_ERROR (2)] Unclosed \"(\" at line 1, column 6.",
            ],
            Validate("$x = (1\n$y = \"a"));
        }

        [Fact]
        public void Validation_Example_WrongEnvironmentVariable()
        {
            var env = Env.Create();
            env.RemoveProperty("suppress_errors");
            env.InitVariable("suppress_errors", Value.Text("yes", "\"yes\""));

            Assert.Equal(
                ["[ENVIRONMENT_ERROR (10)] Environment variable \"suppress_errors\" is missing or is not of type Boolean."],
                Validate("$x = 1", env));
        }

        [Fact]
        public void LineBreaks_AreCountedOnce()
        {
            Assert.Equal(["[SYNTAX_ERROR (2)] Unclosed \"(\" at line 3, column 1."], Validate("1\r\n2\r(", null));
        }

        [Fact]
        public void ValidationErrors_AbortTheExecution()
        {
            var result = FormulaRunner.Execute("$x = (1\n$y = \"a");

            Assert.Equal(2, result.ValidationErrors.Count);
            Assert.Equal(Errors.SyntaxErrorCode, result.Error.Code);
            Assert.False(result.Successful);
        }
    }
}
