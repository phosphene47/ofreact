using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ofreact.Diagnostics;

namespace ofreact
{
    /// <summary>
    /// Represents a node in ofreact.
    /// </summary>
    public class ofNode : IDisposable
    {
        /// <summary>
        /// <see cref="ofNode"/> that is the root of the tree.
        /// </summary>
        public ofRootNode Root { get; internal set; }

        /// <summary>
        /// <see cref="ofNode"/> that contains this node.
        /// </summary>
        public ofNode Parent { get; }

        Dictionary<string, object> _state;
        HashSet<EffectInfo> _effects;
        ContextInfo _context;

        /// <summary>
        /// Dictionary of stateful variables.
        /// </summary>
        /// <remarks>
        /// Named states are lowercase string keys.
        /// Hook states are string keys prefixed with ^ (caret character) followed by zero-based hook index.
        /// </remarks>
        public IDictionary<string, object> State => _state ??= new Dictionary<string, object>(1);

        /// <summary>
        /// List of effects specific to this node.
        /// </summary>
        public ISet<EffectInfo> LocalEffects => _effects ??= new HashSet<EffectInfo>(1);

        /// <summary>
        /// Context object specific to this node.
        /// </summary>
        public ContextInfo LocalContext => _context ??= new ContextInfo();

        /// <summary>
        /// If true, this node will always be considered as invalid and therefore eligible for rerender.
        /// </summary>
        public bool AlwaysInvalid { get; set; }

        /// <summary>
        /// Gets the last element bound to this node.
        /// </summary>
        public ofElement Element { get; internal set; }

        int? _hooks;
        int? _lastHook;

        internal ofNode(ofNode parent)
        {
            Root   = parent?.Root;
            Parent = parent;
        }

        /// <summary>
        /// Renders the given element.
        /// </summary>
        /// <remarks>
        /// The caller is responsible for ensuring that <see cref="CanRenderElement"/> returns true.
        /// </remarks>
        public virtual bool RenderElement(ofElement element)
        {
            var shouldRender = Root.RerenderNodes.Remove(this); // remove first to avoid infinite rendering loop

            if (element == null)
                return false;

            shouldRender = shouldRender || AlwaysInvalid || Element == null || Element.ShouldComponentUpdate(element);

            if (!shouldRender)
            {
                Root.Diagnostics?.OnNodeRenderSkipped(this);
                return false;
            }

            // enable hooks
            _hooks = 0;

            try
            {
                using (element.Bind(this))
                {
                    Root.Diagnostics?.OnNodeRendering(this);

                    var result = element.RenderSubtree();

                    if (result && ofElement.IsHookValidated)
                    {
                        if (_lastHook == null)
                            _lastHook = _hooks;

                        else if (_lastHook != _hooks)
                            throw new InvalidOperationException($"The number of hooks ({_hooks}) does not match with the previous render ({_lastHook}). " +
                                                                "See https://reactjs.org/docs/hooks-rules.html for rules about hooks.");
                    }

                    return result;
                }
            }
            catch (Exception e)
            {
                Root.Diagnostics?.OnException(e);
                throw;
            }
            finally
            {
                _hooks = null;
            }
        }

