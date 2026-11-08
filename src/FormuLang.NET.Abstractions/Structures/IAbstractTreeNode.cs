using System;

namespace FormuLang.NET.Abstractions.Structures
{
    /// <summary>
    /// Represents a node in an abstract tree structure, which can be used to model hierarchical data such as expression trees, syntax trees, or any other tree-like data structures.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the node.</typeparam>
    public interface IAbstractTreeNode<T>
    {
        /// <summary>
        /// Value stored in the node.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Node's unique identifier within the tree.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Parent node of the current node. This property is null for the root or orphaned node.
        /// </summary>
        public IAbstractTreeNode<T>? Parent { get; }

        /// <summary>
        /// Collection of child nodes directly connected to the current node.
        /// </summary>
        public IEnumerable<IAbstractTreeNode<T>> Children { get; }

        /// <summary>
        /// Specifies whether the current node is a direct parent of the given node.
        /// </summary>
        /// <param name="node">The node to check against.</param>
        /// <returns>True if the current node is a direct parent of the given node; otherwise, false.</returns>
        public bool IsParentOf(IAbstractTreeNode<T> node);

        /// <summary>
        /// Specifies whether the current node is a direct child of the given node.
        /// </summary>
        /// <param name="node">The node to check against.</param>
        /// <returns>True if the current node is a direct child of the given node; otherwise, false.</returns>
        public bool IsChildOf(IAbstractTreeNode<T> node);

        /// <summary>
        /// Specifies whether the current node is a direct or indirect ancestor of the given node.
        /// </summary>
        /// <param name="node">The node to check against.</param>
        /// <returns>True if the current node is a direct or indirect ancestor of the given node; otherwise, false.</returns>
        public bool IsAncestorOf(IAbstractTreeNode<T> node);

        /// <summary>
        /// Specifies whether the current node is a direct or indirect descendant of the given node.
        /// </summary>
        /// <param name="node">The node to check against.</param>
        /// <returns>True if the current node is a direct or indirect descendant of the given node; otherwise, false.</returns>
        public bool IsDescendantOf(IAbstractTreeNode<T> node);
    }
}
