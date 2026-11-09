using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Processing;
using FormuLang.NET.Core.Standards;
using FormuLang.NET.Core.Structures;
using FormuLang.NET.Core.Utils;
using static FormuLang.NET.Core.Tests.FormulaRunner;
using Env = FormuLang.NET.Core.Structures.Environment;

namespace FormuLang.NET.Core.Tests
{
    /// <summary>
    /// Changes of the Core standard since <c>Adisoreq/FormuLang@6243bf6</c>: new functions, MAP with an index, SEQ,
    /// serialization variables, environment checks and the Iteration Context.
    /// Examples come from the Examples sections of docs/standards/core/functions/*.md.
    /// </summary>
    public class Core03Tests
    {
        private static void AssertResult(string formula, string expected)
        {
            string actual = Run(formula);

            if (expected.EndsWith("_ERROR"))
                Assert.StartsWith($"[{expected} (", actual);
            else
                Assert.Equal(expected, actual);
        }

        [Fact]
        public void EveryNewFunctionIsDefined()
        {
            string[] documented =
            [
                "SIGN", "TRUNC", "SUBSTR", "FIND", "CONTAINS", "STARTS", "ENDS", "REPLACE", "REVERSE", "REDUCE", "ANY", "ALL",
                "SORT", "KEYS", "HAS", "SEQ", "SIN", "COS", "TAN", "ASIN", "ACOS", "ATAN", "ATAN2", "DEG", "RAD",
            ];

            var defined = CoreStandard.Instance.NamedFunctions.Select(f => f.Name).ToHashSet();
            Assert.Empty(documented.Where(name => !defined.Contains(name)));
        }

