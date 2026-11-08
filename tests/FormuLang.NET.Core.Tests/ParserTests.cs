using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Core.Processing;

namespace FormuLang.NET.Core.Tests
{
    /// <summary>
    /// docs/semantics/parsing.md, docs/standards/core/semantics/parsing.md, docs/semantics/operator-parsing.md
    /// </summary>
    public class ParserTests
    {
        private static ParsingResult Parse(string formula)
        {
            var tokens = Tokenizer.Tokenize(Cleaner.CleanUp(formula).Result);
            Assert.True(tokens.Error.IsSuccess(), tokens.Error.ToString());
            return new Parser().Parse(tokens.Tokens);
        }

        private static string[] Roots(string formula)
        {
            var result = Parse(formula);
            Assert.True(result.Error.IsSuccess(), result.Error.ToString());
            return result.Sequences.Select(s => s.Root.Key).ToArray();
        }

        private static void AssertSyntaxError(string formula)
        {
            Assert.Equal(Errors.SyntaxErrorCode, Parse(formula).Error.Code);
        }

        private const string Example = "REQUIRE \"core-0.2+\"\n\n$a = 4\n$b = 3\n$c = $a^2 + $b^2\n\nSQRT($c)";

        [Fact]
        public void Parsing_Example_Sequences()
        {
            Assert.Equal(
                ["REQUIRE(\"core-0.2+\")", "SET($a,4)", "SET($b,3)", "SET($c,ADD(POW($a,2),POW($b,2)))", "SQRT($c)"],
                Roots(Example));
        }

        [Fact]
        public void Parsing_Example_Queue()
        {
            ISequence sequence = Parse(Example).Sequences[3];

            Assert.Equal(
                ["$a", "2", "$b", "2", "POW($a,2)", "POW($b,2)", "$c", "ADD(POW($a,2),POW($b,2))", "SET($c,ADD(POW($a,2),POW($b,2)))"],
                sequence.Queue.Select(n => n.Key));

            Assert.Equal([4, 4, 4, 4, 3, 3, 2, 2, 1], sequence.Queue.Select(n => n.Level));
        }

        [Fact]
        public void Parsing_Example_PrimitiveMap()
        {
            var map = Parse(Example).PrimitiveMap;

            Assert.Equal(15, map.Count);
            Assert.Equal(ExpressionKind.Value, map["\"core-0.2+\""].Kind);
            Assert.Equal(ExpressionKind.Variable, map["$a"].Kind);
            Assert.Equal(ExpressionKind.FunctionCall, map["POW($a,2)"].Kind);
            Assert.Equal(ExpressionKind.FunctionCall, map["SQRT($c)"].Kind);
        }

        // Operator translation table
        [Theory]
        [InlineData("1+2", "ADD(1,2)")]
        [InlineData("1-2", "SUB(1,2)")]
        [InlineData("1*2", "MUL(1,2)")]
        [InlineData("1/2", "DIV(1,2)")]
        [InlineData("1%2", "MOD(1,2)")]
        [InlineData("1^2", "POW(1,2)")]
        [InlineData("-1", "NEG(1)")]
        [InlineData("1==2", "EQU(1,2)")]
        [InlineData("1!=2", "NEQ(1,2)")]
        [InlineData("1<>2", "NEQ(1,2)")]
        [InlineData("1>2", "GRE(1,2)")]
        [InlineData("1<2", "LES(1,2)")]
        [InlineData("1>=2", "GEQ(1,2)")]
        [InlineData("1<=2", "LEQ(1,2)")]
        [InlineData("true&&false", "AND(true,false)")]
        [InlineData("true||false", "OR(true,false)")]
        [InlineData("!true", "NOT(true)")]
        [InlineData("$x=1", "SET($x,1)")]
        [InlineData("@x=1", "SET(@x,1)")]
        [InlineData("$x:=1", "INIT($x,1)")]
        [InlineData("$x?=1", "COND_SET($x,1)")]
        [InlineData("$a[1]", "GET($a,1)")]
        [InlineData("$a[\"key\"]", "GET($a,\"key\")")]
        [InlineData("$a.key", "GET($a,\"key\")")]
        [InlineData("[1, 2, 3]", "ARRAY(1,2,3)")]
        [InlineData("[]", "ARRAY()")]
        [InlineData("{ k1: 1, \"k2\": 2 }", "OBJECT(\"k1\",1,\"k2\",2)")]
        [InlineData("(p1, p2) => p1", "(p1,p2)=>p1")]
        public void OperatorTranslation(string formula, string expected)
        {
            Assert.Equal([expected], Roots(formula));
        }

