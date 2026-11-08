namespace FormuLang.NET.Abstractions.Structures
{
    /// <summary>
    /// docs/core/context.md, "Context types"
    /// </summary>
    public enum ContextType
    {
        /// <summary>Created for a single execution and disposed after it.</summary>
        Disposable,

        /// <summary>Kept by the host application and reused across executions.</summary>
        Persistent,
    }
}
