using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Primitives;

namespace FormuLang.NET.Core.Functions
{
    /// <summary>
    /// One parameter position of a function overload (docs/implementation/argument_template.md).
    /// </summary>
    public readonly record struct ArgumentTemplate : IArgumentTemplate
    {
        public required string Name { get; init; }

        public IType Type { get; init; }
        public IType? ElementType { get; init; }
        public bool AnyType { get; init; }
        public bool Multiple { get; init; }
        public bool Optional { get; init; }

        public ArgumentTemplate()
        {
            Type = Types.Undefined;
        }

        public static ArgumentTemplate Of(string name, IType type, bool optional = false, bool multiple = false)
            => new() { Name = name, Type = type, Optional = optional, Multiple = multiple };

        /// <summary>
        /// Array whose every element has the given base type, e.g. <c>numbers &lt;Array&lt;Number&gt;&gt;</c>.
        /// </summary>
        public static ArgumentTemplate ArrayOf(string name, IType elementType, bool optional = false)
            => new() { Name = name, Type = Types.Array, ElementType = elementType, Optional = optional };

        public static ArgumentTemplate Any(string name, bool optional = false, bool multiple = false)
            => new() { Name = name, AnyType = true, Optional = optional, Multiple = multiple };

        public override string ToString()
        {
            string typeStr = AnyType ? "Any"
                : ElementType != null ? $"Array<{ElementType.Base}>"
                : Type.HasExtension() ? Type.Extended!
                : Type.Base;

            string mul = Multiple ? "..." : "";
            string opt = Optional ? "?" : "";

            return $"{mul}{Name}{opt} <{typeStr}>";
        }
    }
}