        // operator-parsing.md, "Consequences of the table"
        [Theory]
        [InlineData("-$x^2", "POW(NEG($x),2)")]
        [InlineData("-2^2", "POW(NEG(2),2)")]
        [InlineData("2^3^2", "POW(2,POW(3,2))")]
        [InlineData("$a = $b = 1", "SET($a,SET($b,1))")]
        [InlineData("$a.key", "GET($a,\"key\")")]
        [InlineData("1 + 2 * 3", "ADD(1,MUL(2,3))")]
        [InlineData("1 - 2 - 3", "SUB(SUB(1,2),3)")]
        [InlineData("true || false && false", "OR(true,AND(false,false))")]
        [InlineData("(1 + 2) * 3", "MUL(ADD(1,2),3)")]
        [InlineData("$m[1][0]", "GET(GET($m,1),0)")]
        public void BindingPower(string formula, string expected)
        {
            Assert.Equal([expected], Roots(formula));
        }

        [Theory]
        [InlineData("SET $x 100", "SET($x,100)")]
        [InlineData("SET $x 1 + 2", "SET($x,ADD(1,2))")]
        [InlineData("REQUIRE \"core-0.2+\"", "REQUIRE(\"core-0.2+\")")]
        [InlineData("SET($x, 100)", "SET($x,100)")]
        public void ProcedureCallWithoutParentheses(string formula, string expected)
        {
            Assert.Equal([expected], Roots(formula));
        }

        [Fact]
        public void Sequences_AreSplitOnlyOutsideDelimiters()
        {
            Assert.Equal(["SET($x,1)", "SET($y,2)"], Roots("$x = 1; $y = 2"));
            Assert.Equal(["ARRAY(1,2)"], Roots("[1,\n2]"));
        }

        [Fact]
        public void LambdaBody_IsASeparateTree()
        {
            var result = Parse("MAP($arr, (e) => e * 2)");
            var map = result.PrimitiveMap;

            Assert.True(map.ContainsKey("(e)=>MUL(e,2)"));
            Assert.False(map.ContainsKey("MUL(e,2)"));

            var lambda = (LambdaNode)map["(e)=>MUL(e,2)"];
            Assert.Equal(["e", "2", "MUL(e,2)"], lambda.Body.Queue.Select(n => n.Key));
            Assert.Equal(ExpressionKind.Parameter, lambda.Body.Queue[0].Kind);
        }

        [Fact]
        public void IdentifierClassification()
        {
            var map = Parse("[PI, null, true, false, ABS(1)]").PrimitiveMap;

            Assert.Equal(ExpressionKind.Constant, map["PI"].Kind);
            Assert.Equal(ExpressionKind.Value, map["null"].Kind);
            Assert.Equal(ExpressionKind.Value, map["true"].Kind);
            Assert.Equal(ExpressionKind.FunctionCall, map["ABS(1)"].Kind);
        }

        // Core parsing, "Syntax errors"
        [Theory]
        [InlineData("1 +")]
        [InlineData("* 2")]
        [InlineData("(1, 2)")]
        [InlineData("1 2")]
        [InlineData("1 = 2")]
        [InlineData("ABS(1) = 2")]
        [InlineData("(1) => 2")]
        [InlineData("(x, x) => x")]
        [InlineData("1 < 2 < 3")]
        [InlineData("1 == 2 != 3")]
        [InlineData("$a.1")]
        [InlineData("$ 1")]
        [InlineData("{ \"my key\": 1 }")]
        [InlineData("{ 1: 1 }")]
        [InlineData("{ x: 1, \"x\": 2 }")]
        [InlineData("[1,]")]
        [InlineData("ABS(1,)")]
        [InlineData("{ x 1 }")]
        public void SyntaxErrors(string formula) => AssertSyntaxError(formula);

        [Fact]
        public void ParenthesizedIdentifier_IsAGroupedExpression()
        {
            Assert.Equal(["ADD(x,1)"], Roots("(x) + 1"));
        }

        [Fact]
        public void ObjectKey_SpelledLikeAKeyword_IsAKey()
        {
            Assert.Equal(["OBJECT(\"null\",1)"], Roots("{ null: 1 }"));
        }
    }
}
