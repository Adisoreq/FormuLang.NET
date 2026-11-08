using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Core.Processing;

namespace FormuLang.NET.Core.Tests
{
    /// <summary>
    /// docs/semantics/tokenization.md, docs/semantics/token.md, docs/syntax/literals.md
    /// </summary>
    public class TokenizerTests
    {
        private const TokenType Op = TokenType.Operator;
        private const TokenType Id = TokenType.Identifier;
        private const TokenType Num = TokenType.Number;
        private const TokenType Txt = TokenType.Text;

        private static (TokenType Type, string Value)[] Tokenize(string formula)
        {
            var result = Tokenizer.Tokenize(formula);
            Assert.True(result.Error.IsSuccess(), result.Error.ToString());
            return result.Tokens.Select(t => (t.Type, t.Value)).ToArray();
        }

        private static void AssertSyntaxError(string formula)
        {
            var result = Tokenizer.Tokenize(formula);
            Assert.Equal(Errors.SyntaxErrorCode, result.Error.Code);
        }

        [Fact]
        public void Tokenization_Example()
        {
            string formula =
                "$message = \"12.5[rad] in [deg] is:\"\n" +
                "$angle := 12.5 * 180 / PI\n" +
                "FORMAT(\"%s\\n%.1f\", [$message, $angle])";

            (TokenType, string)[] expected =
            [
                (Op, "$"), (Id, "message"), (Op, "="), (Op, "\""), (Txt, "12.5[rad] in [deg] is:"), (Op, "\""), (Op, "\n"),
                (Op, "$"), (Id, "angle"), (Op, ":="), (Num, "12.5"), (Op, "*"), (Num, "180"), (Op, "/"), (Id, "PI"), (Op, "\n"),
                (Id, "FORMAT"), (Op, "("), (Op, "\""), (Txt, "%s\n%.1f"), (Op, "\""), (Op, ","), (Op, "["),
                (Op, "$"), (Id, "message"), (Op, ","), (Op, "$"), (Id, "angle"), (Op, "]"), (Op, ")"),
            ];

            Assert.Equal(expected, Tokenize(formula));
        }

        [Fact]
        public void Tokenization_Example_AfterCleanup()
        {
            string formula = "$angle := 12.5 * 180 / PI";
            Assert.Equal(Tokenize(formula), Tokenize(Cleaner.CleanUp(formula).Result));
        }

        [Theory]
        [InlineData("$x=\"a\"")]
        [InlineData("$x = \"a\"")]
        public void TextAfterAssignment(string formula)
        {
            Assert.Equal([(Op, "$"), (Id, "x"), (Op, "="), (Op, "\""), (Txt, "a"), (Op, "\"")], Tokenize(formula));
        }

        [Fact]
        public void TextConcatenation()
        {
            Assert.Equal(
                [(Op, "\""), (Txt, "a"), (Op, "\""), (Op, "+"), (Op, "\""), (Txt, "b"), (Op, "\"")],
                Tokenize("\"a\"+\"b\""));
        }

        [Fact]
        public void SingleCharacterOperators_AreClosedImmediately()
        {
            Assert.Equal(
                [(Op, "$"), (Id, "x"), (Op, ":="), (Num, "2"), (Op, ";"), (Op, "$"), (Id, "y"), (Op, "="), (Num, "3")],
                Tokenize("$x:=2;$y=3"));
        }

        [Theory]
        [InlineData(":=")] [InlineData("?=")] [InlineData("=>")] [InlineData("&&")] [InlineData("||")]
        [InlineData(">=")] [InlineData("<=")] [InlineData("==")] [InlineData("<>")] [InlineData("!=")]
        public void TwoCharacterOperators(string op)
        {
            Assert.Equal([(Id, "a"), (Op, op), (Id, "b")], Tokenize($"a{op}b"));
        }

