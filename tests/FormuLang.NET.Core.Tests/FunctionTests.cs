using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Functions;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Structures;
using Env = FormuLang.NET.Core.Structures.Environment;
using Object = FormuLang.NET.Core.Primitives.Object;

namespace FormuLang.NET.Core.Tests
{
    /// <summary>
    /// docs/implementation/argument_collection_template.md, docs/core/functions.md
    /// </summary>
    public class FunctionTests
    {
        private static readonly IContext Environment = Context.Create(Env.Create());

        private static IValue N(double n) => Value.Number(n, n.ToString());
        private static IValue T(string t) => Value.Text(t, $"\"{t}\"");

        private static double Max(IEnumerable<IValue> values) => values.Max(v => v.As<double>());

        /// <summary>
        /// MAX overloads from the "Matching overloads" example (element types of arrays are not modelled yet).
        /// </summary>
        private static Function CreateMax() => new("MAX",
        [
            new FunctionOverload(
                [ArgumentTemplate.Of("a", Types.Number), ArgumentTemplate.Of("b", Types.Number)],
                (args, _) => Value.Number(Max(args), "")),
            new FunctionOverload(
                [ArgumentTemplate.Of("numbers", Types.Number, multiple: true)],
                (args, _) => Value.Number(Max(args), "")),
            new FunctionOverload(
                [ArgumentTemplate.Of("numbers", Types.Array)],
                (args, _) => Value.Number(Max(args[0].As<IValue[]>()!), "")),
        ]);

        [Fact]
        public void FirstMatchingOverload_IsUsed()
        {
            var max = CreateMax();

            Assert.Equal(5d, max.Invoke([N(3), N(5)], Environment).As<double>());
            Assert.Equal(25d, max.Invoke([N(5), N(15), N(25), N(10), N(20)], Environment).As<double>());
            Assert.Equal(25d, max.Invoke([Value.Array([N(15), N(25), N(20)], "")], Environment).As<double>());
        }

        [Fact]
        public void WrongArgumentType_DoesNotMatch()
        {
            var abs = new Function("ABS",
            [
                new FunctionOverload([ArgumentTemplate.Of("num", Types.Number)], (args, _) => Value.Number(Math.Abs(args[0].As<double>()), "")),
            ]);

            Assert.Equal(Errors.ArgumentErrorCode, abs.Invoke([T("x"), N(5)], Environment).As<IError>()!.Code);
            Assert.Equal(Errors.ArgumentErrorCode, abs.Invoke([N(5), T("x")], Environment).As<IError>()!.Code);
            Assert.Equal(Errors.ArgumentErrorCode, abs.Invoke([], Environment).As<IError>()!.Code);
            Assert.Equal(5d, abs.Invoke([N(-5)], Environment).As<double>());
        }

        [Fact]
        public void NoMatch_PropagatesLeftmostErrorArgument()
        {
            var max = CreateMax();
            var first = Value.Error(Errors.CustomError("first"), "");
            var second = Value.Error(Errors.CustomError("second"), "");

            Assert.Same(first, max.Invoke([N(1), first, second], Environment));
        }

        // ARRAY(...elements? <Any>)
        [Fact]
        public void OptionalMultipleTemplate_AcceptsNoArguments()
        {
            var overload = new FunctionOverload([ArgumentTemplate.Any("elements", optional: true, multiple: true)], (args, _) => Value.Array(args, ""));

            Assert.True(overload.Matches([], Environment));
            Assert.True(overload.Matches([N(3), T("a")], Environment));
        }

        [Fact]
        public void RequiredMultipleTemplate_NeedsOneArgument()
        {
            var overload = new FunctionOverload([ArgumentTemplate.Of("numbers", Types.Number, multiple: true)], (_, _) => Value.Null());

            Assert.False(overload.Matches([], Environment));
            Assert.False(overload.Matches([N(1), T("a")], Environment));
        }

        // JOIN(array <Array>, separator? <Text>)
        [Fact]
        public void OptionalArgument()
        {
            var overload = new FunctionOverload(
                [ArgumentTemplate.Of("array", Types.Array), ArgumentTemplate.Of("separator", Types.Text, optional: true)],
                (_, _) => Value.Null());

            var array = Value.Array([N(1)], "");
            Assert.True(overload.Matches([array], Environment));
            Assert.True(overload.Matches([array, T("-")], Environment));
            Assert.False(overload.Matches([array, T("-"), T("x")], Environment));
        }

        [Fact]
        public void InvalidTemplates_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new FunctionOverload(
                [ArgumentTemplate.Of("a", Types.Number, multiple: true), ArgumentTemplate.Of("b", Types.Number)], (_, _) => Value.Null()));

            Assert.Throws<ArgumentException>(() => new FunctionOverload(
                [ArgumentTemplate.Of("a", Types.Number, optional: true), ArgumentTemplate.Of("b", Types.Number)], (_, _) => Value.Null()));
        }

        // ADD(a <Geometry.Vector2D>, b <Geometry.Vector2D>), docs/semantics/inheritance.md
        [Fact]
        public void ExtendedTypes_MatchThemselvesTheirParentsAndObject()
        {
            var env = Env.Create();
            env.RegisterExtendedType("Geometry.Shape");
            env.RegisterExtendedType("Geometry.Circle", "Geometry.Shape");
            var ctx = Context.Create(env);

            var circle = Value.Object(new Object(), "Geometry.Circle", "");
            var plain = Value.Object(new Object(), "");

            var shapeOverload = new FunctionOverload([ArgumentTemplate.Of("s", Types.Of("Object", "Geometry.Shape"))], (_, _) => Value.Null());
            var objectOverload = new FunctionOverload([ArgumentTemplate.Of("o", Types.Object)], (_, _) => Value.Null());
            var circleOverload = new FunctionOverload([ArgumentTemplate.Of("c", Types.Of("Object", "Geometry.Circle"))], (_, _) => Value.Null());

            Assert.True(shapeOverload.Matches([circle], ctx));
            Assert.True(objectOverload.Matches([circle], ctx));
            Assert.True(circleOverload.Matches([circle], ctx));
            Assert.False(shapeOverload.Matches([plain], ctx));
            Assert.False(shapeOverload.Matches([N(1)], ctx));
        }

        [Fact]
        public void Function_IsADelegateValue()
        {
            var max = CreateMax();
            Assert.Equal(Types.DelegateName, max.GetValue().Type.Base);
            Assert.Equal("MAX(args) => any", max.GetValue().ToString());
        }
    }
}
