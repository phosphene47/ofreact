using ofreact;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;

namespace osu.Framework.Declarative
{
    public sealed class TextFlowStyle : TextFlowStyle<TextFlowContainer> { }

    public abstract class TextFlowStyle<T> : FillFlowStyle<T> where T : TextFlowContainer
    {
        /// <inheritdoc cref="TextFlowContainer.FirstLineIndent"/>
        public float? FirstLineIndent;

        /// <inheritdoc cref="TextFlowContainer.ContentIndent"/>
        public float? ContentIndent;

        /// <inheritdoc cref="TextFlowContainer.ParagraphSpacing"/>
        public float? ParagraphSpacing;

        /// <inheritdoc cref="TextFlowContainer.LineSpacing"/>
        public float? LineSpacing;

        /// <inheritdoc cref="TextFlowContainer.TextAnchor"/>
        public Anchor? TextAnchor;

        /// <inheritdoc cref="TextFlowContainer.Text"/>
        public string Text;

        protected override void Apply(T drawable)
        {
            base.Apply(drawable);

            if (FirstLineIndent != null)
                drawable.FirstLineIndent = FirstLineIndent.Value;

            if (ContentIndent != null)
                drawable.ContentIndent = ContentIndent.Value;

            if (ParagraphSpacing != null)
                drawable.ParagraphSpacing = ParagraphSpacing.Value;

            if (LineSpacing != null)
                drawable.LineSpacing = LineSpacing.Value;

            if (TextAnchor != null)
                drawable.TextAnchor = TextAnchor.Value;

            if (Text != null)
                drawable.Text = Text;
        }
    }

    public sealed class ofTextFlow : ofTextFlow<TextFlowContainer>
    {
        public ofTextFlow(ElementKey key = default, RefDelegate<TextFlowContainer> @ref = default, DrawableStyleDelegate<TextFlowContainer> style = default, DrawableEventDelegate @event = default) : base(key, @ref, style, @event) { }

        protected override TextFlowContainer CreateDrawable() => new InternalTextFlowContainer();

        sealed class InternalTextFlowContainer : TextFlowContainer, ISupportEventDelegation
        {
            public DrawableEventDelegate EventDelegate { get; set; }
            public override bool HandlePositionalInput => base.HandlePositionalInput || EventDelegate != null;
            public override bool HandleNonPositionalInput => base.HandleNonPositionalInput || EventDelegate != null;

            protected override bool Handle(UIEvent e) => base.Handle(e) || EventDelegate(e);
        }
    }

    public abstract class ofTextFlow<T> : ofDrawableBase<T> where T : TextFlowContainer
    {
        protected ofTextFlow(ElementKey key = default, RefDelegate<T> @ref = default, DrawableStyleDelegate<T> style = default, DrawableEventDelegate @event = default) : base(key, @ref, style, @event) { }
    }
}