        [Theory]
        // SIGN, TRUNC
        [InlineData("SIGN(42)", "1")] [InlineData("SIGN(-0.5)", "-1")] [InlineData("SIGN(0)", "0")]
        [InlineData("TRUNC(4.29, 1)", "4.2")] [InlineData("TRUNC(-4.29, 1)", "-4.2")] [InlineData("TRUNC(7.9, 0)", "7")]
        [InlineData("TRUNC(-7.9, 0)", "-7")] [InlineData("TRUNC(129, -1)", "120")] [InlineData("TRUNC(1, 0.5)", "OPERATION_ERROR")]
        // Trigonometric
        [InlineData("SIN(0)", "0")] [InlineData("SIN(1.5707963267948966)", "1")]
        [InlineData("COS(0)", "1")] [InlineData("TAN(0)", "0")]
        [InlineData("ASIN(0)", "0")] [InlineData("ASIN(2)", "OPERATION_ERROR")] [InlineData("ACOS(-1.5)", "OPERATION_ERROR")]
        [InlineData("ACOS(1)", "0")] [InlineData("ATAN(0)", "0")]
        [InlineData("ATAN2(1, 1)", "0.7853981633974483")] [InlineData("ATAN2(0, -1)", "3.141592653589793")] [InlineData("ATAN2(0, 0)", "0")]
        [InlineData("ATAN2(1, 0)", "1.5707963267948966")]
        [InlineData("DEG(3.141592653589793)", "180")] [InlineData("DEG(1.5707963267948966)", "90")] [InlineData("DEG(-0.7853981633974483)", "-45")]
        [InlineData("RAD(180)", "3.141592653589793")] [InlineData("RAD(90)", "1.5707963267948966")] [InlineData("RAD(-45)", "-0.7853981633974483")]
        // Text
        [InlineData("LEN(\"FormuLang\")", "9")] [InlineData("LEN(\" a b \")", "5")] [InlineData("LEN(\"\")", "0")]
        [InlineData("SUBSTR(\"FormuLang\", 5)", "Lang")] [InlineData("SUBSTR(\"FormuLang\", 9)", "")]
        [InlineData("SUBSTR(\"FormuLang\", 0, 5)", "Formu")] [InlineData("SUBSTR(\"abc\", 1, 0)", "")]
        [InlineData("SUBSTR(\"abc\", 1, 5)", "INVALID_INDEX_ERROR")] [InlineData("SUBSTR(\"abc\", 4)", "INVALID_INDEX_ERROR")]
        [InlineData("SUBSTR(\"abc\", 0, -1)", "OPERATION_ERROR")] [InlineData("SUBSTR(\"abc\", 0.5, 1)", "INVALID_INDEX_ERROR")]
        [InlineData("FIND(\"FormuLang\", \"Lang\")", "5")] [InlineData("FIND(\"banana\", \"an\")", "1")] [InlineData("FIND(\"banana\", \"x\")", "null")]
        [InlineData("FIND(\"abc\", \"\")", "0")]
        [InlineData("CONTAINS(\"FormuLang\", \"mu\")", "true")] [InlineData("CONTAINS(\"FormuLang\", \"MU\")", "false")]
        [InlineData("STARTS(\"FormuLang\", \"Form\")", "true")] [InlineData("STARTS(\"FormuLang\", \"form\")", "false")]
        [InlineData("STARTS(\"ab\", \"abc\")", "false")] [InlineData("STARTS(\"abc\", \"\")", "true")]
        [InlineData("ENDS(\"report.csv\", \".csv\")", "true")] [InlineData("ENDS(\"report.csv\", \".CSV\")", "false")] [InlineData("ENDS(\"abc\", \"\")", "true")]
        [InlineData("REPLACE(\"a-b-c\", \"-\", \", \")", "a, b, c")] [InlineData("REPLACE(\"aaaa\", \"aa\", \"b\")", "bb")]
        [InlineData("REPLACE(\"Hello\", \"l\", \"\")", "Heo")] [InlineData("REPLACE(\"abc\", \"x\", \"y\")", "abc")]
        [InlineData("REPLACE(\"abc\", \"\", \"y\")", "OPERATION_ERROR")]
        [InlineData("REVERSE(\"abc\")", "cba")] [InlineData("REVERSE(\"\")", "")]
        // Arrays
        [InlineData("FIND([10, 20, 30, 20], 20)", "1")] [InlineData("FIND([[1, 2], [3, 4]], [3, 4])", "1")] [InlineData("FIND([1, 2, 3], \"2\")", "null")]
        [InlineData("CONTAINS([1, 2, 3], 2)", "true")] [InlineData("CONTAINS([1, 2, 3], \"2\")", "false")] [InlineData("CONTAINS([], null)", "false")]
        [InlineData("REVERSE([1, 2, 3])", "[3, 2, 1]")] [InlineData("REVERSE([[1, 2], [3, 4]])", "[[3, 4], [1, 2]]")] [InlineData("REVERSE([])", "[]")]
        [InlineData("REDUCE([1, 2, 3, 4], (acc, e) => acc + e, 0)", "10")] [InlineData("REDUCE([3, 7, 5], (acc, e) => MAX(acc, e), 0)", "7")]
        [InlineData("REDUCE([\"a\", \"b\", \"c\"], (acc, e) => e + acc, \"\")", "cba")] [InlineData("REDUCE([10, 20], (acc, e, i) => acc + i * e, 0)", "20")]
        [InlineData("REDUCE([], (acc, e) => acc + e, 100)", "100")]
        [InlineData("REDUCE([2, 3, 4], (acc, e) => IF(e > acc.max, { max: e }, acc), { max: 0 })", "{ max: 4 }")]
        [InlineData("REDUCE([1], (e) => e, 0)", "ARGUMENT_ERROR")]
        [InlineData("ANY([1, 5, 12], (e) => e > 10)", "true")] [InlineData("ANY([1, 5, 8], (e) => e > 10)", "false")] [InlineData("ANY([], (e) => e > 10)", "false")]
        [InlineData("ANY([1], (e) => e)", "OPERATION_ERROR")]
        [InlineData("ALL([2, 4, 6], (e) => e % 2 == 0)", "true")] [InlineData("ALL([2, 3, 6], (e) => e % 2 == 0)", "false")] [InlineData("ALL([], (e) => e % 2 == 0)", "true")]
        [InlineData("SORT([3, 1, 2])", "[1, 2, 3]")] [InlineData("SORT([-1.5, 10, 0])", "[-1.5, 0, 10]")] [InlineData("SORT([])", "[]")]
        [InlineData("SORT([\"pear\", \"apple\", \"fig\"])", "[\"apple\", \"fig\", \"pear\"]")] [InlineData("SORT([\"b\", \"B\", \"a\", \"ab\"])", "[\"B\", \"a\", \"ab\", \"b\"]")]
        [InlineData("SORT([ { name: \"Billy\", age: 18 }, { name: \"Anna\", age: 15 } ], (e) => e.age)", "[{ name: \"Anna\", age: 15 }, { name: \"Billy\", age: 18 }]")]
        [InlineData("SORT([1, 3, 2], (e) => -e)", "[3, 2, 1]")] [InlineData("SORT([\"bb\", \"a\", \"ccc\"], (e) => LEN(e))", "[\"a\", \"bb\", \"ccc\"]")]
        [InlineData("SORT([1, \"a\"])", "ARGUMENT_ERROR")] [InlineData("SORT([1, 2], (e) => IF(e == 1, 1, \"a\"))", "OPERATION_ERROR")]
        [InlineData("SORT([[2], [1, 1], [3]], (e) => LEN(e))", "[[2], [3], [1, 1]]")]
        [InlineData("HAS([10, 20, 30], 2)", "true")] [InlineData("HAS([10, 20, 30], 3)", "false")] [InlineData("HAS([10, 20, 30], -1)", "false")]
        [InlineData("HAS([10, 20, 30], 1.5)", "false")] [InlineData("HAS([], 0)", "false")] [InlineData("HAS([10, 20], \"1\")", "ARGUMENT_ERROR")]
        [InlineData("SEQ(5)", "[0, 1, 2, 3, 4]")] [InlineData("SEQ(0)", "[]")] [InlineData("SEQ(5, 2)", "[0, 2, 4, 6, 8]")]
        [InlineData("SEQ(4, -1)", "[0, -1, -2, -3]")] [InlineData("SEQ(3, 0.5)", "[0, 0.5, 1]")] [InlineData("SEQ(5, 2, 1)", "[1, 3, 5, 7, 9]")]
        [InlineData("SEQ(3, 10, 100)", "[100, 110, 120]")] [InlineData("SEQ(2.5, 1, 0)", "OPERATION_ERROR")] [InlineData("SEQ(-1)", "OPERATION_ERROR")]
        [InlineData("SEQ(4, (i) => 1 + 10 * i)", "[1, 11, 21, 31]")] [InlineData("SEQ(3, (i) => i * i)", "[0, 1, 4]")]
        [InlineData("SEQ(3, (i) => \"item\")", "[\"item\", \"item\", \"item\"]")] [InlineData("SEQ(3, (i, j) => i)", "ARGUMENT_ERROR")]
        [InlineData("MAP([10, 20, 30], (e, i) => i)", "[0, 1, 2]")] [InlineData("$b = [1, 2, 3]\nMAP([10, 20, 30], (e, i) => e + $b[i])", "[11, 22, 33]")]
        // Objects
        [InlineData("KEYS({ name: \"Ada\", age: 36 })", "[\"name\", \"age\"]")] [InlineData("KEYS({})", "[]")]
        [InlineData("$o = { a: 1, b: 2 }\nMAP(KEYS($o), (k) => $o[k])", "[1, 2]")]
        [InlineData("HAS({ name: \"Ada\" }, \"name\")", "true")] [InlineData("HAS({ name: \"Ada\" }, \"Name\")", "false")]
        [InlineData("HAS({ x: null }, \"x\")", "true")] [InlineData("$o = { a: 1 }\nIF(HAS($o, \"b\"), $o.b, 0)", "0")]
        [InlineData("HAS({ a: 1 }, 1)", "ARGUMENT_ERROR")]
        public void FunctionExamples(string formula, string expected) => AssertResult(formula, expected);

