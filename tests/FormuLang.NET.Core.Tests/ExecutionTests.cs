using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Processing;
using FormuLang.NET.Core.Structures;
using static FormuLang.NET.Core.Tests.FormulaRunner;
using Env = FormuLang.NET.Core.Structures.Environment;

namespace FormuLang.NET.Core.Tests
{
    /// <summary>
    /// End-to-end execution: docs/semantics/evaluation.md, procedures and the Core examples.
    /// </summary>
    public class ExecutionTests
    {
        [Fact]
        public void Parsing_Example_Result()
        {
            Assert.Equal("5", Run("REQUIRE \"core-0.2+\"\n\n$a = 4\n$b = 3\n$c = $a^2 + $b^2\n\nSQRT($c)"));
        }

        // evaluation.md, "Reading variables"
        [Theory]
        [InlineData("$x", "[SUCCESS (0)] Operation completed successfully.")]
        [InlineData("$x\n$x + 1", "[UNDEFINED_ERROR (12)] Variable \"x\" has no value.")]
        [InlineData("$y + 1", "[UNKNOWN_SYMBOL_ERROR (5)] Variable \"y\" is not defined.")]
        [InlineData("$x\n$x = 2\n$x", "2")]
        [InlineData("@x", "[ENVIRONMENT_ERROR (10)] Environment variable \"x\" is not defined.")]
        [InlineData("UNKNOWN", "[UNKNOWN_SYMBOL_ERROR (5)] Constant \"UNKNOWN\" is not defined.")]
        [InlineData("UNKNOWN(1)", "[UNKNOWN_SYMBOL_ERROR (5)] Function \"UNKNOWN\" is not defined.")]
        public void ReadingVariables(string formula, string expected)
        {
            Assert.Equal(expected, Run(formula));
        }

        // evaluation.md, "Sequence result"
        [Fact]
        public void SequenceResult_Example()
        {
            Assert.Equal("0.25", Run("$x = 4\n$y = IF($x > 0, DIV(1, $x), ERROR(\"Negative!\"))\n$y"));
            Assert.Equal("[CUSTOM_ERROR (1)] Not negative!", Run("$x = 4\n$z = IF($x < 0, 1, ERROR(\"Not negative!\"))\n$x"));
            Assert.Equal("true", Run("ISERR(ERROR(\"a\"))"));
        }

        [Fact]
        public void AbortedExecution_SkipsTheRemainingSequences()
        {
            var result = Execute("$x = 1\n$y = DIV(1, 0)\n$x = 2");

            Assert.Equal(Errors.OperationErrorCode, result.Error.Code);
            Assert.Equal("1", result.Context.GetProperty("x").GetValue().ToString());
        }

        [Fact]
        public void SuppressErrors_ReplacesTheErrorWithNull()
        {
            Assert.Equal("7", Run("@suppress_errors = true\n$a = DIV(1, 0)\n7"));
            Assert.Equal("null", Run("@suppress_errors = true\nDIV(1, 0)"));
        }

        [Fact]
        public void UnselectedBranch_IsComputedAnyway()
        {
            // All arguments are computed before IF, so both assignments happen
            var result = Execute("IF(true, $a = 1, $b = 2)\n$a + $b");
            Assert.Equal("3", result.Value.ToString());
        }

        // procedures/SET.md
        [Theory]
        [InlineData("SET $x 4.25\n$x", "4.25")]
        [InlineData("$x = 4.25\n$x", "4.25")]
        [InlineData("$x\n$x = 1.2\n$x", "1.2")]
        [InlineData("$x = TRUE\n$x = 1.2", "[TYPE_ERROR (4)] Trying to override BOOLEAN value with NUMBER.")]
        [InlineData("$array_indexing = 1", "[INVALID_IDENTIFIER_ERROR (9)] \"array_indexing\" symbol is already defined by environment.")]
        [InlineData("@array_indexing = 1\n@array_indexing", "1")]
        [InlineData("@x = 1", "[ENVIRONMENT_ERROR (10)] Environment variable \"x\" is not defined.")]
        [InlineData("@array_indexing = \"one\"", "[TYPE_ERROR (4)] Trying to override NUMBER value with TEXT.")]
        [InlineData("SET(5, 1)", "[ARGUMENT_ERROR (6)] No overload of \"SET\" matches the arguments (Number, Number).")]
        [InlineData("$x = ERROR(\"e\")", "[CUSTOM_ERROR (1)] e")]
        public void Set(string formula, string expected)
        {
            Assert.Equal(expected, Run(formula));
        }

