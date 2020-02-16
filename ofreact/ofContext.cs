using System;
using System.Collections.Generic;

namespace ofreact
{
    /// <summary>
    /// Makes a context object available to all descendants of this element.
    /// </summary>
    /// <remarks>
    /// Contexts objects that implement <see cref="IDisposable"/> will be disposed automatically.
    /// </remarks>
    /// <typeparam name="TContext">Type of context object.</typeparam>
    public class ofContext<TContext> : ofFragment
    {
        [Prop] public readonly TContext Value;

        /// <summary>
        /// Creates a new <see cref="ofContext{TContext}"/>.
        /// </summary>
        public ofContext(object key = default, IEnumerable<ofElement> children = default, TContext value = default) : base(key, children)
        {
            Value = value;
        }

        protected internal override bool RenderSubtree()
        {
            UseEffect(() =>
            {
                if (Value is IDisposable disposable)
                    return disposable.Dispose;

                return null;
            }, Value);

            Node.LocalContext = Value;

            return base.RenderSubtree();
        }
    }
}