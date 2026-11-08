using FormuLang.NET.Core.Utils;

namespace FormuLang.NET.Core.Validation
{
    /// <summary>
    /// Identifier rules of the standard (docs/syntax/naming.md): ^[a-zA-Z_]\w*$, ASCII only.
    /// </summary>
    public static class IdentifierValidator
    {
        public static bool Validate(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;

            if (!StringUtils.IsCharIdentifierStart(identifier[0]))
                return false;

            foreach (char c in identifier)
            {
                if (!StringUtils.IsCharWordCharacter(c))
                    return false;
            }

            return true;
        }
    }
}
