using FormuLang.NET.Abstractions.Primitives;
using FormuLang.NET.Core.Utils;

namespace FormuLang.NET.Core.Primitives
{
    public class Object : IObject
    {
        protected readonly Dictionary<string, Container> _properties = new Dictionary<string, Container>();

        public IEnumerable<string> Keys => _properties.Keys;

        public IContainer GetProperty(string key) => _properties[key];

        public bool HasKey(string key) => _properties.ContainsKey(key);

        public bool IsEmpty() => _properties.Count == 0;

        public bool InitConstant(string key, IValue value)
        {
            if (_properties.ContainsKey(key))
                return false;

            _properties[key] = new Constant(key, value);
            return true;
        }

        public bool InitVariable(string key, IValue value)
        {
            if (_properties.ContainsKey(key))
                return false;

            _properties[key] = new Variable(key, value);
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
        public void SetConstant(string key, IValue value)
        {
            _properties[key] = new Constant(key, value);
        }

        /// <summary>
        /// Removes a property (variable or constant).
        /// </summary>
        /// <returns>False if the key does not exist.</returns>
        public bool RemoveProperty(string key) => _properties.Remove(key);

        public override string ToString() => StringConverter.ObjectToString(this);
    }
}
