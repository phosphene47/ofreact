using System.Collections.Generic;

namespace ofreact
{
    public delegate void RefDelegate<in T>(T value);

    /// <summary>
    /// A container that represents a reference to a value.
    /// </summary>
    /// <typeparam name="T">Type of the value.</typeparam>
    public class RefObject<T>
    {
        readonly Dictionary<string, object> _dict;
        readonly string _key;

        /// <summary>
        /// Gets or sets the current value referenced by this object.
        /// </summary>
        public T Current
        {
            get => _dict.TryGetValue(_key, out var value) ? (T) value : default;
            set => _dict[_key] = value;
        }

        internal RefObject(ofNode node, string key, T initialValue)
        {
            _dict = node.State;
            _key  = key;

            if (!_dict.ContainsKey(key))
                Current = initialValue;
        }

        /// <summary>
        /// Returns <see cref="Current"/>.
        /// </summary>
        public static implicit operator T(RefObject<T> obj) => obj.Current;

        /// <summary>
        /// Creates a <see cref="RefDelegate{T}"/> that sets <see cref="Current"/> as the given argument.
        /// </summary>
        public static implicit operator RefDelegate<T>(RefObject<T> obj) => v => obj.Current = v;
    }
}