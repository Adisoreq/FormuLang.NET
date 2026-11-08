using FormuLang.NET.Abstractions.Functions;
using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Abstractions.Structures;
using FormuLang.NET.Core.Functions;
using FormuLang.NET.Core.Primitives;

namespace FormuLang.NET.Core.Processing
{
    /// <summary>
    /// Evaluation stage (docs/semantics/evaluation.md): resolves every sequence by traversing its parsing queue bottom-up,
    /// and stops the execution when the root of a sequence returns an Error other than <c>SUCCESS</c>.
    /// </summary>
    public class Evaluator : IEvaluator
    {
        public EvaluationResult Evaluate(ParsingResult formula, IContext context)
        {
            IValue last = Value.Error(Errors.Success(), "");

            foreach (Sequence sequence in formula.Sequences)
            {
                IValue result = EvaluateSequence(sequence, new Scope(context, null, null), isFormulaSequence: true);

                if (result.Type.Base == Types.ErrorName && result.As<IError>()!.IsFailed())
                {
                    if (!SuppressErrors(context))
                        return new EvaluationResult(result, result.As<IError>()!);

                    result = Value.Null();
                }

                last = result;
            }

            return new EvaluationResult(last, Errors.Success());
        }

        IEvaluationResult IEvaluator.Evaluate(IParsingResult formula, IContext context)
        {
            if (formula is not ParsingResult parsed)
                throw new ArgumentException($"{nameof(Evaluator)} evaluates only the result of {nameof(Parser)}.", nameof(formula));

            return Evaluate(parsed, context);
        }

        private static bool SuppressErrors(IContext context)
            => context.EnvironmentVariables.HasKey("suppress_errors")
               && context.EnvironmentVariables.GetProperty("suppress_errors").GetValue().Raw is true;

        /// <summary>
        /// Lambda parameters bound by one invocation, chained to the scope the lambda was created in.
        /// </summary>
        internal sealed class Scope(IContext context, IReadOnlyDictionary<string, IValue>? parameters, Scope? parent)
        {
            public IContext Context { get; } = context;

            public IValue? Lookup(string name)
                => parameters != null && parameters.TryGetValue(name, out IValue? value) ? value : parent?.Lookup(name);
        }

        /// <summary>
        /// Traverses the parsing queue: every node is resolved after all of its children.
        /// </summary>
        internal IValue EvaluateSequence(Sequence sequence, Scope scope, bool isFormulaSequence)
        {
            var results = new Dictionary<ExpressionNode, IValue>(ReferenceEqualityComparer.Instance);

            foreach (ExpressionNode node in sequence.Queue)
            {
                bool declaration = isFormulaSequence && node == sequence.Root;
                results[node] = EvaluateNode(node, results, scope, declaration);
            }

            return results[sequence.Root];
        }

        private IValue EvaluateNode(ExpressionNode node, Dictionary<ExpressionNode, IValue> results, Scope scope, bool isRoot)
        {
            switch (node)
            {
                case LiteralNode literal:
                    return literal.Value;

                case VariableNode variable:
                    return isRoot && variable.Variable.Scope == VariableScope.Context
                        ? ReadOrDeclare(variable.Variable.Name, scope.Context)
                        : ReadVariable(variable.Variable, scope.Context);

                case ConstantNode constant:
                    return ReadConstant(constant.Name, scope.Context);

                case ParameterNode parameter:
                    return scope.Lookup(parameter.Name)
                        ?? Value.Error(Errors.UnknownSymbolError($"Lambda parameter \"{parameter.Name}\" has no value."), parameter.Name);

                case LambdaNode lambda:
                    return Value.Delegate(new Lambda(lambda, this, scope), lambda.Key);

                case CallNode call:
                    return Call(call, results, scope.Context);

                default:
                    throw new NotSupportedException($"Unsupported expression node {node.GetType().Name}.");
            }
        }

        private static IValue Call(CallNode call, Dictionary<ExpressionNode, IValue> results, IContext context)
        {
            IFunction? function = context.Environment.GetFunction(call.Name);

            if (function == null)
                return Value.Error(Errors.UnknownSymbolError($"Function \"{call.Name}\" is not defined."), call.Key);

            var arguments = new IValue[call.Children.Count];

            for (int i = 0; i < arguments.Length; i++)
            {
                ExpressionNode child = call.Children[i];

                // The variable argument of SET, INIT and COND_SET is the variable itself, not its value
                arguments[i] = i == 0 && function is Procedure { TakesVariable: true } && child is VariableNode variable
                    ? variable.Variable.ToValue()
                    : results[child];
            }

            return function.Invoke(arguments, context);
        }

