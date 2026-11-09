using FormuLang.NET.Abstractions.Structures;

namespace FormuLang.NET.Core.Structures
{
    public class AbstractTreeNode<T> : IAbstractTreeNode<T>
    {
        private readonly List<AbstractTreeNode<T>> _children = [];

        public string Key { get; }
        public T Value { get; private set; }

        public AbstractTreeNode<T>? Parent { get; private set; }
        IAbstractTreeNode<T>? IAbstractTreeNode<T>.Parent => Parent;

        public IReadOnlyList<AbstractTreeNode<T>> Children => _children;
        IEnumerable<IAbstractTreeNode<T>> IAbstractTreeNode<T>.Children => _children;

        public AbstractTreeNode(string key, T value, AbstractTreeNode<T>? parent = null)
        {
            Key = key;
            Value = value;

            if (parent != null)
                SetParent(parent);
        }

        public void Update(T value)
        {
            Value = value;
        }

        public void SetParent(AbstractTreeNode<T> newParent)
        {
            if (newParent == this || newParent.IsDescendantOf(this))
                throw new InvalidOperationException("A node cannot become a child of itself or of its descendant.");

            Parent?._children.Remove(this);
            Parent = newParent;
            newParent._children.Add(this);
        }

        public void RemoveParent()
        {
            Parent?._children.Remove(this);
            Parent = null;
        }

        public bool IsParentOf(IAbstractTreeNode<T> node)
        {
            return node != null && node.Parent == this;
        }

        public bool IsChildOf(IAbstractTreeNode<T> node)
        {
            return node != null && Parent != null && Parent == node;
        }

        public bool IsAncestorOf(IAbstractTreeNode<T> node)
        {
            return node != null && node.IsDescendantOf(this);
        }

        public bool IsDescendantOf(IAbstractTreeNode<T> node)
        {
            if (node == null)
                return false;

            for (IAbstractTreeNode<T>? current = Parent; current != null; current = current.Parent)
            {
                if (current == node)
                    return true;
            }

            return false;
        }

        public override string ToString() => Key;
    }
}
