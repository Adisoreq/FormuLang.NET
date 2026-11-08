using FormuLang.NET.Core.Processing;

namespace FormuLang.NET.Core.Tests
{
    /// <summary>
    /// docs/semantics/cleanup.md
    /// </summary>
    public class CleanerTests
    {
        private static string Clean(string formula)
        {
            var result = Cleaner.CleanUp(formula);
            Assert.True(result.Error.IsSuccess(), result.Error.ToString());
            return result.Result;
        }

        [Fact]
        public void Cleanup_Example_RemovingComments()
        {
            Assert.Equal("$x=4\n$x^2", Clean("# First comment\n\n$x = 4 # Second comment\n\n$x ^ 2 # Third comment"));
        }

        [Fact]
        public void Cleanup_Example_WhiteSpaceAndLineCompression()
        {
            Assert.Equal("$x=2\n$y=3\nMUL($x,$y)", Clean("$x = 2\n$y = 3\n\nMUL($x, $y)"));
        }

        [Fact]
        public void Normalizes_TabsAndCarriageReturns()
        {
            Assert.Equal("$a=1\n$b=2", Clean("$a\t=\t1\r\n$b = 2\r"));
        }

        [Theory]
        [InlineData("$x = \"Item #1\"", "$x=\"Item #1\"")]
        [InlineData("$x = \"a\\\"#b\" # comment", "$x=\"a\\\"#b\"")]
        [InlineData("$x = \"a\\\\\" # comment", "$x=\"a\\\\\"")]
        public void HashInsideTextLiteral_IsNotAComment(string input, string expected)
        {
            Assert.Equal(expected, Clean(input));
        }

        [Fact]
        public void SemicolonAfterComment_IsRemoved()
        {
            Assert.Equal("$x=1;", Clean("$x = 1; # note; ignored"));
        }

        [Theory]
        [InlineData("SET $x 100", "SET$x 100")]
        [InlineData("SET   $x    100", "SET$x 100")]
        [InlineData("\"hello  world\"  +  \"x\"", "\"hello  world\"+\"x\"")]
        [InlineData("REQUIRE \"core-0.2+\"", "REQUIRE \"core-0.2+\"")]
        public void OnlySpacesNextToOperatorsAreRemoved(string input, string expected)
        {
            Assert.Equal(expected, Clean(input));
        }

        [Fact]
        public void TextLiteral_KeepsItsSpaces_AtTheEndOfTheLine()
        {
            // An unclosed literal runs to the end of the line; the tokenizer reports it, the clean-up must not alter it.
            Assert.Equal("$x=\"abc  ", Clean("$x = \"abc  "));
        }

        [Fact]
        public void EmptyInput_Fails()
        {
            Assert.True(Cleaner.CleanUp("   \n ").Error.IsFailed());
        }
    }
}
