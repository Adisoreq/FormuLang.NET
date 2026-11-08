using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Abstractions.Primitives
{
    public interface IContainer : IPrimitive, INamedStructure
    {
        public ContainerType ContainerType { get; }
        public bool Mutable { get; }
    }
}