using System;

namespace FormuLang.NET.Abstractions.Structures
{
    public interface IAbstractTree<T>
    {
        /// <summary>
        /// Root node of the tree.
        /// </summary>
        public IAbstractTreeNode<T> Root { get; }

        /// <summary>
        /// Checks if a node with the specified key exists in the tree.
        /// </summary>
        /// <param name="key">The unique identifier of the node to check.</param>
        /// <returns>True if a node with the specified key exists; otherwise, false.</returns>
        public bool ContainsNode(string key);

        /// <summary>
        /// Retrieves a node from the tree based on its unique key.
        /// </summary>
        /// <param name="key">The unique identifier of the node to retrieve.</param>
        /// <returns>The node associated with the specified key.</returns>
        public IAbstractTreeNode<T>? GetNode(string key);

        /// <summary>
        /// Attempts to retrieve a node from the tree based on its unique key, returning a boolean indicating success or failure.
        /// </summary>
        /// <param name="key">The unique identifier of the node to retrieve.</param>
        /// <param name="node">When this method returns, contains the node associated with the specified key, if the key is found; otherwise, null.</param>
        /// <returns>True if the node was found; otherwise, false.</returns>
        public bool TryGetNode(string key, out IAbstractTreeNode<T>? node);
    }

    public interface IAbstractTree : IAbstractTree<object> {}
}
