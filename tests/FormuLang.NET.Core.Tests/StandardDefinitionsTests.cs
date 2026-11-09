using FormuLang.NET.Abstractions.Operators;
using FormuLang.NET.Core.Processing;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Validation;
using Env = FormuLang.NET.Core.Structures.Environment;

namespace FormuLang.NET.Core.Tests
{
    /// <summary>
    /// Values copied from the standard: errors, operators, environment, literals, naming.
    /// </summary>
    public class StandardDefinitionsTests
    {
        // docs/standards/core/errors/README.md
        [Fact]
        public void ErrorCodes_MatchTheCoreErrorList()
        {
            var expected = new (uint, string, Error)[]
            {
                (0, "SUCCESS", Errors.Success()),
                (1, "CUSTOM_ERROR", Errors.CustomError()),
                (2, "SYNTAX_ERROR", Errors.SyntaxError()),
                (3, "OPERATION_ERROR", Errors.OperationError()),
                (4, "TYPE_ERROR", Errors.TypeError()),
                (5, "UNKNOWN_SYMBOL_ERROR", Errors.UnknownSymbolError()),
                (6, "ARGUMENT_ERROR", Errors.ArgumentError()),
                (7, "INVALID_INDEX_ERROR", Errors.InvalidIndexError()),
                (8, "INVALID_KEY_ERROR", Errors.InvalidKeyError()),
                (9, "INVALID_IDENTIFIER_ERROR", Errors.InvalidIdentifierError()),
                (10, "ENVIRONMENT_ERROR", Errors.EnvironmentError()),
                (11, "NOT_SUPPORTED_ERROR", Errors.NotSupportedError()),
                (12, "UNDEFINED_ERROR", Errors.UndefinedError()),
            };

            foreach (var (code, name, error) in expected)
            {
                Assert.Equal(code, error.Code);
                Assert.Equal(name, error.Name);
            }
        }

        // docs/syntax/operators.md, "Operator length"
        [Fact]
        public void OperatorSpellings_MatchTheOperatorLengthTable()
        {
            string[] expected =
            [
                "$", "@", ";", "=", "(", ")", "[", "]", "{", "}", "\"", ",", ":", ".", ">", "<", "+", "-", "*", "/", "%", "^", "!",
                ":=", "?=", "=>", "&&", "||", ">=", "<=", "==", "<>", "!=",
            ];

            Assert.Equal(expected.Order(), Operators.TokenSpellings.Order());
        }

        // docs/semantics/operator-parsing.md, "Binding power"
        [Fact]
        public void BindingPowers_FollowTheStandardOrder()
        {
            Operator[][] lowestToHighest =
            [
                [Operators.SetVariable, Operators.InitVariable, Operators.ConditionalSetVariable],
                [Operators.Lambda],
                [Operators.Or],
                [Operators.And],
                [Operators.Equal, Operators.NotEqualA, Operators.NotEqualB, Operators.Lesser, Operators.LesserOrEqual, Operators.Greater, Operators.GreaterOrEqual],
                [Operators.Add, Operators.Subtract],
                [Operators.Multiply, Operators.Divide, Operators.Modulo],
                [Operators.Power],
                [Operators.Negate, Operators.Not],
                [Operators.Dot, Operators.OpenSquareBracket, Operators.OpenCircleBracket],
                [Operators.ContextSigil, Operators.EnvironmentSigil],
            ];

            for (int level = 0; level < lowestToHighest.Length; level++)
                foreach (var op in lowestToHighest[level])
                    Assert.Equal(level + 1, op.Priority);
        }

        [Fact]
        public void Associativity_FollowsTheStandard()
        {
            Assert.Equal(Associativity.Right, Operators.Power.Associativity);
            Assert.Equal(Associativity.Right, Operators.SetVariable.Associativity);
            Assert.Equal(Associativity.Right, Operators.Lambda.Associativity);
            Assert.Equal(Associativity.None, Operators.Lesser.Associativity);
            Assert.Equal(Associativity.Left, Operators.Subtract.Associativity);
        }

