using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Core.Structures
{
    public class AbstractTree<T> : IAbstractTree<T>
    {
        private readonly Dictionary<string, AbstractTreeNode<T>> _nodes = new();

        public AbstractTreeNode<T> Root { get; }

        IAbstractTreeNode<T> IAbstractTree<T>.Root => Root;

        public AbstractTree(string rootKey, T rootValue)
        {
            Root = new AbstractTreeNode<T>(rootKey, rootValue);
            _nodes[Root.Key] = Root;
        }

        public bool ContainsNode(string key) => _nodes.ContainsKey(key);

        public AbstractTreeNode<T>? GetNode(string key)
        {
            return _nodes.TryGetValue(key, out var node) ? node : null;
        }

        IAbstractTreeNode<T>? IAbstractTree<T>.GetNode(string key) => GetNode(key);

        public bool TryGetNode(string key, out IAbstractTreeNode<T>? node)
        {
            bool found = _nodes.TryGetValue(key, out var foundNode);
            node = foundNode;
            return found;
        }

        /// <exception cref="InvalidOperationException">The parent does not exist, or the key is already used.</exception>
        public AbstractTreeNode<T> AddNode(string key, T value, string parentKey)
        {
            if (_nodes.ContainsKey(key))
                throw new InvalidOperationException($"Node with key '{key}' already exists.");

            var parentNode = GetNode(parentKey)
                ?? throw new InvalidOperationException($"Parent node with key '{parentKey}' not found.");

            var node = new AbstractTreeNode<T>(key, value, parentNode);
            _nodes[key] = node;
            return node;
        }

        /// <summary>
        /// Removes the node and all its descendants. The root cannot be removed.
        /// </summary>
        /// <returns>False if the node does not exist or is the root.</returns>
        public bool RemoveNode(string key)
        {
            if (!_nodes.TryGetValue(key, out var node) || node == Root)
                return false;

            node.RemoveParent();

            var pending = new Stack<AbstractTreeNode<T>>();
            pending.Push(node);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                _nodes.Remove(current.Key);

                foreach (var child in current.Children)
                    pending.Push(child);
            }

            return true;
        }
    }
}
