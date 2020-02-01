// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class MultilineTextBox : TextBox
    {
        private ScrollContainer<Drawable> scroll;

        protected virtual float TopBottomPadding => 5;

        public override bool OnPressed(PlatformAction action)
        {
            int? amount = null;

            //int i, indexInLine = 0, targetLineLength = 0;

            int indexInLine;

            switch (action.ActionType)
            {
                case PlatformActionType.LinePrevious:
                    if (Text.LastIndexOf('\n', SelectionEnd - 1) < 0)
                    {
                        amount = -SelectionEnd;
                        break;
                    }

                    indexInLine = SelectionEnd - (Text.LastIndexOf('\n', SelectionEnd - 1) + 1);
                    int prevLineIndex = SelectionEnd - indexInLine - 2;
                    while (prevLineIndex > 0 && Text[prevLineIndex] != '\n')
                        prevLineIndex--;

                    int prevLineEnd = SelectionEnd - indexInLine - 1;
                    amount = (prevLineIndex + Math.Min(indexInLine, prevLineEnd - prevLineIndex)) - SelectionEnd;
                    break;

                case PlatformActionType.LineNext:
                    if (Text.IndexOf('\n', SelectionEnd - 1) < 0)
                    {
                        amount = Text.Length - SelectionEnd;
                        break;
                    }

                    indexInLine = SelectionEnd - Math.Max(Text.LastIndexOf('\n', SelectionEnd - 1) + 1, 0);
                    int nextLineIndex = Math.Max(SelectionEnd - 1, 0);
                    while (nextLineIndex < Text.Length && Text[nextLineIndex] != '\n')
                        nextLineIndex++;

                    int nextLineEnd = ++nextLineIndex;
                    while (nextLineEnd < Text.Length && Text[nextLineEnd] != '\n')
                        nextLineEnd++;

                    amount = (nextLineIndex + Math.Min(indexInLine, nextLineEnd - nextLineIndex)) - SelectionEnd;
                    break;

                case PlatformActionType.LineStart:
                    amount = 0;
                    for (int i = SelectionEnd; i >= 0 && Text[i] != '\n'; i--)
                        amount--;

                    break;

                case PlatformActionType.LineEnd:
                    for (int i = SelectionEnd; i < Text.Length && Text[i] != '\n'; i++)
                        amount++;

                    break;
            }

            if (amount is int val)
            {
                switch (action.ActionMethod)
                {
                    case PlatformActionMethod.Move:
                        ResetSelection();
                        MoveSelection(val, false);
                        return true;

                    case PlatformActionMethod.Select:
                        MoveSelection(val, true);
                        return true;

                    case PlatformActionMethod.Delete:
                        if (SelectionLength == 0)
                            SelectionEnd = Math.Clamp(SelectionStart + amount.Value, 0, Text.Length);
                        if (SelectionLength > 0)
                            RemoveCharacterOrSelection();

                        return true;
                }
            }

            if (action.ActionType == PlatformActionType.NewLine)
            {
                InsertCharacter('\n');
                return true;
            }

            return base.OnPressed(action);
        }

        protected sealed override Drawable GetDrawableCharacter(char c)
        {
            if (c == '\n')
                return new NewLineTextCharacter(TextSize);

            return GetTextCharacter(c);
        }

        protected virtual Drawable GetTextCharacter(char c) => base.GetDrawableCharacter(c);

        protected override Vector2 ComputeCaretPositionAt(int index)
        {
            if (index == 0)
                return Vector2.Zero;

            if (index >= TextFlow.Count)
            {
                var last = TextFlow[index - 1];

                if (last is NewLineTextCharacter)
                    return new Vector2(TextFlow.DrawPosition.X + TextFlow.Spacing.X, TextFlow.DrawPosition.Y + TextFlow.DrawHeight);

                return new Vector2(TextFlow.DrawPosition.X + TextFlow.Spacing.X + last.DrawPosition.X + last.DrawWidth, TextFlow.DrawPosition.Y + last.DrawPosition.Y);
            }

            var d = TextFlow[index];
            return TextFlow.DrawPosition + d.DrawPosition;
        }

        protected override void UpdateCursorAndLayout()
        {
            base.UpdateCursorAndLayout();

            Caret.DelayUntilTransformsFinished().Schedule(() => scroll.ScrollIntoView(Caret));
        }

        protected abstract ScrollContainer<Drawable> CreateVerticalScrollContainer();

        protected override Container<Drawable> CreateTextContainerSubTree()
        {
            scroll = CreateVerticalScrollContainer();
            scroll.Child = base.CreateTextContainerSubTree();
            return scroll;
        }

        protected override Container CreateTextContainer() => new Container
        {
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            AutoSizeAxes = Axes.Both,
            Position = new Vector2(LeftRightPadding, TopBottomPadding),
        };

        protected override FillFlowContainer CreateTextFlowContainer() => new MultilineTextFlowContainer
        {
            Anchor = Anchor.TopLeft,
            Origin = Anchor.TopLeft,
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Full,
        };

        protected override float TextSize => 20;

        protected override bool CanAddCharacter(char character) => base.CanAddCharacter(character) || character == '\n';

        protected class MultilineTextFlowContainer : FillFlowContainer
        {
            protected override bool ForceNewRow(Drawable child) =>
                // Move this and upcoming characters to a new row if previous is new-line.
                this[Math.Max(0, IndexOf(child) - 1)] is NewLineTextCharacter;
        }

        /// <summary>
        /// A drawable to be added to the <see cref="MultilineTextFlowContainer"/> to move any character after it to a new line.
        /// </summary>
        protected class NewLineTextCharacter : Drawable
        {
            public NewLineTextCharacter(float textSize)
            {
                Width = (Height = textSize) * 0.25f;
            }
        }
    }
}
