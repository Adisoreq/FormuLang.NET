using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Processing;

namespace FormuLang.NET.Core.Processing
{
    public sealed class ParsingResult : IParsingResult
    {
        public IReadOnlyList<Sequence> Sequences { get; }
        public IReadOnlyDictionary<string, IExpressionNode> PrimitiveMap { get; }
        public IError Error { get; }

        IReadOnlyList<ISequence> IParsingResult.Sequences => Sequences;

        public ParsingResult(IReadOnlyList<Sequence> sequences, IError error)
        {
            Sequences = sequences;
            PrimitiveMap = Sequence.BuildPrimitiveMap(sequences);
            Error = error;
        }

        public static ParsingResult Failed(IError error) => new([], error);
    }
}