        [Fact]
        public void MinusIsNeverPartOfANumber()
        {
            Assert.Equal([(Op, "$"), (Id, "a"), (Op, "=="), (Op, "-"), (Num, "1")], Tokenize("$a==-1"));
            Assert.Equal([(Num, "3"), (Op, "-"), (Num, "2")], Tokenize("3-2"));
        }

        [Theory]
        [InlineData("a\\\"b", "a\"b")]
        [InlineData("C:\\\\dir", "C:\\dir")]
        [InlineData("line\\nnext", "line\nnext")]
        [InlineData("tab\\there", "tab\there")]
        [InlineData("# not a comment", "# not a comment")]
        public void EscapeSequences(string literal, string content)
        {
            Assert.Equal([(Op, "\""), (Txt, content), (Op, "\"")], Tokenize($"\"{literal}\""));
        }

        [Fact]
        public void EmptyText_HasAllThreeTokens()
        {
            Assert.Equal([(Op, "\""), (Txt, ""), (Op, "\"")], Tokenize("\"\""));
        }

        [Theory]
        [InlineData("\"abc")]
        [InlineData("\"abc\\\"")]
        [InlineData("\"a\nb\"")]
        [InlineData("\"bad \\x escape\"")]
        public void InvalidTextLiterals(string formula) => AssertSyntaxError(formula);

        [Theory]
        [InlineData("42")]
        [InlineData("042")]
        [InlineData("1_000_000")]
        [InlineData("12.5")]
        [InlineData(".45")]
        [InlineData("1.")]
        [InlineData("1.2E+3")]
        [InlineData("5.2e-3")]
        [InlineData("1e5")]
        [InlineData("0xFF")]
        [InlineData("0x40C")]
        public void NumberLiterals(string literal)
        {
            Assert.Equal([(Num, literal)], Tokenize(literal));
        }

        [Theory]
        [InlineData("1.2.3")]
        [InlineData("0x1.5")]
        [InlineData("1e5.5")]
        [InlineData("1_")]
        [InlineData("1__0")]
        [InlineData("1e")]
        [InlineData("0x")]
        [InlineData("12\"a\"")]
        public void InvalidNumberLiterals(string formula) => AssertSyntaxError(formula);

        [Fact]
        public void LetterAfterNumber_StartsIdentifier()
        {
            Assert.Equal([(Num, "2"), (Id, "x")], Tokenize("2x"));
        }

        [Fact]
        public void ExponentSign_RequiresDigit()
        {
            // "e" followed by "+" and a non-digit is not an exponent: "1e" is not a valid number
            AssertSyntaxError("1e+x");
        }

        [Fact]
        public void MemberAccess()
        {
            Assert.Equal([(Op, "$"), (Id, "obj"), (Op, "."), (Id, "name")], Tokenize("$obj.name"));
        }

        [Fact]
        public void ProcedureCallWithoutParentheses()
        {
            Assert.Equal([(Id, "SET"), (Op, "$"), (Id, "x"), (Num, "100")], Tokenize("SET $x 100"));
            Assert.Equal([(Id, "SET"), (Op, "$"), (Id, "x"), (Num, "100")], Tokenize(Cleaner.CleanUp("SET $x 100").Result));
        }

        [Fact]
        public void NewLineToken_OnlyBetweenLines()
        {
            Assert.Equal([(Num, "1"), (Op, "\n"), (Num, "2")], Tokenize("1\n2"));
        }

        [Theory]
        [InlineData("$żółw = 1")]
        [InlineData("$x = 1 € 2")]
        [InlineData("$x\u0001")]
        public void InvalidSymbols(string formula) => AssertSyntaxError(formula);

        [Theory]
        [InlineData("a & b")]
        [InlineData("a | b")]
        [InlineData("a ? b")]
        [InlineData("a ~ b")]
        [InlineData("a ' b")]
        public void UnknownOperators(string formula) => AssertSyntaxError(formula);

        [Fact]
        public void SyntaxError_ReportsPosition()
        {
            var result = Tokenizer.Tokenize("$x = 1\n$y = 1.2.3");
            Assert.Contains("line 2", result.Error.Message);
        }
    }
}
