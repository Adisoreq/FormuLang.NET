using FormuLang.NET.Abstractions.Primitives;

namespace FormuLang.NET.Core.Primitives
{
    public class Constant : Container
    {
        public Constant(string name, IValue value) :
            base(name, value, ContainerType.Constant, false) { }

        /// <summary>
        /// Constants cannot be changed. The Core standard reports an attempt as <c>ENVIRONMENT_ERROR</c>
        /// (docs/standards/core/procedures/SET.md: "Environment constants are not variables and cannot be changed").
        /// </summary>
        public override IError CanUpdateValue => Errors.EnvironmentError($"'{Name}' is a constant and cannot be changed.");

        public override IError SetValue(IValue value) => CanUpdateValue;
    }
}