        // Reading variables (docs/semantics/evaluation.md, "Direct extraction")

        /// <summary>
        /// A context variable at the root of a sequence is declared if it does not exist.
        /// </summary>
        private static IValue ReadOrDeclare(string name, IContext context)
        {
            if (context.HasKey(name))
                return context.GetProperty(name).GetValue();

            if (context.Environment.HasKey(name) || context.EnvironmentVariables.HasKey(name))
                return Value.Error(Errors.InvalidIdentifierError($"\"{name}\" symbol is already defined by environment."), "$" + name);

            context.InitVariable(name, Value.Undefined("$" + name));
            return Value.Error(Errors.Success(), "$" + name);
        }

        private static IValue ReadVariable(VariableReference variable, IContext context)
        {
            if (variable.Scope == VariableScope.Environment)
            {
                return context.EnvironmentVariables.HasKey(variable.Name)
                    ? context.EnvironmentVariables.GetProperty(variable.Name).GetValue()
                    : Value.Error(Errors.EnvironmentError($"Environment variable \"{variable.Name}\" is not defined."), variable.ToString());
            }

            return context.HasKey(variable.Name)
                ? context.GetProperty(variable.Name).GetValue()
                : Value.Error(Errors.UnknownSymbolError($"Variable \"{variable.Name}\" is not defined."), variable.ToString());
        }

        /// <summary>
        /// An identifier without a sigil: a context or environment constant, or a named function as a Delegate.
        /// </summary>
        private static IValue ReadConstant(string name, IContext context)
        {
            if (context.HasKey(name) && context.GetProperty(name).ContainerType == ContainerType.Constant)
                return context.GetProperty(name).GetValue();

            if (context.Environment.HasKey(name) && context.Environment.GetProperty(name).ContainerType == ContainerType.Constant)
                return context.Environment.GetProperty(name).GetValue();

            if (context.Environment.GetFunction(name) is IFunction function)
                return Value.Delegate(function, name);

            return Value.Error(Errors.UnknownSymbolError($"Constant \"{name}\" is not defined."), name);
        }
    }

    /// <summary>
    /// Anonymous function created by a lambda expression. Invoking it evaluates the stored body tree
    /// with the arguments bound to the parameters; the body is never parsed again.
    /// </summary>
    public sealed class Lambda : Primitive, ILambda
    {
        private readonly LambdaNode _node;
        private readonly Evaluator _evaluator;
        private readonly Evaluator.Scope _scope;

        public string Name => _node.Key;
        public FunctionType FunctionType => FunctionType.Lambda;
        public IEnumerable<IFunctionOverload> Overloads => [];
        public IReadOnlyList<string> Parameters => _node.Parameters;

        internal Lambda(LambdaNode node, Evaluator evaluator, Evaluator.Scope scope)
            : base(PrimitiveType.Function, Types.Delegate, node.Key)
        {
            _node = node;
            _evaluator = evaluator;
            _scope = scope;
        }

        public override bool CanAccessValue => true;
        public override IError CanUpdateValue => Errors.OperationError("A lambda function cannot be changed.");
        public override IValue GetValue() => Value.Delegate(this, Name);
        public override IError SetValue(IValue value) => CanUpdateValue;

        /// <summary>
        /// docs/semantics/evaluation.md, "Lambda invocation": no argument matching, only the count is checked.
        /// </summary>
        public IValue Invoke(IEnumerable<IValue> parameters, IContext context)
        {
            IValue[] args = parameters.ToArray();

            if (args.Length != Parameters.Count)
                return Value.Error(Errors.ArgumentError(
                    $"The lambda expects {Parameters.Count} argument(s), but {args.Length} were given."), Name);

            var bindings = new Dictionary<string, IValue>(StringComparer.Ordinal);
            for (int i = 0; i < args.Length; i++)
                bindings[Parameters[i]] = args[i];

            return _evaluator.EvaluateSequence(_node.Body, new Evaluator.Scope(_scope.Context, bindings, _scope), isFormulaSequence: false);
        }

        public override string ToString() => $"({string.Join(", ", Parameters)}) => any";
    }

    public sealed class EvaluationResult(IValue value, IError error) : IEvaluationResult
    {
        public IValue Value { get; } = value;
        public IError Error { get; } = error;
    }
}
