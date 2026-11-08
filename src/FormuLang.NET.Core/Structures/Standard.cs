using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormuLang.NET.Core.Structures
{
    public class Standard : IStandard
    {
        public required string ID { get; init; }

        public required IStandardVersion Version { get; init; }

        public required string Name { get; init; }

        public required IEnumerable<string> ExtendedTypes { get; init; }

        public required IAbstractTree<string> ExtendedTypeTree { get; init; }

        public required IEnumerable<IFunction> NamedFunctions { get; init; }
    }
}
