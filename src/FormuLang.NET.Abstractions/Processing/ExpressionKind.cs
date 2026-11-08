namespace FormuLang.NET.Abstractions.Processing
{
    /// <summary>
    /// Classification of an expression in the primitive map (docs/semantics/parsing.md, "Primitive classification").
    /// </summary>
    public enum ExpressionKind
    {
        /// <summary>Literal value: number, text, true, false, null.</summary>
        Value,

        /// <summary>Variable with a $ (context) or @ (environment) sigil.</summary>
        Variable,

        /// <summary>Identifier without a sigil or parentheses: a constant, or a named function used as a Delegate.</summary>
        Constant,

        /// <summary>Lambda parameter inside a lambda body. Unresolved until the lambda is invoked.</summary>
        Parameter,

        /// <summary>Function or procedure call; every operator is translated into one.</summary>
        FunctionCall,

        /// <summary>Lambda expression; its body is a separate tree.</summary>
        Lambda,
    }
}
