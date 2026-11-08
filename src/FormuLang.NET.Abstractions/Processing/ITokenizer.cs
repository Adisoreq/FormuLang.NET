using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormuLang.NET.Abstractions.Processing
{
    public interface ITokenizer
    {
        public ITokenizationResult Tokenize(string expression);
    }
}
