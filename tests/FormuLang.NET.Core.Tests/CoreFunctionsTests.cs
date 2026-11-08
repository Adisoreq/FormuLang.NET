using FormuLang.NET.Core.Standards;
using static FormuLang.NET.Core.Tests.FormulaRunner;

namespace FormuLang.NET.Core.Tests
{
    /// <summary>
    /// Examples and Errors sections of docs/standards/core/functions/*.md.
    /// Errors are compared by name only; their messages are implementation-defined.
    /// </summary>
    public class CoreFunctionsTests
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
        public void EveryDocumentedFunctionIsDefined()
        {
            string[] documented =
            [
                "ABS", "ADD", "AND", "ARRAY", "AVG", "BOOL", "CEIL", "CLAMP", "CONCAT", "DIV", "EQU", "ERROR", "EXP", "FILTER",
                "FLOOR", "GEQ", "GET", "GRE", "IF", "ISARR", "ISBOOL", "ISDEL", "ISERR", "ISNULL", "ISNUM", "ISOBJ", "ISTEXT",
                "JOIN", "LEN", "LEQ", "LES", "LN", "LOG", "LOWERCASE", "MAP", "MAX", "MIN", "MOD", "MUL", "NEG", "NEQ", "NOT",
                "NUMBER", "OR", "POW", "PROD", "RANGE", "ROOT", "ROUND", "SPLIT", "SQRT", "SUB", "SUM", "TEXT", "TRIM",
                "UPPERCASE", "XOR", "SET", "INIT", "COND_SET", "REQUIRE",
            ];

            var defined = CoreStandard.Instance.NamedFunctions.Select(f => f.Name).ToHashSet();
            Assert.Empty(documented.Where(name => !defined.Contains(name)));
        }

