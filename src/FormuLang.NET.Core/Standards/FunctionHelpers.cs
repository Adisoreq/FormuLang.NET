using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Functions;
using FormuLang.NET.Core.Primitives;
using FormuLang.NET.Core.Utils;

namespace FormuLang.NET.Core.Standards
{
    /// <summary>
    /// Short helpers for defining built-in functions.
    /// </summary>
    internal static class FunctionHelpers
    {
        // Definitions

        public static Function Fn(string name, params FunctionOverload[] overloads) => new(name, overloads);

        public static FunctionOverload Overload(FunctionImplementation implementation, params ArgumentTemplate[] template)
            => new(template.Cast<IArgumentTemplate>(), implementation);

        public static ArgumentTemplate NumberArg(string name) => ArgumentTemplate.Of(name, Types.Number);
        public static ArgumentTemplate BooleanArg(string name) => ArgumentTemplate.Of(name, Types.Boolean);
        public static ArgumentTemplate TextArg(string name, bool optional = false) => ArgumentTemplate.Of(name, Types.Text, optional);
        public static ArgumentTemplate ArrayArg(string name) => ArgumentTemplate.Of(name, Types.Array);
        public static ArgumentTemplate NumberArrayArg(string name) => ArgumentTemplate.ArrayOf(name, Types.Number);
        public static ArgumentTemplate ObjectArg(string name) => ArgumentTemplate.Of(name, Types.Object);
        public static ArgumentTemplate DelegateArg(string name) => ArgumentTemplate.Of(name, Types.Delegate);
        public static ArgumentTemplate AnyArg(string name) => ArgumentTemplate.Any(name);

        // Argument access

        public static double Num(IValue value) => (double)value.Raw!;
        public static bool Bool(IValue value) => (bool)value.Raw!;
        public static string Txt(IValue value) => (string)value.Raw!;
        public static IValue[] Arr(IValue value) => (IValue[])value.Raw!;
        public static IObject Obj(IValue value) => (IObject)value.Raw!;
        public static IFunction Del(IValue value) => (IFunction)value.Raw!;

        public static bool IsWhole(double value) => double.IsFinite(value) && Math.Floor(value) == value;

        public static bool IsError(IValue value) => value.Type.Base == Types.ErrorName;

        // Results

        /// <summary>
        /// Number result, or <c>OPERATION_ERROR</c> if it is not finite.
        /// </summary>
        public static IValue N(double value)
        {
            if (!double.IsFinite(value))
                return OperationError("The result is not a finite number.");

            // Avoid -0, which is not a FormuLang value
            if (value == 0)
                value = 0;

            return Value.Number(value, StringConverter.NumberToString(value));
        }

        public static IValue B(bool value) => Value.Boolean(value, value ? "true" : "false");

        public static IValue T(string value) => Value.Text(value, StringConverter.TextToLiteral(value));

        public static IValue A(IEnumerable<IValue> values) => Value.Array(values, "");

        public static IValue Success() => Value.Error(Errors.Success(), "");

        public static IValue ErrorValue(IError error) => Value.Error(error, "");

        public static IValue OperationError(string message) => Value.Error(Errors.OperationError(message), "");

        public static IValue ArgumentError(string message) => Value.Error(Errors.ArgumentError(message), "");

        public static IValue InvalidIndexError(string message) => Value.Error(Errors.InvalidIndexError(message), "");

        // Delegates

        /// <summary>
        /// A handler of MAP, FILTER, MAX and MIN must accept exactly one argument.
        /// </summary>
        public static IValue? CheckHandler(IFunction handler, string functionName)
        {
            if (handler is ILambda lambda && lambda.Parameters.Count != 1)
                return ArgumentError($"The lambda passed to \"{functionName}\" must have exactly 1 parameter.");

            return null;
        }

        public static IValue Call(IFunction function, IValue argument, IContext context)
            => function.Invoke([argument], context);

        // Environment

        /// <summary>
        /// Value of the <c>array_indexing</c> environment variable in the current execution.
        /// </summary>
        public static double ArrayIndexing(IContext context)
        {
            IObject variables = context.EnvironmentVariables;

            if (variables.HasKey("array_indexing") && variables.GetProperty("array_indexing").GetValue().Raw is double indexing)
                return indexing;

            return 0;
        }

        /// <summary>
        /// Converts a FormuLang index to a 0-based index, or returns <c>INVALID_INDEX_ERROR</c>.
        /// </summary>
        public static bool TryResolveIndex(double index, int length, IContext context, out int resolved, out IValue? error)
        {
            double zeroBased = index - ArrayIndexing(context);
            resolved = -1;
            error = null;

            if (!IsWhole(zeroBased) || zeroBased < 0 || zeroBased >= length)
            {
                error = InvalidIndexError($"Index {StringConverter.NumberToString(index)} is not a valid index of the array.");
                return false;
            }

            resolved = (int)zeroBased;
            return true;
        }
    }
}
