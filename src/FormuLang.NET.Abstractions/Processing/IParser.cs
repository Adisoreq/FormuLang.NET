namespace FormuLang.NET.Abstractions.Processing
{
    public interface IParser
    {
        public IParsingResult Parse(IEnumerable<IToken> tokens);
    }
}
