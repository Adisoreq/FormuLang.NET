using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Utils;

namespace FormuLang.NET.Core.Processing
{
    /// <summary>
    /// Validation stage required by the Core standard (docs/standards/core/semantics/validation.md):
    /// environment checks, then a single scan of the raw formula that collects every problem.
    /// </summary>
    public class Validator : IValidator
    {
        private enum Mode { Code, Text, Comment }

        private readonly record struct Position(int Line, int Column)
        {
            public override string ToString() => $"line {Line}, column {Column}";
        }

        public static ValidationResult Validate(string formula, IEnvironment environment)
        {
            var errors = new List<IError>();

            CheckEnvironment(environment, errors);
            ScanFormula(formula, errors);

            return new ValidationResult(errors);
        }

        IValidationResult IValidator.Validate(string formula, IEnvironment environment) => Validate(formula, environment);

        // Environment checks

        private static void CheckEnvironment(IEnvironment environment, List<IError> errors)
        {
            IValue? standards = Read(environment, "STANDARDS", ContainerType.Constant);
            if (standards?.Raw is not IValue[] items || items.Any(i => i.Type.Base != Types.TextName))
                errors.Add(MissingConstant("STANDARDS", "Array"));
            else if (!items.Any(i => (string?)i.Raw == Structures.Environment.CoreStandard))
                errors.Add(Errors.EnvironmentError("Environment constant \"STANDARDS\" does not contain the Core standard."));

            foreach (string name in new[] { "E", "PI", "PHI" })
            {
                if (Read(environment, name, ContainerType.Constant)?.Type.Base != Types.NumberName)
                    errors.Add(MissingConstant(name, Types.NumberName));
            }

            if (Read(environment, "NULL", ContainerType.Constant)?.Type.Base != Types.NullName)
                errors.Add(MissingConstant("NULL", Types.NullName));

            CheckBooleanConstant(environment, "TRUE", true, errors);
            CheckBooleanConstant(environment, "FALSE", false, errors);

            CheckWholeNumberVariable(environment, "array_indexing", errors);
            CheckVariable(environment, "suppress_errors", Types.BooleanName, errors);

            // Serialization variables
            CheckWholeNumberVariable(environment, SerializationOptions.DecimalPlaceName, errors);
            CheckVariable(environment, SerializationOptions.SplitArrayName, Types.BooleanName, errors);
            CheckVariable(environment, SerializationOptions.SplitObjectName, Types.BooleanName, errors);
            CheckVariable(environment, SerializationOptions.IncludeObjectClassName, Types.BooleanName, errors);
            CheckWholeNumberVariable(environment, SerializationOptions.MaxArrayPreviewName, errors);
        }

        private static void CheckBooleanConstant(IEnvironment environment, string name, bool expected, List<IError> errors)
        {
            IValue? value = Read(environment, name, ContainerType.Constant);

            if (value?.Type.Base != Types.BooleanName)
                errors.Add(MissingConstant(name, Types.BooleanName));
            else if (value.Raw is not bool actual || actual != expected)
                errors.Add(Errors.EnvironmentError($"Environment constant \"{name}\" must be {(expected ? "true" : "false")}."));
        }

        private static void CheckVariable(IEnvironment environment, string name, string type, List<IError> errors)
        {
            if (Read(environment, name, ContainerType.Variable)?.Type.Base != type)
                errors.Add(MissingVariable(name, type));
        }

        private static void CheckWholeNumberVariable(IEnvironment environment, string name, List<IError> errors)
        {
            IValue? value = Read(environment, name, ContainerType.Variable);

            if (value?.Type.Base != Types.NumberName)
                errors.Add(MissingVariable(name, Types.NumberName));
            else if (value.Raw is double d && Math.Floor(d) != d)
                errors.Add(Errors.EnvironmentError($"Environment variable \"{name}\" must be a whole number."));
        }

