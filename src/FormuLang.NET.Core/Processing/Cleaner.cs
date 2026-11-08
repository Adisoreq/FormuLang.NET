using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Core.Utils;
using System.Text;

namespace FormuLang.NET.Core.Processing
{
    /// <summary>
    /// Clean-up stage (docs/semantics/cleanup.md): normalization, comment removal,
    /// white space compression and line compression.
    /// </summary>
    public class Cleaner : ICleaner
    {
        protected const char TextDelimiter = '"';
        protected const char EscapeSymbol = '\\';
        protected const char CommentSigil = '#';

        /// <summary>
        /// Part of a line: either a text literal (with its quotation marks) or code outside text literals.
        /// </summary>
        private readonly record struct Segment(string Content, bool IsText);

        /// <summary>
        /// Splits a line into text literals and code, and drops the comment.
        /// A text literal ends at the next quotation mark not preceded by an escaping backslash, or at the end of the line.
        /// </summary>
        private static List<Segment> SplitLine(string line)
        {
            var segments = new List<Segment>();
            var current = new StringBuilder();

            bool insideText = false;
            bool escape = false;

            foreach (char c in line)
            {
                if (insideText)
                {
                    current.Append(c);

                    if (escape)
                        escape = false;
                    else if (c == EscapeSymbol)
                        escape = true;
                    else if (c == TextDelimiter)
                    {
                        segments.Add(new Segment(current.ToString(), true));
                        current.Clear();
                        insideText = false;
                    }

                    continue;
                }

                if (c == CommentSigil)
                    break;

                if (c == TextDelimiter)
                {
                    if (current.Length > 0)
                        segments.Add(new Segment(current.ToString(), false));

                    current.Clear();
                    current.Append(c);
                    insideText = true;
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
                segments.Add(new Segment(current.ToString(), insideText));

            return segments;
        }

        private static bool IsCompressibleOperator(char c)
            => StringUtils.IsCharOperator(c) && c != TextDelimiter;

        /// <summary>
        /// Compresses white spaces in code outside text literals:
        /// multiple spaces become one, and spaces next to an operator are removed.
        /// </summary>
        private static string CompressCode(string code)
        {
            var result = new StringBuilder(code.Length);

            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];

                if (c != ' ')
                {
                    result.Append(c);
                    continue;
                }

                // Multi-space: keep only the last space of a run
                if (i + 1 < code.Length && code[i + 1] == ' ')
                    continue;

                char? before = result.Length > 0 ? result[^1] : null;
                char? after = i + 1 < code.Length ? code[i + 1] : null;

                // Operator spaces
                if ((before.HasValue && IsCompressibleOperator(before.Value)) ||
                    (after.HasValue && IsCompressibleOperator(after.Value)))
                    continue;

                result.Append(c);
            }

            return result.ToString();
        }

        private static string CleanLine(string line)
        {
            List<Segment> segments = SplitLine(line);
            var result = new StringBuilder(line.Length);

            for (int i = 0; i < segments.Count; i++)
            {
                Segment segment = segments[i];

                if (segment.IsText)
                {
                    result.Append(segment.Content);
                    continue;
                }

                string code = CompressCode(segment.Content);

                // Trim spaces at the start and end of the line (never inside a text literal)
                if (i == 0)
                    code = code.TrimStart(' ');
                if (i == segments.Count - 1)
                    code = code.TrimEnd(' ');

                result.Append(code);
            }

            return result.ToString();
        }

        public static CleanUpResult CleanUp(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return new CleanUpResult(string.Empty, Errors.OperationError("Input string is empty."));

            // 1. Normalization
            string normalized = expression
                .Replace('\t', ' ')
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

            // 2. Comments and 3. white spaces, line by line
            IEnumerable<string> lines = normalized
                .Split('\n')
                .Select(CleanLine)
                // 4. Line compression
                .Where(line => line.Length > 0);

            return new CleanUpResult(string.Join('\n', lines), Errors.Success());
        }

        ICleanUpResult ICleaner.CleanUp(string expression) => CleanUp(expression);
    }
}