        // procedures/INIT.md
        [Theory]
        [InlineData("INIT $count 0\n$count", "0")]
        [InlineData("$count := 0\n$count", "0")]
        [InlineData("$count = 5\n$count := 0\n$count", "5")]
        [InlineData("$count\n$count := 0\n$count", "0")]
        [InlineData("@array_indexing := 1\n@array_indexing", "0")]
        [InlineData("@x := 1", "[ENVIRONMENT_ERROR (10)] Environment variable \"x\" is not defined.")]
        public void Init(string formula, string expected)
        {
            Assert.Equal(expected, Run(formula));
        }

        // procedures/COND_SET.md
        [Theory]
        [InlineData("$x = 1\nCOND_SET $x 2\n$x", "2")]
        [InlineData("$x = 1\n$x ?= 2\n$x", "2")]
        [InlineData("$x ?= 2\n$x + 0", "[UNKNOWN_SYMBOL_ERROR (5)] Variable \"x\" is not defined.")]
        [InlineData("$x = 1\n$x ?= \"text\"", "[TYPE_ERROR (4)] Trying to override NUMBER value with TEXT.")]
        [InlineData("@array_indexing ?= 1\n@array_indexing", "1")]
        [InlineData("@x ?= 1", "[SUCCESS (0)] Operation completed successfully.")]
        public void ConditionalSet(string formula, string expected)
        {
            Assert.Equal(expected, Run(formula));
        }

        // procedures/REQUIRE.md
        [Theory]
        [InlineData("REQUIRE \"core-0.2+\"", "[SUCCESS (0)] Operation completed successfully.")]
        [InlineData("REQUIRE([\"core-0.x\", \"core-0.3\"])", "[SUCCESS (0)] Operation completed successfully.")]
        [InlineData("REQUIRE \"stock-5.2+\"", "[NOT_SUPPORTED_ERROR (11)] Standard \"stock-5.2+\" is not supported.")]
        [InlineData("REQUIRE \"core-1.0+\"", "[NOT_SUPPORTED_ERROR (11)] Standard \"core-1.0+\" is not supported.")]
        [InlineData("REQUIRE \"core\"", "[OPERATION_ERROR (3)] \"core\" is not a valid standard requirement.")]
        [InlineData("REQUIRE([1])", "[ARGUMENT_ERROR (6)] No overload of \"REQUIRE\" matches the arguments (Array).")]
        public void Require(string formula, string expected)
        {
            Assert.Equal(expected, Run(formula));
        }

        [Theory]
        [InlineData("core-0.2", "core-0.2", true)]
        [InlineData("core-0.2", "core-0.1", false)]
        [InlineData("core-0.2", "core-1.0", false)]
        [InlineData("core-0.2+", "core-0.5", true)]
        [InlineData("core-0.2+", "core-1.0", false)]
        [InlineData("core-0.x", "core-0.1", true)]
        [InlineData("core-0.x", "core-1.0", false)]
        [InlineData("core-1", "core-1.4", true)]
        [InlineData("core-1", "core-2.0", false)]
        [InlineData("core-1+", "core-2.3", true)]
        [InlineData("core-1+", "core-0.2", false)]
        [InlineData("stock-5.2+", "stock-5.7", true)]
        [InlineData("stock-5.2+", "stock-6.0", false)]
        public void RequirementFormat(string requirement, string standard, bool satisfied)
        {
            string name = requirement[..requirement.IndexOf('-')];
            string[] pattern = requirement[(requirement.IndexOf('-') + 1)..].Split('.');

            Assert.Equal(satisfied, Standards.CoreProcedures.Satisfies(standard, name, pattern));
        }

