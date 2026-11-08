using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Structures;
using Object = FormuLang.NET.Core.Primitives.Object;

namespace FormuLang.NET.Core.Tests
{
    public class PrimitivesTests
    {
        [Fact]
        public void Value_CanBeOverriddenBySameType()
        {
            var v = Value.Number(1, "1");
            Assert.True(v.SetValue(Value.Number(2, "2")).IsSuccess());
            Assert.Equal(2d, v.AsNumber());
            Assert.Equal("2", v.Expression);
        }

        [Fact]
        public void Value_CannotBeOverriddenByOtherType()
        {
            var v = Value.Number(1, "1");
            Assert.Equal(Errors.TypeErrorCode, v.SetValue(Value.Text("a", "\"a\"")).Code);
        }

        [Fact]
        public void Variable_TypeFollowsTheStoredValue()
        {
            var x = new Variable("x");
            Assert.True(x.Type.IsUndefined());

            Assert.True(x.SetValue(Value.Number(3, "3")).IsSuccess());
            Assert.Equal(Types.NumberName, x.Type.Base);
            Assert.Equal("3", x.Expression);

            Assert.Equal(Errors.TypeErrorCode, x.SetValue(Value.Text("a", "\"a\"")).Code);
        }

        // docs/standards/core/errors/UNDEFINED_ERROR.md
        [Fact]
        public void ReadingUndefinedVariable_ReturnsUndefinedError()
        {
            var x = new Variable("x");

            Assert.False(x.CanAccessValue);
            IValue value = x.GetValue();
            Assert.Equal(Types.ErrorName, value.Type.Base);
            Assert.Equal(Errors.UndefinedErrorCode, value.As<IError>()!.Code);
        }

        [Fact]
        public void AccessChecks_DoNotThrow()
        {
            Assert.True(Value.Null().CanAccessValue);
            Assert.True(new Variable("x", Value.Number(1, "1")).CanUpdateValue.IsSuccess());
            Assert.True(new Constant("C", Value.Number(1, "1")).CanUpdateValue.IsFailed());
        }

        [Fact]
        public void TryAs_FailsForOtherType()
        {
            var text = Value.Text("a", "\"a\"");
            Assert.False(text.TryAsNumber(out _));
            Assert.Null(text.AsNumber());
            Assert.True(text.TryAsText(out var s));
            Assert.Equal("a", s);

            Assert.True(Value.Boolean(false, "false").TryAsBoolean(out bool b));
            Assert.False(b);
        }

        [Fact]
        public void TryAsObject_ReturnsTheObject()
        {
            var obj = new Object();
            Assert.True(Value.Object(obj, "{ }").TryAsObject(out var result));
            Assert.Same(obj, result);
        }

        [Fact]
        public void TypeCategories_FollowValueSemantics()
        {
            // docs/core/values.md: Error is passed by reference
            Assert.False(Types.Error.IsSimple());
            Assert.True(Types.Error.IsComplex());
            Assert.True(Types.Delegate.IsComplex());
            Assert.True(Types.Text.IsSimple());
        }

        // docs/standards/core/semantics/serialization.md
        [Theory]
        [InlineData(64, "64")]
        [InlineData(-12.0, "-12")]
        [InlineData(128.24801540, "128.2480154")]
        [InlineData(0.05, "0.05")]
        public void Serialization_Numbers(double number, string expected)
        {
            Assert.Equal(expected, Value.Number(number, "").ToString());
        }

        [Fact]
        public void Serialization_SimpleTypes()
        {
            Assert.Equal("null", Value.Null().ToString());
            Assert.Equal("true", Value.Boolean(true, "").ToString());
            Assert.Equal("false", Value.Boolean(false, "").ToString());
            Assert.Equal("This is text value", Value.Text("This is text value", "").ToString());
            Assert.Equal("[CUSTOM_ERROR (1)] You've reached a breakpoint.", Value.Error(Errors.CustomError("You've reached a breakpoint."), "").ToString());
        }

        [Fact]
        public void Serialization_Array()
        {
            var numbers = Enumerable.Range(1, 10).Select(i => (IValue)Value.Number(i, i.ToString()));
            Assert.Equal("[1, 2, 3, 4, 5, 6, 7, 8, 9, 10]", Value.Array(numbers, "").ToString());

            var texts = new IValue[] { Value.Text("abc", ""), Value.Text("d\"f", "") };
            Assert.Equal("[\"abc\", \"d\\\"f\"]", Value.Array(texts, "").ToString());

            Assert.Equal("[]", Value.Array([], "").ToString());
        }

        [Fact]
        public void Serialization_Object()
        {
            var obj = new Object();
            obj.InitConstant("name", Value.Text("John", "\"John\""));
            obj.InitVariable("age", Value.Number(24, "24"));

            Assert.Equal("{ name: \"John\", age: 24 }", Value.Object(obj, "").ToString());
            Assert.Equal("{ name: \"John\", age: 24 }", obj.ToString());
        }

        [Fact]
        public void AbstractTree_AddAndRemove()
        {
            var tree = new AbstractTree<string>("Object", "Object");
            tree.AddNode("Geometry.Shape", "Geometry.Shape", "Object");
            tree.AddNode("Geometry.Circle", "Geometry.Circle", "Geometry.Shape");

            var circle = tree.GetNode("Geometry.Circle")!;
            Assert.True(circle.IsDescendantOf(tree.Root));
            Assert.True(tree.Root.IsAncestorOf(circle));
            Assert.True(circle.IsChildOf(tree.GetNode("Geometry.Shape")!));
            Assert.False(tree.Root.IsChildOf(circle));

            Assert.Throws<InvalidOperationException>(() => tree.AddNode("Geometry.Circle", "x", "Object"));

            Assert.False(tree.RemoveNode("Object"));
            Assert.True(tree.RemoveNode("Geometry.Shape"));
            Assert.False(tree.ContainsNode("Geometry.Circle"));
            Assert.Empty(tree.Root.Children);
        }
    }
}
