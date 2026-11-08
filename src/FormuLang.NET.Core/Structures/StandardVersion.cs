using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Core.Structures
{
    public readonly record struct StandardVersion : IStandardVersion
    {
        public uint Major { get; init; }
        public uint Minor { get; init; }
        public string Version => $"{Major}.{Minor}";

        public override string ToString() => Version;
    }
}