        // examples/context_examples.md
        [Theory]
        [InlineData("$elements = ARRAY(1, 2, 3, 4, 5)\n$n = 5\n$sum = SUM($elements)\nDIV($sum, $n)", "3")]
        [InlineData("$elements = ARRAY(10, 20, 30, 40, 50)\nSUB(MAX($elements), MIN($elements))", "40")]
        [InlineData("$x\nCLAMP($x, 0, 1)", "[UNDEFINED_ERROR (12)] Variable \"x\" has no value.")]
        [InlineData("$x\n$x = 1.2\nCLAMP($x, 0, 1)", "1")]
        public void ContextExamples(string formula, string expected)
        {
            Assert.Equal(expected, Run(formula));
        }

        // examples/environment_examples.md
        [Theory]
        [InlineData("$standards = JOIN(STANDARDS, \", \")\nCONCAT(\"Supported standards: \", $standards)", "Supported standards: core-0.3")]
        [InlineData("REQUIRE \"core-0.3\"\nREQUIRE \"stock-5.2+\"\n$diff = CLOSE[I] - OPEN[I]", "[NOT_SUPPORTED_ERROR (11)] Standard \"stock-5.2+\" is not supported.")]
        [InlineData("$radius = 2\nMUL(PI, POW($radius, 2))", "12.566370614359172")]
        [InlineData("@array_indexing = 1\n$elements = ARRAY(10, 20, 30, 40)\n$elements[1]", "10")]
        public void EnvironmentExamples(string formula, string expected)
        {
            Assert.Equal(expected, Run(formula));
        }

        // examples/number_examples.md, text_examples.md, array_examples.md, common_formulas.md
        [Theory]
        [InlineData("5 + 3", "8")]
        [InlineData("15 * 0.5", "7.5")]
        [InlineData("4.2E+3 * 2E-2", "84")]
        [InlineData("$a = -40\n$b = .8\n$c = 2.5e-1\n$d = 0x2c\n$a * $b + $c * $d", "-21")]
        [InlineData("\"Hello, world!\"", "Hello, world!")]
        [InlineData("\"A1\\tB1\\nA2\\tB2\"", "A1\tB1\nA2\tB2")]
        [InlineData("\"Backslash: \\\\\\nHashtag: #\\nQuotation: \\\"\"", "Backslash: \\\nHashtag: #\nQuotation: \"")]
        [InlineData("$text1 = \"Hello\"\n$text2 = \"world!\"\nJOIN([$text1, $text2], \", \")", "Hello, world!")]
        [InlineData("$text = \"apples | bananas | lemons\"\nSPLIT($text, \" | \")", "[\"apples\", \"bananas\", \"lemons\"]")]
        [InlineData("$a1 = ARRAY(1, 2, 3)\n$a2 = ARRAY(\"a\", \"b\", \"c\")\n[$a1, $a2]", "[[1, 2, 3], [\"a\", \"b\", \"c\"]]")]
        [InlineData("$a = 1\n$b = -3\n$c = 2\n$delta = $b^2 - 4*$a*$c\n$delta", "1")]
        [InlineData("$arr = [2, 4, 4, 4, 5, 5, 7, 9]\n$stdev = ROOT(SUM(MAP($arr, (e) => (e - AVG($arr))^2)) / LEN($arr), 2)\n$stdev", "2")]
        [InlineData("$arr = [2, 4, 4, 4, 5, 5, 7, 9]\nROOT(DIV(SUM(MAP($arr, (e) => POW(SUB(e, AVG($arr)), 2))), LEN($arr)), 2)", "2")]
        [InlineData("$x1 = 0\n$y1 = 0\n$x2 = 3\n$y2 = 4\n$d = (($x2 - $x1)^2 + ($y2 - $y1)^2) ^ 0.5\n$d", "5")]
        [InlineData("$p = 1000\n$r = 0.1\n$n = 1\n$t = 2\n$a = $p * (1 + $r/$n)^($n*$t)\nROUND($a, 2)", "1210")]
        public void CoreExamples(string formula, string expected)
        {
            Assert.Equal(expected, Run(formula));
        }

