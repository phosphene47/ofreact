using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ofreact
{
    public class CollectionPropProvider : IPropProvider
    {
        /// <summary>
        /// Determines whether the given type is a collection or not.
        /// </summary>
        /// <param name="type">Type to determine.</param>
        /// <param name="constructed">Type that will be constructed.</param>
        public static bool IsCollection(Type type, out Type constructed)
        {
            if (type.IsArray)
            {
                constructed = type;
                return true;
            }

            if (!type.IsAbstract)
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);

                if (ctor != null)
                {
                    constructed = type;
                    return true;
                }
            }

            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments();

                if (args.Length == 1)
                {
                    var arg   = args[0];
                    var array = arg.MakeArrayType();

                    // array
                    if (type.IsAssignableFrom(array))
                    {
                        constructed = array;
                        return true;
                    }

                    //todo: this breaks with aot
                    var list = typeof(List<>).MakeGenericType(arg);

                    // list
                    if (type.IsAssignableFrom(list))
                    {
                        constructed = list;
                        return true;
                    }
                }
            }

            constructed = null;
            return false;
        }

        readonly Type _type;
        readonly IPropProvider[] _items;

        /// <summary>
        /// Creates a new <see cref="CollectionPropProvider"/>.
        /// </summary>
        /// <param name="type">Type of the collection object.</param>
        /// <param name="items">Items to initialize the collection with.</param>
        public CollectionPropProvider(Type type, IEnumerable<IPropProvider> items)
        {
            if (!IsCollection(type, out _type))
                throw new ArgumentException($"{type} is not a valid collection type.");

            _items = items.ToArray();
        }

        public Expression GetValue(Expression node)
        {
            var items = _items.Select(x => x.GetValue(node));

            var elementType = _type.GetElementType();

            if (elementType != null)
                return Expression.NewArrayInit(elementType, items);

            return Expression.ListInit(Expression.New(_type), items);
        }
    }
}