        [Theory]
        // Arithmetic
        [InlineData("ABS(-5)", "5")] [InlineData("ABS(5)", "5")] [InlineData("ABS(0)", "0")]
        [InlineData("ADD(10, 20)", "30")] [InlineData("ADD(-4, 4)", "0")] [InlineData("ADD(0.2, 2.3)", "2.5")]
        [InlineData("ADD(\"Hello, \", \"world!\")", "Hello, world!")] [InlineData("ADD(\"\", \"test\")", "test")]
        [InlineData("ADD(1, \"a\")", "ARGUMENT_ERROR")] [InlineData("ADD(1e308, 1e308)", "OPERATION_ERROR")]
        [InlineData("SUB(20, 10)", "10")] [InlineData("SUB(10, 20)", "-10")] [InlineData("SUB(2.5, 0.5)", "2")]
        [InlineData("MUL(3, 4)", "12")] [InlineData("MUL(-2, 5)", "-10")] [InlineData("MUL(0.5, 4)", "2")]
        [InlineData("DIV(10, 2)", "5")] [InlineData("DIV(7, 2)", "3.5")] [InlineData("DIV(5, 0)", "OPERATION_ERROR")]
        [InlineData("MOD(10, 3)", "1")] [InlineData("MOD(7, 4)", "3")] [InlineData("MOD(8, 2)", "0")] [InlineData("MOD(-7, 3)", "-1")]
        [InlineData("MOD(1, 0)", "OPERATION_ERROR")]
        [InlineData("POW(2, 3)", "8")] [InlineData("POW(5, 0)", "1")] [InlineData("POW(4, 0.5)", "2")] [InlineData("POW(0, 0)", "1")]
        [InlineData("POW(0, -1)", "OPERATION_ERROR")] [InlineData("POW(-8, 0.5)", "OPERATION_ERROR")]
        [InlineData("EXP(0)", "1")] [InlineData("EXP(1)", "2.718281828459045")] [InlineData("EXP(1000)", "OPERATION_ERROR")]
        [InlineData("ROOT(8, 3)", "2")] [InlineData("ROOT(16, 4)", "2")] [InlineData("ROOT(9, 2)", "3")] [InlineData("ROOT(-8, 3)", "-2")]
        [InlineData("ROOT(-16, 4)", "OPERATION_ERROR")] [InlineData("ROOT(4, 0)", "OPERATION_ERROR")]
        [InlineData("SQRT(9)", "3")] [InlineData("SQRT(2)", "1.4142135623730951")] [InlineData("SQRT(0)", "0")] [InlineData("SQRT(-1)", "OPERATION_ERROR")]
        [InlineData("LOG(8, 2)", "3")] [InlineData("LOG(100, 10)", "2")] [InlineData("LOG(1, 5)", "0")]
        [InlineData("LOG(0, 2)", "OPERATION_ERROR")] [InlineData("LOG(8, 1)", "OPERATION_ERROR")] [InlineData("LOG(8, -2)", "OPERATION_ERROR")]
        [InlineData("LN(1)", "0")] [InlineData("LN(2.718281828459045)", "1")] [InlineData("LN(10)", "2.302585092994046")] [InlineData("LN(0)", "OPERATION_ERROR")]
        [InlineData("NEG(5)", "-5")] [InlineData("NEG(-5)", "5")] [InlineData("NEG(0)", "0")]
        [InlineData("CEIL(4.21, 1)", "4.3")] [InlineData("CEIL(4.2, 0)", "5")] [InlineData("CEIL(-4.26, 1)", "-4.2")] [InlineData("CEIL(123, -1)", "130")]
        [InlineData("CEIL(1, 0.5)", "OPERATION_ERROR")]
        [InlineData("FLOOR(4.29, 1)", "4.2")] [InlineData("FLOOR(4.8, 0)", "4")] [InlineData("FLOOR(-4.21, 1)", "-4.3")] [InlineData("FLOOR(129, -1)", "120")]
        [InlineData("ROUND(4.21, 1)", "4.2")] [InlineData("ROUND(4.26, 1)", "4.3")] [InlineData("ROUND(4.5, 0)", "5")] [InlineData("ROUND(-4.5, 0)", "-5")]
        [InlineData("ROUND(125, -1)", "130")] [InlineData("ROUND(1, 1.5)", "OPERATION_ERROR")]
        [InlineData("CLAMP(5, 0, 10)", "5")] [InlineData("CLAMP(-3, 0, 10)", "0")] [InlineData("CLAMP(15, 0, 10)", "10")] [InlineData("CLAMP(1, 10, 0)", "OPERATION_ERROR")]
        // Logic and comparison
        [InlineData("AND(true, true)", "true")] [InlineData("AND(true, false)", "false")] [InlineData("AND(false, false)", "false")]
        [InlineData("OR(true, false)", "true")] [InlineData("OR(false, false)", "false")] [InlineData("OR(true, true)", "true")]
        [InlineData("XOR(false, false)", "false")] [InlineData("XOR(true, false)", "true")] [InlineData("XOR(false, true)", "true")] [InlineData("XOR(true, true)", "false")]
        [InlineData("NOT(true)", "false")] [InlineData("NOT(false)", "true")] [InlineData("NOT(1)", "ARGUMENT_ERROR")]
        [InlineData("EQU(5, 5)", "true")] [InlineData("EQU(5, 6)", "false")] [InlineData("EQU(\"a\", \"a\")", "true")] [InlineData("EQU(\"a\", \"A\")", "false")]
        [InlineData("EQU(1, \"1\")", "false")] [InlineData("EQU(null, null)", "true")] [InlineData("EQU([1, [2]], [1, [2]])", "true")] [InlineData("EQU([1], [1, 2])", "false")]
        [InlineData("EQU({ a: 1 }, { a: 1 })", "true")] [InlineData("EQU({ a: 1 }, { b: 1 })", "false")] [InlineData("EQU(ABS, ABS)", "true")]
        [InlineData("EQU(ERROR(\"a\"), ERROR(\"a\"))", "true")] [InlineData("EQU(ERROR(\"a\"), ERROR(\"b\"))", "false")]
        [InlineData("NEQ(5, 6)", "true")] [InlineData("NEQ(5, 5)", "false")] [InlineData("NEQ(\"a\", \"b\")", "true")]
        [InlineData("GRE(5, 3)", "true")] [InlineData("GRE(3, 5)", "false")] [InlineData("GRE(5, 5)", "false")]
        [InlineData("LES(3, 5)", "true")] [InlineData("LES(5, 3)", "false")] [InlineData("LES(5, 5)", "false")]
        [InlineData("GEQ(5, 5)", "true")] [InlineData("GEQ(6, 5)", "true")] [InlineData("GEQ(4, 5)", "false")]
        [InlineData("LEQ(5, 5)", "true")] [InlineData("LEQ(4, 5)", "true")] [InlineData("LEQ(6, 5)", "false")]
        // Control flow
        [InlineData("IF(true, \"High\", \"Low\")", "High")] [InlineData("IF(false, 1, 0)", "0")] [InlineData("IF(false, \"High\", \"Low\")", "Low")]
        [InlineData("IF(true, 1, ERROR(\"Invalid\"))", "1")] [InlineData("IF(false, 1, ERROR(\"Invalid\"))", "[CUSTOM_ERROR (1)] Invalid")]
        [InlineData("IF(1, 1, 0)", "ARGUMENT_ERROR")] [InlineData("IF(DIV(1, 0), 1, 0)", "OPERATION_ERROR")]
        [InlineData("ERROR(\"Invalid input\")", "[CUSTOM_ERROR (1)] Invalid input")]
        // Text
        [InlineData("CONCAT(\"foo\", \"bar\")", "foobar")] [InlineData("CONCAT(\"\", \"test\")", "test")]
        [InlineData("TRIM(\"  hello  \")", "hello")] [InlineData("TRIM(\" a b \")", "a b")] [InlineData("TRIM(\"\\t\\nx\\n\")", "x")]
        [InlineData("UPPERCASE(\"hello\")", "HELLO")] [InlineData("UPPERCASE(\"World\")", "WORLD")]
        [InlineData("LOWERCASE(\"HELLO\")", "hello")] [InlineData("LOWERCASE(\"World\")", "world")]
        [InlineData("JOIN([1, 2, 3])", "123")] [InlineData("JOIN([1, 2, 3], \"-\")", "1-2-3")] [InlineData("JOIN([\"a\", \"b\"], \", \")", "a, b")]
        [InlineData("JOIN([])", "")] [InlineData("JOIN([1, ERROR(\"e\")])", "[CUSTOM_ERROR (1)] e")]
        [InlineData("SPLIT(\"a,b,c\", \",\")", "[\"a\", \"b\", \"c\"]")] [InlineData("SPLIT(\"a,,b\", \",\")", "[\"a\", \"\", \"b\"]")]
        [InlineData("SPLIT(\"abc\", \"\")", "[\"a\", \"b\", \"c\"]")] [InlineData("SPLIT(\"abc\", \";\")", "[\"abc\"]")]
        // Arrays and collections
        [InlineData("ARRAY(1, 2, 3)", "[1, 2, 3]")] [InlineData("ARRAY()", "[]")] [InlineData("LEN(ARRAY(ERROR(\"e\")))", "1")]
        [InlineData("GET([10, 20, 30], 1)", "20")] [InlineData("[10, 20, 30][2]", "30")] [InlineData("[10, 20][2]", "INVALID_INDEX_ERROR")]
        [InlineData("[10, 20][0.5]", "INVALID_INDEX_ERROR")] [InlineData("[10, 20][-1]", "INVALID_INDEX_ERROR")]
        [InlineData("GET({\"name\": \"Ada\"}, \"name\")", "Ada")] [InlineData("{\"name\": \"Ada\"}.name", "Ada")] [InlineData("{name: \"Ada\"}[\"name\"]", "Ada")]
        [InlineData("{name: \"Ada\"}[\"first name\"]", "INVALID_KEY_ERROR")] [InlineData("GET(1, 1)", "ARGUMENT_ERROR")]
        [InlineData("SUM([1, 2, 3])", "6")] [InlineData("SUM([5, 10, 15])", "30")] [InlineData("SUM([])", "0")] [InlineData("SUM([1, \"2\"])", "ARGUMENT_ERROR")]
        [InlineData("SUM([1, ERROR(\"e\")])", "[CUSTOM_ERROR (1)] e")]
        [InlineData("PROD([1, 2, 3])", "6")] [InlineData("PROD([2, 2, 2])", "8")] [InlineData("PROD([])", "1")]
        [InlineData("AVG([1, 2, 3])", "2")] [InlineData("AVG([10, 20, 30])", "20")] [InlineData("AVG([])", "OPERATION_ERROR")]
        [InlineData("LEN([1, 2, 3])", "3")] [InlineData("LEN([])", "0")] [InlineData("LEN([[1, 2], 3])", "2")]
        [InlineData("MAX(3, 5)", "5")] [InlineData("MAX(5, 15, 25, 10, 20)", "25")] [InlineData("MAX([4, 5, 7, 9])", "9")] [InlineData("MAX([4, \"5\"])", "ARGUMENT_ERROR")]
        [InlineData("MAX([15, \"25\", 20])", "ARGUMENT_ERROR")] [InlineData("MAX([])", "OPERATION_ERROR")]
        [InlineData("MAX([true, 4, 5, 7, \"9\"], (e) => IF(ISNUM(e), e, 0))", "7")]
        [InlineData("MAX([ { name: \"Anna\", age: 15 }, { name: \"Billy\", age: 18 } ], (e) => e.age)", "18")]
        [InlineData("MAX([1], (e) => \"x\")", "OPERATION_ERROR")]
        [InlineData("MIN(3, 5)", "3")] [InlineData("MIN(5, 15, 25, 10, 20)", "5")] [InlineData("MIN([4, 5, 7, 9])", "4")]
        [InlineData("MIN([true, 4, 5, 7, \"9\"], (e) => IF(ISNUM(e), e, 100))", "4")]
        [InlineData("MIN([ { name: \"Anna\", age: 15 }, { name: \"Billy\", age: 18 } ], (e) => e.age)", "15")]
        [InlineData("RANGE([1, 2, 3, 4, 5], 1, 3)", "[2, 3, 4]")] [InlineData("RANGE([10, 20, 30], 0, 1)", "[10, 20]")]
        [InlineData("RANGE([10, 20, 30], 2, 1)", "INVALID_INDEX_ERROR")] [InlineData("RANGE([10, 20, 30], 0, 3)", "INVALID_INDEX_ERROR")]
        [InlineData("MAP([1, 2, 3], (e) => MUL(e, 2))", "[2, 4, 6]")] [InlineData("MAP([1, 2, 3], (e) => ADD(e, 1))", "[2, 3, 4]")]
        [InlineData("FILTER([1, 2, 3, 4], (e) => GRE(e, 2))", "[3, 4]")] [InlineData("FILTER([1, 2, 3, 4], (e) => EQU(MOD(e, 2), 0))", "[2, 4]")]
        [InlineData("FILTER([1, 2], (e) => e)", "OPERATION_ERROR")]
        // Types
        [InlineData("ISNULL(null)", "true")] [InlineData("ISNULL(5)", "false")]
        [InlineData("ISBOOL(true)", "true")] [InlineData("ISBOOL(5)", "false")]
        [InlineData("ISNUM(5)", "true")] [InlineData("ISNUM(\"5\")", "false")] [InlineData("ISNUM(true)", "false")]
        [InlineData("ISTEXT(\"text\")", "true")] [InlineData("ISTEXT(5)", "false")]
        [InlineData("ISERR(ERROR(\"Invalid input\"))", "true")] [InlineData("ISERR(5)", "false")] [InlineData("ISERR(DIV(1, 0))", "true")]
        [InlineData("ISARR([1, 2, 3])", "true")] [InlineData("ISARR(5)", "false")]
        [InlineData("ISOBJ({ name: \"Ada\" })", "true")] [InlineData("ISOBJ([1, 2, 3])", "false")]
        [InlineData("ISDEL((x) => MUL(x, 2))", "true")] [InlineData("ISDEL([1, 2, 3])", "false")] [InlineData("ISDEL(ABS)", "true")]
        // Conversion
        [InlineData("BOOL(null)", "false")] [InlineData("BOOL(true)", "true")] [InlineData("BOOL(0)", "false")] [InlineData("BOOL(25)", "true")] [InlineData("BOOL(-0.4)", "true")]
        [InlineData("BOOL(\"\")", "false")] [InlineData("BOOL(\"false\")", "true")] [InlineData("BOOL([])", "false")] [InlineData("BOOL([2, 4])", "true")]
        [InlineData("BOOL({ })", "false")] [InlineData("BOOL({ name: \"OBJECT\", empty: false })", "true")] [InlineData("BOOL(ABS)", "ARGUMENT_ERROR")]
        [InlineData("NUMBER(null)", "0")] [InlineData("NUMBER(false)", "0")] [InlineData("NUMBER(true)", "1")] [InlineData("NUMBER(-0.4)", "-0.4")]
        [InlineData("NUMBER(\"\")", "0")] [InlineData("NUMBER(\"2.15\")", "2.15")] [InlineData("NUMBER(\"2.99792458e+8\")", "299792458")] [InlineData("NUMBER(\"zero\")", "0")]
        [InlineData("TEXT(null)", "null")] [InlineData("TEXT(true)", "true")] [InlineData("TEXT(25)", "25")] [InlineData("TEXT(-12.0)", "-12")] [InlineData("TEXT(\"abc\")", "abc")]
        [InlineData("TEXT([1, 2, 3])", "[1, 2, 3]")] [InlineData("TEXT({ name: \"John\", age: 24 })", "{ name: \"John\", age: 24 }")] [InlineData("TEXT(AVG)", "AVG(args) => any")]
        [InlineData("TEXT(ERROR(\"Invalid\"))", "[CUSTOM_ERROR (1)] Invalid")]
        public void Examples(string formula, string expected) => AssertResult(formula, expected);
    }
}