        // README of the standard
        [Fact]
        public void StandardReadmeExample()
        {
            string formula = """
                $a = 1
                $b = 2
                $c = $a + $b # 3
                $b1 = true
                $integer_number = 42
                $negative_number = -28
                $scientific_notation = 5.2e-3
                $hexadecimal_integer = 0x40C
                $message = "Hello, world!"
                $sum = ADD(4, 5)
                $l2 = 25 > 10
                $l3 = IF(MOD(4, 2) == 0, "Even", "Odd")
                $arr = [5, 6, 7, 8, 9]
                $element = $arr[2]
                $both = CONCAT("ab", "cd")
                $obj = { name: "John", age: 24 }
                $name = $obj.name
                $div_by_3 = (n) => EQU(MOD(n, 3), 0)
                $filtered = FILTER($arr, $div_by_3)
                $is_err = ISERR(ERROR("Division by zero (0) is not permitted!"))
                $result = $message + "!!"
                [$c, $hexadecimal_integer, $l3, $element, $name, $filtered, $is_err, $result]
                """;

            Assert.Equal("[3, 1036, \"Even\", 7, \"John\", [6, 9], true, \"Hello, world!!!\"]", Run(formula));
        }

        // Lambdas
        [Theory]
        [InlineData("$f = (x) => x * 2\nISDEL($f)", "true")]
        [InlineData("$f = (x, y) => x * y\nTEXT($f)", "(x, y) => any")]
        [InlineData("MAP([1, 2], (a, b, c) => a)", "[ARGUMENT_ERROR (6)] The lambda passed to \"MAP\" must have 1 or 2 parameter(s).")]
        [InlineData("MAP([1, 2], (e) => DIV(1, e - 1))", "[OPERATION_ERROR (3)] Division by zero.")]
        [InlineData("$k = 10\nMAP([1, 2], (e) => e + $k)", "[11, 12]")]
        [InlineData("MAP([[1, 2], [3]], (row) => SUM(MAP(row, (e) => e * 10)))", "[30, 30]")]
        [InlineData("MAP([-1, 2], ABS)", "[1, 2]")]
        public void Lambdas(string formula, string expected)
        {
            Assert.Equal(expected, Run(formula));
        }

        [Fact]
        public void PersistentContext_KeepsVariablesBetweenExecutions()
        {
            var env = Env.Create();
            var context = Context.Create(env, ContextType.Persistent);
            var executor = Executor.Create();

            Assert.Equal("1", executor.Execute("$count := 0\n$count = $count + 1\n$count", context).Value.ToString());
            Assert.Equal("2", executor.Execute("$count := 0\n$count = $count + 1\n$count", context).Value.ToString());
        }

        [Fact]
        public void EnvironmentVariables_AreChangedOnlyInTheContext()
        {
            var env = Env.Create();
            Executor.Create().Execute("@array_indexing = 1", env);

            Assert.Equal("0", env.GetProperty("array_indexing").GetValue().ToString());
        }

        [Fact]
        public void HostProvidedContextVariables_CanBeRead()
        {
            var context = Context.Create(Env.Create());
            context.InitVariable("price", Value.Number(10, "10"));
            context.InitConstant("TAX", Value.Number(0.23, "0.23"));

            Assert.Equal("12.3", Executor.Create().Execute("$price * (1 + TAX)", context).Value.ToString());
        }

        [Fact]
        public void SyntaxErrors_StopBeforeEvaluation()
        {
            var result = Execute("$x = 1\n$y = 1 +");
            Assert.Equal(Errors.SyntaxErrorCode, result.Error.Code);
            Assert.False(result.Context.HasKey("x"));
        }
    }
}
