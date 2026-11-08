using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Primitives;

namespace FormuLang.NET.Core.Utils
{
    /// <summary>
    /// Argument collection templates (docs/implementation/argument_collection_template.md).
    /// </summary>
    internal static class ArgumentUtils
    {
        public static string PrintCollection(IEnumerable<IArgumentTemplate> args)
        {
            return string.Join(", ", args);
        }

        /// <summary>
        /// Only the last template can be multiple, and optional templates must follow the required ones.
        /// </summary>
        public static IError ValidateCollection(IEnumerable<IArgumentTemplate> args)
        {
            IArgumentTemplate[] templates = args.ToArray();

            for (int i = 0; i < templates.Length; i++)
            {
                IArgumentTemplate arg = templates[i];
                bool isLast = i == templates.Length - 1;

                if (arg.Multiple && !isLast)
                    return Errors.ArgumentError("Only the last argument template can be multiple.");

                if (i > 0 && templates[i - 1].Optional && !arg.Optional)
                    return Errors.ArgumentError("All required argument templates must stand before the optional ones.");
            }

            return Errors.Success();
        }

        public static bool MatchTemplateType(IValue value, IArgumentTemplate template, IEnvironment environment)
        {
            if (template.AnyType)
                return true;

            if (template.ElementType != null)
            {
                return value.Raw is IValue[] elements
                    && value.Type.Base == Types.ArrayName
                    && elements.All(e => e.Type.Base == template.ElementType.Base);
            }

            IType valueType = value.Type;
            IType templateType = template.Type;

            // Template without an extended type: base types must match.
            // Every extended type derives from Object, so it matches an Object template.
            if (!templateType.HasExtension())
                return valueType.Base == templateType.Base;

            // Template with an extended type: the value's extended type must be the same type or derive from it.
            if (!valueType.HasExtension() || valueType.Base != templateType.Base)
                return false;

            if (valueType.Extended == templateType.Extended)
                return true;

            IAbstractTreeNode<string>? vt = environment.ExtendedTypeTree.GetNode(valueType.Extended!);
            IAbstractTreeNode<string>? tt = environment.ExtendedTypeTree.GetNode(templateType.Extended!);

            return vt != null && tt != null && vt.IsDescendantOf(tt);
        }

        /// <summary>
        /// Matches arguments against templates sequentially, left to right. Fails if a required argument is missing,
        /// an argument's type does not satisfy its template, or there are more arguments than the templates accept.
        /// </summary>
        public static bool MatchCollection(IEnumerable<IValue> args, IEnumerable<IArgumentTemplate> template, IEnvironment environment)
        {
            IValue[] values = args.ToArray();
            IArgumentTemplate[] templates = template.ToArray();

            int a = 0;

            foreach (IArgumentTemplate temp in templates)
            {
                if (temp.Multiple)
                {
                    int consumed = 0;

                    for (; a < values.Length; a++, consumed++)
                    {
                        if (!MatchTemplateType(values[a], temp, environment))
                            return false;
                    }

                    if (consumed == 0 && !temp.Optional)
                        return false;

                    continue;
                }

                if (a >= values.Length)
                {
                    if (!temp.Optional)
                        return false;

                    continue;
                }

                if (!MatchTemplateType(values[a], temp, environment))
                    return false;

                a++;
            }

            // Every argument must be consumed
            return a == values.Length;
        }
    }
}