        [Theory]
        [InlineData("SUBSTR(\"FormuLang\", 1, 5)", "Formu")]
        [InlineData("FIND(\"FormuLang\", \"Lang\")", "6")]
        [InlineData("FIND([10, 20, 30], 20)", "2")]
        [InlineData("HAS([10, 20, 30], 3)", "true")]
        [InlineData("SEQ(5)", "[1, 2, 3, 4, 5]")]
        [InlineData("SEQ(4, -2, 0)", "[0, -2, -4, -6]")]
        [InlineData("MAP([10, 20, 30], (e, i) => i)", "[1, 2, 3]")]
        [InlineData("REDUCE([10, 20], (acc, e, i) => acc + i * e, 0)", "50")]
        public void FunctionExamples_WithArrayIndexing1(string formula, string expected)
            => AssertResult("@array_indexing = 1\n" + formula, expected);

        // Serialization

        [Theory]
        [InlineData(64, -1, "64")] [InlineData(-12.0, -1, "-12")] [InlineData(128.24801540, -1, "128.2480154")]
        [InlineData(16.45, 0, "16")] [InlineData(2.58, 0, "3")] [InlineData(1.451, 2, "1.45")] [InlineData(1.451, 3, "1.451")]
        [InlineData(1.451, 4, "1.4510")] [InlineData(0.6666, 3, "0.667")] [InlineData(0.9999, 3, "1.000")] [InlineData(0.001, 2, "0.00")]
        [InlineData(0.001, 3, "0.001")] [InlineData(0.001, 4, "0.0010")] [InlineData(-0.001, 2, "0.00")]
        public void Serialization_DecimalPlace(double value, int decimalPlace, string expected)
            => Assert.Equal(expected, StringConverter.NumberToString(value, decimalPlace));

