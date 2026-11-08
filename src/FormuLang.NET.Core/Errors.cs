using FormuLang.NET.Core.Primitives;

namespace FormuLang.NET.Core
{
    /// <summary>
    /// Errors defined by the Core standard. Codes and names follow docs/standards/core/errors/README.md of the FormuLang standard.
    /// </summary>
    public static class Errors
    {
        public const uint SuccessCode = 0;
        public const uint CustomErrorCode = 1;
        public const uint SyntaxErrorCode = 2;
        public const uint OperationErrorCode = 3;
        public const uint TypeErrorCode = 4;
        public const uint UnknownSymbolErrorCode = 5;
        public const uint ArgumentErrorCode = 6;
        public const uint InvalidIndexErrorCode = 7;
        public const uint InvalidKeyErrorCode = 8;
        public const uint InvalidIdentifierErrorCode = 9;
        public const uint EnvironmentErrorCode = 10;
        public const uint NotSupportedErrorCode = 11;
        public const uint UndefinedErrorCode = 12;

        public static Error Success(string message = "Operation completed successfully.")
            => new Error(SuccessCode, "SUCCESS", message);

        public static Error CustomError(string message = "Custom error.")
            => new Error(CustomErrorCode, "CUSTOM_ERROR", message);

        public static Error SyntaxError(string message = "Syntax error.")
            => new Error(SyntaxErrorCode, "SYNTAX_ERROR", message);

        public static Error OperationError(string message = "Operation error.")
            => new Error(OperationErrorCode, "OPERATION_ERROR", message);

        public static Error TypeError(string message = "Invalid type.")
            => new Error(TypeErrorCode, "TYPE_ERROR", message);

        public static Error UnknownSymbolError(string message = "Unknown symbol.")
            => new Error(UnknownSymbolErrorCode, "UNKNOWN_SYMBOL_ERROR", message);

        public static Error ArgumentError(string message = "Invalid argument.")
            => new Error(ArgumentErrorCode, "ARGUMENT_ERROR", message);

        public static Error InvalidIndexError(string message = "Index out of bounds.")
            => new Error(InvalidIndexErrorCode, "INVALID_INDEX_ERROR", message);

        public static Error InvalidKeyError(string message = "Invalid key.")
            => new Error(InvalidKeyErrorCode, "INVALID_KEY_ERROR", message);

        public static Error InvalidIdentifierError(string message = "Invalid identifier.")
            => new Error(InvalidIdentifierErrorCode, "INVALID_IDENTIFIER_ERROR", message);

        public static Error EnvironmentError(string message = "Environment error.")
            => new Error(EnvironmentErrorCode, "ENVIRONMENT_ERROR", message);

        public static Error NotSupportedError(string message = "Not supported.")
            => new Error(NotSupportedErrorCode, "NOT_SUPPORTED_ERROR", message);

        public static Error UndefinedError(string message = "Variable has no value.")
            => new Error(UndefinedErrorCode, "UNDEFINED_ERROR", message);
    }
}