        private static IValue? Read(IEnvironment environment, string name, ContainerType kind)
        {
            if (!environment.HasKey(name))
                return null;

            IContainer container = environment.GetProperty(name);
            return container.ContainerType == kind && container.CanAccessValue ? container.GetValue() : null;
        }

        private static IError MissingConstant(string name, string type)
            => Errors.EnvironmentError($"Environment constant \"{name}\" is missing or is not of type {type}.");

        private static IError MissingVariable(string name, string type)
            => Errors.EnvironmentError($"Environment variable \"{name}\" is missing or is not of type {type}.");

        // Formula scan

        private static void ScanFormula(string formula, List<IError> errors)
        {
            Mode mode = Mode.Code;
            bool escape = false;
            var delimiters = new Stack<(char Symbol, Position Position)>();
            Position textStart = default;

            int line = 1, column = 0;

            for (int i = 0; i < formula.Length; i++)
            {
                char c = formula[i];
                bool lineBreak = c == '\n' || c == '\r';

                if (lineBreak && c == '\r' && i + 1 < formula.Length && formula[i + 1] == '\n')
                    i++;

                if (!lineBreak)
                    column++;

                var position = new Position(line, column);

                if (mode == Mode.Comment)
                {
                    if (lineBreak)
                    {
                        mode = Mode.Code;
                        line++;
                        column = 0;
                    }
                    continue;
                }

                if (!lineBreak && !StringUtils.IsValidSymbol(c))
                {
                    errors.Add(Errors.SyntaxError($"Invalid symbol \"{Display(c)}\" at {position}."));
                    continue;
                }

                if (lineBreak)
                {
                    if (mode == Mode.Text)
                    {
                        errors.Add(Errors.SyntaxError($"Text literal is not closed at {textStart}."));
                        mode = Mode.Code;
                        escape = false;
                    }

                    line++;
                    column = 0;
                    continue;
                }

                if (mode == Mode.Text)
                {
                    if (escape)
                    {
                        escape = false;
                        if (c != '\\' && c != '"' && c != 'n' && c != 't')
                            errors.Add(Errors.SyntaxError($"Invalid escape sequence \"\\{c}\" at {position}."));
                    }
                    else if (c == '"')
                        mode = Mode.Code;
                    else if (c == '\\')
                        escape = true;

                    continue;
                }

                // Code
                switch (c)
                {
                    case '#':
                        mode = Mode.Comment;
                        break;

                    case '"':
                        mode = Mode.Text;
                        textStart = position;
                        break;

                    case '(' or '[' or '{':
                        delimiters.Push((c, position));
                        break;

                    case ')' or ']' or '}':
                        if (delimiters.Count == 0)
                            errors.Add(Errors.SyntaxError($"Unmatched \"{c}\" at {position}."));
                        else if (delimiters.Pop().Symbol != Opening(c))
                            errors.Add(Errors.SyntaxError($"\"{c}\" does not match the opening delimiter at {position}."));
                        break;

                    case ';' when delimiters.Count > 0:
                        errors.Add(Errors.SyntaxError($"Unexpected \";\" inside a delimiter at {position}."));
                        break;
                }
            }

            if (mode == Mode.Text)
                errors.Add(Errors.SyntaxError($"Text literal is not closed at {textStart}."));

            foreach (var (symbol, position) in delimiters)
                errors.Add(Errors.SyntaxError($"Unclosed \"{symbol}\" at {position}."));
        }

        private static char Opening(char closing) => closing switch { ')' => '(', ']' => '[', _ => '{' };

        private static string Display(char c) => char.IsControl(c) ? $"U+{(int)c:X4}" : c.ToString();
    }

    public sealed class ValidationResult : IValidationResult
    {
        public IReadOnlyList<IError> Errors { get; }

        public IError Error => Errors.Count > 0 ? Errors[0] : Core.Errors.Success();

        public ValidationResult(IReadOnlyList<IError> errors)
        {
            Errors = errors;
        }
    }
}