        [Theory]
        [InlineData(-1, 12, "[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]")]
        [InlineData(10, 100, "[1, 2, 3, 4, 5, ..., 96, 97, 98, 99, 100]")]
        [InlineData(5, 100, "[1, 2, 3, ..., 99, 100]")]
        [InlineData(5, 3, "[1, 2, 3]")]
        [InlineData(0, 100, "[...]")]
        public void Serialization_MaxArrayPreview(int preview, int count, string expected)
        {
            var array = Enumerable.Range(1, count).Select(i => (Abstractions.Primitives.IValue)Value.Number(i, ""));
            Assert.Equal(expected, StringConverter.ArrayToString(array.ToArray(), new SerializationOptions { MaxArrayPreview = preview }));
        }

        [Theory]
        [InlineData("@split_array = true\nTEXT([\"abc\", \"def\", \"ghi\"])", "[\n  \"abc\",\n  \"def\",\n  \"ghi\"\n]")]
        [InlineData("@split_object = true\nTEXT({ name: \"John\", age: 24 })", "{\n  name: \"John\",\n  age: 24\n}")]
        [InlineData("@include_object_class = true\nTEXT({ name: \"John\", age: 24 })", "[Object] { name: \"John\", age: 24 }")]
        [InlineData("@include_object_class = true\n@split_object = true\nTEXT({ name: \"John\", age: 24 })", "[Object] {\n  name: \"John\",\n  age: 24\n}")]
        [InlineData("@split_array = true\nTEXT([[1, 2], 3])", "[\n  [\n    1,\n    2\n  ],\n  3\n]")]
        [InlineData("@decimal_place = 2\nTEXT(PI)", "3.14")]
        [InlineData("@decimal_place = 2\nJOIN([1, 2.5], \"; \")", "1.00; 2.50")]
        [InlineData("@max_array_preview = 2\nTEXT(SEQ(10))", "[0, ..., 9]")]
        public void Serialization_VariablesOfTheExecution(string formula, string expected) => AssertResult(formula, expected);

        [Fact]
        public void Serialization_ResultUsesTheVariablesOfTheExecution()
        {
            ExecutionResult result = Execute("@decimal_place = 3\n2 / 3");
            Assert.Equal("0.667", result.ToString());
            Assert.Equal("0.6666666666666666", result.Value.ToString());
        }

        // Environment

        [Fact]
        public void Environment_HasSerializationVariablesAndKeywordConstants()
        {
            var env = Env.Create();

            foreach (string name in new[] { "decimal_place", "split_array", "split_object", "include_object_class", "max_array_preview" })
                Assert.True(env.GetProperty(name).Mutable, name);

            Assert.Equal("-1", env.GetProperty("decimal_place").GetValue().ToString());
            Assert.Equal("-1", env.GetProperty("max_array_preview").GetValue().ToString());
            Assert.Empty(Validator.Validate("1", env).Errors);
        }

