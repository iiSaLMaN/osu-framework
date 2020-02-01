// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicMultilineTextBox : MultilineTextBox
    {
        protected virtual float CaretWidth => 2;

        private const float caret_move_time = 60;

        protected virtual Color4 SelectionColour => FrameworkColour.YellowGreen;

        protected Color4 BackgroundCommit { get; set; } = FrameworkColour.Green;

        private Color4 backgroundFocused = new Color4(100, 100, 100, 255);
        private Color4 backgroundUnfocused = new Color4(100, 100, 100, 120);

        private readonly Box background;

        protected Color4 BackgroundFocused
        {
            get => backgroundFocused;
            set
            {
                backgroundFocused = value;
                if (HasFocus)
                    background.Colour = value;
            }
        }

        protected Color4 BackgroundUnfocused
        {
            get => backgroundUnfocused;
            set
            {
                backgroundUnfocused = value;
                if (!HasFocus)
                    background.Colour = value;
            }
        }

        protected virtual Color4 InputErrorColour => Color4.Red;

        public BasicMultilineTextBox()
        {
            Add(background = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Depth = 1,
                Colour = BackgroundUnfocused,
            });

            BackgroundFocused = FrameworkColour.BlueGreen;
            BackgroundUnfocused = FrameworkColour.BlueGreenDark;
        }

        protected override void NotifyInputError() => background.FlashColour(InputErrorColour, 200);

        protected override Drawable GetTextCharacter(char c) => new BasicTextBox.FallingDownContainer
        {
            AutoSizeAxes = Axes.Both,
            Child = new SpriteText { Text = c.ToString(), Font = FrameworkFont.Condensed.With(size: TextSize) }
        };

        //protected override ScrollContainer<Drawable> CreateHorizontalScrollContainer() => new BasicScrollContainer(Direction.Horizontal)
        //{
        //    AutoSizeAxes = Axes.X,
        //    RelativeSizeAxes = Axes.Y,
        //};

        protected override ScrollContainer<Drawable> CreateVerticalScrollContainer() => new BasicScrollContainer(Direction.Vertical)
        {
            RelativeSizeAxes = Axes.Both,
        };

        protected override SpriteText CreatePlaceholder() => new BasicTextBox.FadingPlaceholderText
        {
            Colour = FrameworkColour.YellowGreen,
            Font = FrameworkFont.Condensed,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            X = CaretWidth,
        };

        protected override Caret CreateCaret() => new BasicTextBox.BasicCaret
        {
            CaretWidth = CaretWidth,
            RelativeSizeAxes = Axes.None,
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            Height = TextSize,
        };

        protected override Highlighter CreateSelectionHighlighter(FillFlowContainer textFlow) => new BasicHighlighter(textFlow)
        {
            HighlightColour = SelectionColour,
        };
    }
}
