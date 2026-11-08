namespace FormuLang.NET.Abstractions.Structures
{
    public interface IStandardVersion
    {
        public string Version { get; }
        public uint Major { get; }
        public uint Minor { get; }
    }
}
