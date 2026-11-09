using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Core.Structures
{
    /// <summary>
    /// Data of formula executions (docs/core/context.md). Stores the context ($) variables, the context constants and
    /// the copies of the environment (@) variables.
    /// </summary>
    /// <remarks>
    /// The host application chooses the context type (docs/core/context.md, "Context types"). This class is the Core
    /// <b>Default Context</b>, which has no constants of its own. A context type that provides its own data derives from
    /// it and loads that data in <see cref="LoadContextData"/>, e.g. <see cref="IterationContext"/>.
    /// </remarks>
    public class Context : Primitives.Object, IContext
    {
        private readonly Primitives.Object _environmentVariables = new();
        private bool _contextDataLoaded;

        public IEnvironment Environment { get; }

        public IObject EnvironmentVariables => _environmentVariables;

        /// <summary>
        /// Context kind: disposable or persistent (docs/core/context.md, "Context kinds").
        /// </summary>
        public ContextType ContextType { get; }

        protected Context(IEnvironment environment, ContextType contextType)
        {
            Environment = environment;
            ContextType = contextType;
        }

        /// <summary>
        /// Creates a context and runs the pre-execution.
        /// </summary>
        /// <param name="contextType">Context kind: disposable for a single execution; persistent to keep the variables between executions.</param>
        public static Context Create(IEnvironment environment, ContextType contextType = ContextType.Disposable)
        {
            var context = new Context(environment, contextType);
            context.PreExecute();
            return context;
        }

        /// <summary>
        /// Pre-execution (docs/semantics/pre-execution.md): loads the data of the context type (once), then copies the
        /// environment variables that the context does not contain yet, so the state of a reused persistent context is
        /// never overwritten. <see cref="Processing.Executor"/> calls it before every execution.
        /// </summary>
        public void PreExecute()
        {
            if (!_contextDataLoaded)
            {
                _contextDataLoaded = true;
                LoadContextData();
            }

            foreach (string key in Environment.Keys)
            {
                IContainer container = Environment.GetProperty(key);

                if (container.ContainerType == ContainerType.Variable && !_environmentVariables.HasKey(key))
                    _environmentVariables.InitVariable(key, container.GetValue());
            }
        }

        /// <summary>
        /// Pre-execution step 2: loads the constants and parameters required by the context type, e.g. with
        /// <see cref="Primitives.Object.SetConstant"/>. Called once, before the first execution in this context.
        /// The basic Core context has no data of its own.
        /// </summary>
        protected virtual void LoadContextData() { }

        /// <summary>
        /// Removes every context variable and every copy of an environment variable; context constants are kept.
        /// The next <see cref="PreExecute"/> copies the environment variables again.
        /// </summary>
        protected void ClearVariables()
        {
            foreach (string key in Keys.Where(k => GetProperty(k).ContainerType == ContainerType.Variable).ToArray())
                RemoveProperty(key);

            foreach (string key in _environmentVariables.Keys.ToArray())
                _environmentVariables.RemoveProperty(key);
        }

        /// <summary>
        /// Reads a copied environment variable, e.g. <c>array_indexing</c>.
        /// </summary>
        public IValue? GetEnvironmentVariable(string name)
            => _environmentVariables.HasKey(name) ? _environmentVariables.GetProperty(name).GetValue() : null;
    }
}
