using static ofreact.Hooks;

namespace ofreact
{
    /// <summary>
    /// Represents the base class for an ofreact component.
    /// </summary>
    /// <remarks>
    /// This component is equivalent to react's PureComponent which optimizes rerenders if props are not changed.
    /// </remarks>
    public abstract class ofComponent : ofElement
    {
        /// <summary>
        /// Creates a new <see cref="ofComponent"/>.
        /// </summary>
        protected ofComponent(ElementKey key = default) : base(key) { }

        /// <summary>
        /// Renders this component and returns the rendered element.
        /// </summary>
        protected abstract ofElement Render();

        protected internal sealed override bool RenderSubtree()
        {
            if (!base.RenderSubtree())
                return false;

            var nodeRef = UseChild();
            var node    = nodeRef.Current;

            var child = Render();

            if (child == null)
            {
                if (node != null)
                {
                    node.Dispose();
                    nodeRef.Current = null;
                }

                return false;
            }

            node ??= nodeRef.Current = Node.CreateChild();

            var result = node.RenderElement(child);

            if (result == RenderResult.Mismatch)
            {
                node.Dispose();
                node = nodeRef.Current = Node.CreateChild();

                result = node.RenderElement(child);
            }

            return result == RenderResult.Rendered;
        }
    }
}