        [Fact]
        public void Validation_KeywordConstantsAndSerializationVariables()
        {
            var env = Env.Create();
            env.RemoveProperty("TRUE");
            env.InitConstant("TRUE", Value.Boolean(false, "false"));
            env.RemoveProperty("NULL");
            env.RemoveProperty("decimal_place");
            env.InitVariable("decimal_place", Value.Number(1.5, "1.5"));
            env.RemoveProperty("split_array");

            Assert.Equal(
            [
                "[ENVIRONMENT_ERROR (10)] Environment constant \"NULL\" is missing or is not of type Null.",
                "[ENVIRONMENT_ERROR (10)] Environment constant \"TRUE\" must be true.",
                "[ENVIRONMENT_ERROR (10)] Environment variable \"decimal_place\" must be a whole number.",
                "[ENVIRONMENT_ERROR (10)] Environment variable \"split_array\" is missing or is not of type Boolean.",
            ],
            Validator.Validate("1", env).Errors.Select(e => e.ToString()!));
        }

        // Iteration Context

        [Fact]
        public void IterationContext_ConstantsOfEveryIteration()
        {
            var context = IterationContext.Create(Env.Create(), 3);
            IterationExecutionResult result = Executor.Create().ExecuteIterations("[I, N, PREV]", context);

            Assert.True(result.Successful);
            Assert.Equal(["[0, 3, null]", "[1, 3, [0, 3, null]]", "[2, 3, [1, 3, [0, 3, null]]]"], result.Values.Select(v => v.ToString()!));
        }

        [Fact]
        public void IterationContext_IFollowsArrayIndexingAtTheMomentItIsRead()
        {
            var context = IterationContext.Create(Env.Create(), 2);
            IterationExecutionResult result = Executor.Create().ExecuteIterations("$a = I\n@array_indexing = 1\n[$a, I, N]", context);

            Assert.Equal(["[0, 1, 2]", "[1, 2, 2]"], result.Values.Select(v => v.ToString()!));
        }

        [Fact]
        public void IterationContext_VariablesAreNotKeptBetweenIterations()
        {
            var context = IterationContext.Create(Env.Create(), 3);
            IterationExecutionResult result = Executor.Create().ExecuteIterations("$x := I\n$x", context);

            Assert.Equal(["0", "1", "2"], result.Values.Select(v => v.ToString()!));
        }

        [Fact]
        public void IterationContext_PrevAccumulates()
        {
            var context = IterationContext.Create(Env.Create(), 4);
            IterationExecutionResult result = Executor.Create().ExecuteIterations("IF(ISNULL(PREV), 1, PREV * 2)", context);

            Assert.Equal(["1", "2", "4", "8"], result.Values.Select(v => v.ToString()!));
        }

        [Fact]
        public void IterationContext_ConstantsCannotBeChanged()
        {
            var context = IterationContext.Create(Env.Create(), 1);
            IterationExecutionResult result = Executor.Create().ExecuteIterations("SET($I, 5)", context);

            Assert.False(result.Successful);
        }

        [Fact]
        public void IterationContext_HostDecidesAboutTheRemainingIterations()
        {
            const string formula = "IF(I == 1, ERROR(\"stop\"), I)";

            var stopped = Executor.Create().ExecuteIterations(formula, IterationContext.Create(Env.Create(), 3));
            Assert.Equal(2, stopped.Results.Count);
            Assert.Equal("CUSTOM_ERROR", stopped.Error.Name);

            var all = Executor.Create().ExecuteIterations(formula, IterationContext.Create(Env.Create(), 3), stopOnError: false);
            Assert.Equal(3, all.Results.Count);
            Assert.Equal("2", all.Results[2].Value.ToString());
        }

        [Fact]
        public void IterationContext_InvalidFormulaFailsOnce()
        {
            var result = Executor.Create().ExecuteIterations("(1", IterationContext.Create(Env.Create(), 5));
            Assert.Single(result.Results);
            Assert.Equal("SYNTAX_ERROR", result.Error.Name);
        }
    }
}
