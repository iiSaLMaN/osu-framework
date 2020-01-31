// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicHighlighter : Highlighter
    {
        private Color4 highlightColour;

        public Color4 HighlightColour
        {
            get => highlightColour;
            set
            {
                if (highlightColour == value)
                    return;

                highlightColour = value;

                foreach (var line in HighlightContainer)
                    line.Colour = highlightColour;
            }
        }

        public BasicHighlighter(Container<Drawable> source)
            : base(source)
        {
        }

        protected override HighlightLine CreateHighlightLine() => new BasicHighlightLine
        {
            Colour = highlightColour,
        };

        public class BasicHighlightLine : HighlightLine
        {
            private bool hasDisplayed;

            public BasicHighlightLine()
            {
                InternalChild = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                };
            }

            public override void Reset()
            {
                hasDisplayed = false;

                this.FadeOut(200, Easing.Out).Then()
                    .MoveTo(Vector2.Zero).ResizeTo(Vector2.Zero);
            }

            public override void DisplayAt(Vector2 position, Vector2 size)
            {
                // display line immediately on first appearance.
                this.MoveTo(position, 60, Easing.Out)
                    .ResizeTo(size, 60, Easing.Out);

                if (!hasDisplayed)
                    FinishTransforms();

                this.FadeTo(0.5f, 200, Easing.Out);
                hasDisplayed = true;
            }
        }
    }
}
