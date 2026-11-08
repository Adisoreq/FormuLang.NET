using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Core.Primitives
{
    internal readonly record struct Type : IType
    {
        public readonly string Base { get; init; } = Types.UndefinedName;
        public readonly string? Extended { get; init; } = null;

        public Type(string baseType, string? extendedType)
        {
            this.Base = baseType;
            this.Extended = extendedType;
        }

        public bool IsNull() => Base == Types.NullName;
        public bool IsUndefined() => Base == Types.UndefinedName;

        /// <summary>
        /// Basic (pass-by-value) types: Null, Undefined, Boolean, Number, Text (docs/core/values.md).
        /// </summary>
        public bool IsSimple()
        {
            switch (Base)
            {
                case Types.NullName:
                case Types.UndefinedName:
                case Types.BooleanName:
                case Types.NumberName:
                case Types.TextName:
                    return true;

                default: return false;
            }
        }

        /// <summary>
        /// Complex (pass-by-reference) types: Error, Array, Object, Delegate (docs/core/values.md).
        /// </summary>
        public bool IsComplex()
        {
            switch (Base)
            {
                case Types.ErrorName:
                case Types.ArrayName:
                case Types.ObjectName:
                case Types.DelegateName:
                    return true;

                default: return false;
            }
        }

        public bool HasExtension() => Extended != null;

        public override string ToString()
        {
            return HasExtension() ? $"{Base}<{Extended}>" : Base;
        }

        public static bool CompareBaseOnly(IType a, IType b) => a.Base == b.Base;

        public static bool CompareBaseAndExtended(IType a, IType b) => a.Base == b.Base && a.Extended == b.Extended;
    }

    /// <summary>
    /// Base types of the Core standard (docs/standards/core/types/README.md).
    /// </summary>
    public static class Types
    {
        public const string NullName = "Null";
        public const string UndefinedName = "Undefined";
        public const string BooleanName = "Boolean";
        public const string NumberName = "Number";
        public const string TextName = "Text";
        public const string ErrorName = "Error";
        public const string ArrayName = "Array";
        public const string ObjectName = "Object";
        public const string DelegateName = "Delegate";

        /// <summary>
        /// Not a value type of the standard: marks the variable argument of SET, INIT and COND_SET,
        /// which receive the variable itself instead of its value.
        /// </summary>
        public const string VariableName = "Variable";

        public static IType Null { get; } = new Type(NullName, null);
        public static IType Undefined { get; } = new Type(UndefinedName, null);
        public static IType Boolean { get; } = new Type(BooleanName, null);
        public static IType Number { get; } = new Type(NumberName, null);
        public static IType Text { get; } = new Type(TextName, null);
        public static IType Error { get; } = new Type(ErrorName, null);
        public static IType Array { get; } = new Type(ArrayName, null);
        public static IType Object { get; } = new Type(ObjectName, null);
        public static IType Delegate { get; } = new Type(DelegateName, null);
        public static IType Variable { get; } = new Type(VariableName, null);

        /// <summary>
        /// Creates a type, optionally with an extended type (e.g. <c>Of("Object", "Stock.Candle")</c>).
        /// </summary>
        public static IType Of(string baseType, string? extendedType = null) => new Type(baseType, extendedType);

        /// <summary>
        /// Two types are equal when both the base and the extended type are equal.
        /// </summary>
        public static bool AreEqual(IType a, IType b) => Type.CompareBaseAndExtended(a, b);
    }
}