        /// <summary>
        /// Determines whether the given element can be bound to this node and rendered or not.
        /// </summary>
        /// <param name="element">Element to test.</param>
        /// <returns>True if the element can be bound to this node and rendered.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanRenderElement(ofElement element)
        {
            if (element == null)
                return false;

            if (Element == null || ReferenceEquals(element, Element))
                return true;

            return element.GetType() == Element.GetType() && KeysEqual(element, Element);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool KeysEqual(ofElement a, ofElement b) => a.Key == b.Key || a.Key != null && a.Key.Equals(b.Key);

        /// <summary>
        /// Returns a named mutable <see cref="RefObject{T}"/> holding a strongly typed variable that is persisted across renders.
        /// </summary>
        /// <param name="key">Name of the reference.</param>
        /// <param name="initialValue">Initial value of the referenced value.</param>
        /// <typeparam name="T">Type of the referenced value.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefObject<T> GetNamedRef<T>(string key, T initialValue = default) => new RefObject<T>(this, key, initialValue);

        /// <summary>
        /// Returns a mutable <see cref="RefObject{T}"/> used for hooks holding a strongly typed variable that is persisted across renders.
        /// </summary>
        /// <param name="initialValue">Initial value of the referenced value.</param>
        /// <param name="index">Zero-based index of the hook, or null to use auto-incrementing index.</param>
        /// <typeparam name="T">Type of the referenced value.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefObject<T> GetHookRef<T>(T initialValue = default, int? index = default)
        {
            if (_hooks == null)
                throw new InvalidOperationException($"Cannot use hooks outside the rendering method ({Element.GetType()}).");

            return new RefObject<T>(this, $"^{index ?? _hooks++}", initialValue);
        }

        /// <summary>
        /// Finds the nearest context object from ancestor nodes assignable to the given type.
        /// </summary>
        /// <typeparam name="T">Type of the context object.</typeparam>
        /// <returns>The found context object or default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ContextInfo<T> FindNearestContext<T>()
        {
            var node = this;

            do
            {
                var context = node._context;

                if (context?.Value is T)
                    return context;

                node = node.Parent;
            }
            while (node != null);

            return default;
        }

        /// <summary>
        /// Finds the nearest context object from ancestor nodes assignable to the given type.
        /// </summary>
        /// <param name="type">Type of the context object.</param>
        /// <returns>The found context object or default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ContextInfo FindNearestContext(Type type)
        {
            var node = this;

            do
            {
                var context = node._context;

                if (type.IsInstanceOfType(context?.Value))
                    return context;

                node = node.Parent;
            }
            while (node != null);

            return default;
        }

        /// <summary>
        /// Marks this node for rerender.
        /// </summary>
        /// <returns>True if this node was previously unmarked.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Invalidate()
        {
            if (Root.RerenderNodes.Add(this))
            {
                Root.Diagnostics?.OnNodeInvalidated(this);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates an <see cref="ofNode"/> that is a child of this node.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ofNode CreateChild() => new ofNode(this);

        public virtual void Dispose()
        {
            // do effect cleanups
            if (_effects != null)
                foreach (var effect in _effects)
                    effect.Cleanup();

            _effects = null;
            _state   = null;

            Element = null;

            Root.Diagnostics?.OnNodeDisposed(this);
        }
    }

    /// <summary>
    /// Represents the root node of a declarative tree.
    /// </summary>
    public class ofRootNode : ofNode
    {
        static bool? _enableDiagnostics;

        /// <summary>
        /// Gets or sets a value indicating whether rendering diagnostics is enabled or not.
        /// This is enabled by default if <see cref="Debugger.IsAttached"/> is true.
        /// </summary>
        public static bool IsDiagnosticsEnabled
        {
            get => _enableDiagnostics ?? Debugger.IsAttached;
            set => _enableDiagnostics = value;
        }

        /// <summary>
        /// Set of <see cref="ofNode"/> that are marked for rerender.
        /// </summary>
        public HashSet<ofNode> RerenderNodes { get; } = new HashSet<ofNode>(512);

        /// <summary>
        /// List of effects to be triggered after render.
        /// </summary>
        public Queue<EffectInfo> PendingEffects { get; } = new Queue<EffectInfo>(2048);

        /// <summary>
        /// Diagnostics object used for debugging.
        /// </summary>
        public RenderDiagnostics Diagnostics { get; }

        /// <summary>
        /// Creates a new <see cref="ofRootNode"/>.
        /// </summary>
        public ofRootNode() : base(null)
        {
            Root = this;

            AlwaysInvalid = true;

            if (IsDiagnosticsEnabled)
                Diagnostics = new RenderDiagnostics();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool GetRerenderNodes(out ofNode[] nodes)
        {
            var count = RerenderNodes.Count;

            if (count == 0)
            {
                nodes = null;
                return false;
            }

            nodes = new ofNode[count];
            RerenderNodes.CopyTo(nodes);

            return true;
        }

        public override bool RenderElement(ofElement element)
        {
            Diagnostics?.Clear();

            if (!CanRenderElement(element))
                return false;

            var result = base.RenderElement(element);

            do
            {
                // render nodes marked for rerender
                while (GetRerenderNodes(out var nodes))
                {
                    foreach (var node in nodes)
                        result |= node.RenderElement(node.Element);
                }

                // run effects
                while (PendingEffects.TryDequeue(out var effect))
                {
                    Diagnostics?.OnEffectInvoking(effect);

                    effect.Invoke();
                    result = true;
                }
            }
            while (RerenderNodes.Count != 0);

            return result;
        }

        public override void Dispose()
        {
            base.Dispose();

            RerenderNodes.Clear();
            PendingEffects.Clear();
        }
    }
}