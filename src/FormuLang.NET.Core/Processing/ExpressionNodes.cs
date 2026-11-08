using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Abstractions.Processing;
using FormuLang.NET.Core.Primitives;

namespace FormuLang.NET.Core.Processing
{
    /// <summary>
    /// Node of a reference tree. Operators are already translated into <see cref="CallNode"/>s.
    /// </summary>
    public abstract class ExpressionNode : IExpressionNode
    {
        public abstract string Key { get; }
        public abstract ExpressionKind Kind { get; }
        public int Level { get; internal set; }

        public virtual IReadOnlyList<ExpressionNode> Children => [];
        IReadOnlyList<IExpressionNode> IExpressionNode.Children => Children;

        public override string ToString() => Key;
    }

    /// <summary>Literal: number, text, <c>true</c>, <c>false</c>, <c>null</c>.</summary>
    public sealed class LiteralNode(IValue value, string key) : ExpressionNode
    {
        public IValue Value { get; } = value;
        public override string Key { get; } = key;
        public override ExpressionKind Kind => ExpressionKind.Value;
    }

    /// <summary><c>$name</c> or <c>@name</c>.</summary>
    public sealed class VariableNode(VariableScope scope, string name) : ExpressionNode
    {
        public VariableReference Variable { get; } = new(scope, name);
        public override string Key => Variable.ToString();
        public override ExpressionKind Kind => ExpressionKind.Variable;
    }

    /// <summary>Identifier without a sigil: a constant, or a named function used as a Delegate.</summary>
    public sealed class ConstantNode(string name) : ExpressionNode
    {
        public string Name { get; } = name;
        public override string Key => Name;
        public override ExpressionKind Kind => ExpressionKind.Constant;
    }

    /// <summary>Lambda parameter; resolved when the lambda is invoked.</summary>
    public sealed class ParameterNode(string name) : ExpressionNode
    {
        public string Name { get; } = name;
        public override string Key => Name;
        public override ExpressionKind Kind => ExpressionKind.Parameter;
    }

    /// <summary>Function or procedure call, e.g. <c>ADD(POW($a,2),POW($b,2))</c>.</summary>
    public sealed class CallNode(string name, IReadOnlyList<ExpressionNode> arguments) : ExpressionNode
    {
        public string Name { get; } = name;
        public override IReadOnlyList<ExpressionNode> Children { get; } = arguments;
        public override string Key { get; } = $"{name}({string.Join(",", arguments.Select(a => a.Key))})";
        public override ExpressionKind Kind => ExpressionKind.FunctionCall;
    }

    /// <summary>
    /// Lambda expression. The body is a separate tree with its own parsing queue and primitive map.
    /// </summary>
    public sealed class LambdaNode(IReadOnlyList<string> parameters, Sequence body) : ExpressionNode
    {
        public IReadOnlyList<string> Parameters { get; } = parameters;
        public Sequence Body { get; } = body;
        public IReadOnlyDictionary<string, IExpressionNode> PrimitiveMap { get; } = Sequence.BuildPrimitiveMap([body]);
        public override string Key { get; } = $"({string.Join(",", parameters)})=>{body.Root.Key}";
        public override ExpressionKind Kind => ExpressionKind.Lambda;
    }

    /// <summary>
    /// One sequence: its tree and its parsing queue (bottom-up, left to right within a level).
    /// </summary>
    public sealed class Sequence : ISequence
    {
        public ExpressionNode Root { get; }
        public IReadOnlyList<ExpressionNode> Queue { get; }

        IExpressionNode ISequence.Root => Root;
        IReadOnlyList<IExpressionNode> ISequence.Queue => Queue;

        public Sequence(ExpressionNode root)
        {
            Root = root;
            Queue = BuildQueue(root);
        }

        /// <summary>
        /// Assigns levels (root = 1) and orders the nodes from the highest level to the root.
        /// Lambda bodies are not included: they have their own queues.
        /// </summary>
        private static List<ExpressionNode> BuildQueue(ExpressionNode root)
        {
            var levels = new List<List<ExpressionNode>>();
            var current = new List<ExpressionNode> { root };
            int level = 1;

            while (current.Count > 0)
            {
                foreach (var node in current)
                    node.Level = level;

                levels.Add(current);
                current = current.SelectMany(n => n.Children).ToList();
                level++;
            }

            levels.Reverse();
            return levels.SelectMany(l => l).ToList();
        }

        public static Dictionary<string, IExpressionNode> BuildPrimitiveMap(IEnumerable<Sequence> sequences)
        {
            var map = new Dictionary<string, IExpressionNode>(StringComparer.Ordinal);

            foreach (var sequence in sequences)
                foreach (var node in sequence.Queue)
                    map.TryAdd(node.Key, node);

            return map;
        }
    }
}
