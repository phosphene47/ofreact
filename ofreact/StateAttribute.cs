using System;
using System.Reflection;

namespace ofreact
{
    /// <summary>
    /// Marks a field or parameter as a state of an element.
    /// </summary>
    /// <remarks>
    /// State fields must be a constructed <see cref="StateObject{T}"/>.
    /// When used on a method parameter, <see cref="StateObject{T}"/> or its unwrapped value will be injected as the argument.
    /// States will cause a rerender when its value changes.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter)]
    public class StateAttribute : Attribute, IElementFieldBinder, IElementMethodArgumentProvider
    {
        readonly object _initialValue;

        string _name;
        ContainerObjectFactoryDelegate _create;
        bool _wrapped;

        /// <summary>
        /// Creates a new <see cref="StateAttribute"/>.
        /// </summary>
        public StateAttribute() { }

        /// <summary>
        /// Creates a new <see cref="StateAttribute"/> with the given initial value.
        /// </summary>
        /// <param name="initialValue">Initial value of the stateful value.</param>
        public StateAttribute(object initialValue)
        {
            _initialValue = initialValue;
        }

        public FieldInfo Field { get; private set; }
        public ParameterInfo Parameter { get; private set; }

        void IElementFieldBinder.Initialize(FieldInfo field)
        {
            Field   = field;
            _name   = field.Name;
            _create = InternalReflection.GetStateObjectFactory(field.FieldType, out _wrapped);

            if (_wrapped)
                throw new ArgumentException($"Field {field} of {field.DeclaringType} must be a type of {typeof(RefObject<>)}");
        }

        void IElementMethodArgumentProvider.Initialize(ParameterInfo parameter)
        {
            Parameter = parameter;
            _name     = parameter.Name;
            _create   = InternalReflection.GetStateObjectFactory(parameter.ParameterType, out _wrapped);
        }

        object IElementFieldBinder.GetValue(ofElement element)
        {
            var container = _create(element.Node, _name, _initialValue);

            if (_wrapped)
                return container.Current;

            return container;
        }

        object IElementMethodArgumentProvider.GetValue(ofElement element)
        {
            var container = _create(element.Node, _name, _initialValue);

            if (_wrapped)
                return container.Current;

            return container;
        }
    }
}