        [Fact]
        public void SequenceBoundaries_AreNotExpressionOperators()
        {
            Assert.Equal(OperatorType.Boundary, Operators.NewLine.Type);
            Assert.Equal(OperatorType.Boundary, Operators.Terminator.Type);
            Assert.False(Operators.OperatorMap.ContainsKey("#"));
        }

        // docs/standards/core/README.md, "Environment structure"
        [Fact]
        public void CoreEnvironment_HasRequiredConstantsAndVariables()
        {
            var env = Env.Create();

            Assert.True(env.Supports("core-0.3"));
            Assert.Equal("[\"core-0.3\"]", env.GetProperty("STANDARDS").GetValue().ToString());
            Assert.Equal(Math.E, ((Value)env.GetProperty("E").GetValue()).AsNumber());
            Assert.Equal(Math.PI, ((Value)env.GetProperty("PI").GetValue()).AsNumber());
            Assert.Equal(1.618033988749894, ((Value)env.GetProperty("PHI").GetValue()).AsNumber()!.Value, tolerance: 1e-14);
            Assert.Equal(0d, ((Value)env.GetProperty("array_indexing").GetValue()).AsNumber());
            Assert.False(((Value)env.GetProperty("suppress_errors").GetValue()).AsBoolean());

            Assert.False(env.GetProperty("PI").Mutable);
            Assert.True(env.GetProperty("array_indexing").Mutable);
        }

        [Fact]
        public void EnvironmentConstant_CannotBeChanged()
        {
            var env = Env.Create();
            Assert.Equal(Errors.EnvironmentErrorCode, env.SetProperty("PI", Value.Number(3, "3")).Code);
            Assert.True(env.SetProperty("array_indexing", Value.Number(1, "1")).IsSuccess());
        }

        [Fact]
        public void RegisterStandard_UpdatesStandardsConstant()
        {
            var env = Env.Create();
            env.RegisterStandard("stock-0.1");

            Assert.True(env.Supports("stock-0.1"));
            Assert.Equal("[\"core-0.3\", \"stock-0.1\"]", env.GetProperty("STANDARDS").GetValue().ToString());
        }

        // docs/syntax/literals.md, "Number"
        [Theory]
        [InlineData("42", 42)]
        [InlineData("042", 42)]
        [InlineData("1_000_000", 1000000)]
        [InlineData("12.5", 12.5)]
        [InlineData(".45", 0.45)]
        [InlineData("1.", 1)]
        [InlineData("1.2E+3", 1200)]
        [InlineData("5.2e-3", 0.0052)]
        [InlineData("0xFF", 255)]
        [InlineData("0x1A", 26)]
        [InlineData("0x40C", 1036)]
        [InlineData("0x00ff", 255)]
        public void NumberLiterals_AreParsed(string literal, double expected)
        {
            Assert.True(NumberValidator.TryParse(literal, out double value));
            Assert.Equal(expected, value, 12);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData("1.2.3")]
        [InlineData("_1")]
        [InlineData("1_")]
        [InlineData("0xG")]
        [InlineData("-1")]
        public void InvalidNumberLiterals_AreRejected(string literal)
        {
            Assert.False(NumberValidator.IsValidNumber(literal));
            Assert.False(NumberValidator.TryParse(literal, out _));
        }

        // docs/syntax/naming.md
        [Theory]
        [InlineData("x", true)]
        [InlineData("_tmp1", true)]
        [InlineData("MAX_VALUE", true)]
        [InlineData("1st", false)]
        [InlineData("my key", false)]
        [InlineData("żółw", false)]
        [InlineData("", false)]
        public void Identifiers(string identifier, bool valid)
        {
            Assert.Equal(valid, IdentifierValidator.Validate(identifier));
        }
    }
}
