using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Utils;

namespace FormuLang.NET.Core.Primitives
{
    public class Object : IObject
    {
        protected readonly Dictionary<string, Container> _properties = new Dictionary<string, Container>();

        // Keys in the order in which they were defined (docs/standards/core/functions/KEYS.md)
        private readonly List<string> _order = [];

        public IEnumerable<string> Keys => _order;

        public IContainer GetProperty(string key) => _properties[key];

        public bool HasKey(string key) => _properties.ContainsKey(key);

        public bool IsEmpty() => _properties.Count == 0;

        public bool InitConstant(string key, IValue value)
        {
            if (_properties.ContainsKey(key))
                return false;

            _properties[key] = new Constant(key, value);
            _order.Add(key);
            return true;
        }

        public bool InitVariable(string key, IValue value)
        {
            if (_properties.ContainsKey(key))
                return false;

            _properties[key] = new Variable(key, value);
            _order.Add(key);
            return true;
        }

        public IError SetProperty(string key, IValue value)
        {
            if (!_properties.TryGetValue(key, out Container? container))
                return Errors.InvalidKeyError($"Key '{key}' does not exist.");

            return container.SetValue(value);
        }

        /// <summary>
        /// Creates a constant, or replaces an existing property with a constant. Meant for the host application, e.g. to
        /// set the current iteration of a loop context; a formula can never change a constant.
        /// </summary>
        public void SetConstant(string key, IValue value) => SetContainer(new Constant(key, value));

        /// <summary>
        /// Creates or replaces a property with the given container, e.g. a <see cref="ComputedConstant"/>. An existing key
        /// keeps its position in <see cref="Keys"/>.
        /// </summary>
        public void SetContainer(Container container)
        {
            if (!_properties.ContainsKey(container.Name))
                _order.Add(container.Name);

            _properties[container.Name] = container;
        }

        /// <summary>
        /// Removes a property (variable or constant).
        /// </summary>
        /// <returns>False if the key does not exist.</returns>
        public bool RemoveProperty(string key)
        {
            if (!_properties.Remove(key))
                return false;

            _order.Remove(key);
            return true;
        }

        public override string ToString() => StringConverter.ObjectToString(this);
    }
}
