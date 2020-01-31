// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// Represents a component that can highlight any specified area or <see cref="Drawable"/> in a <see cref="Container"/>.
    /// </summary>
    public abstract class Highlighter : CompositeDrawable
    {
        /// <summary>
        /// The source to highlight lines onto.
        /// </summary>
        protected readonly Container<Drawable> Source;

        protected readonly Container<HighlightLine> HighlightContainer;

        protected Highlighter(Container<Drawable> source)
        {
            RelativeSizeAxes = Axes.Both;

            Source = source;
            InternalChild = HighlightContainer = CreateHighlightContainer();
        }

        private static bool requiresNewLine(Drawable drawable, Drawable last)
            => drawable.DrawPosition.Y != last.DrawPosition.Y;

        // Reuse existing highlight lines to reduce allocations.
        private HighlightLine getHighlightLineAt(int line)
        {
            while (HighlightContainer.Count <= line)
            {
                var hl = CreateHighlightLine();
                hl.Reset();
                HighlightContainer.Add(hl);
            }

            return HighlightContainer[line];
        }

        /// <summary>
        /// Highlights an area from an <paramref name="offset"/> with specified <paramref name="length"/>.
        /// </summary>
        /// <param name="offset">The index to start highlighting from.</param>
        /// <param name="length">The length of the highlight area.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when:
        ///  Specified <paramref name="offset"/> is out of range.
        ///  Specified <paramref name="length"/> is zero or negative.
        ///  Sum of <paramref name="offset"/> and <paramref name="length"/> exceeds <see cref="Source"/> length.
        /// </exception>
        public virtual void HighlightFrom(int offset, int length)
        {
            if (offset < 0 || offset > Source.Count)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, $"Must be within {nameof(Source)} range.");

            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), length, "Must be non-zero and positive.");

            int end = offset + length;

            if (end > Source.Count)
                throw new ArgumentOutOfRangeException(nameof(length), length, $"({nameof(offset)} + {nameof(length)}) must not be greater than {nameof(Source.Count)}.");

            int lineIndex = 0;

            // Catch-up to the line index of the first to-be-highlighted drawable.
            for (int i = 1; i < offset; i++)
            {
                if (requiresNewLine(Source[i], Source[i - 1]))
                    // Reset unused highlight lines to be re-used later on.
                    getHighlightLineAt(lineIndex++).Reset();
            }

            Drawable last = null;

            HighlightLine line = getHighlightLineAt(lineIndex);
            Vector2 linePosition = Source[offset].DrawPosition;
            float lineHeight = 0;

            // Start highlighting specified range.
            for (int i = offset; i <= end; i++)
            {
                var drawable = Source[Math.Min(i, Source.Count - 1)];

                // Check if this drawable has (landed on a new row or the loop will end) to display a highlight line.
                if (last != null && (requiresNewLine(drawable, last) || i == end))
                {
                    line.DisplayAt(linePosition, new Vector2((last.DrawPosition.X + last.DrawWidth) - linePosition.X, lineHeight));

                    // Go to the next row if not finished yet.
                    if (i < end)
                    {
                        line = getHighlightLineAt(++lineIndex);
                        linePosition = drawable.DrawPosition;
                        lineHeight = 0;
                    }
                }

                // Calculate highlight line height by maximum height of each drawable to fit all of them.
                lineHeight = Math.Max(drawable.DrawHeight, lineHeight);
                last = drawable;
            }

            // Reset properties of unused highlight lines to be re-used later on.
            while (++lineIndex < HighlightContainer.Count)
                HighlightContainer[lineIndex].Reset();
        }

        /// <summary>
        /// Highlights a specific drawable.
        /// </summary>
        /// <param name="child"></param>
        public void Highlight(Drawable child)
        {
            var index = Source.IndexOf(child);
            if (index < 0)
                throw new ArgumentException($"Attempting to highlight a drawable not within {nameof(Source)}.", nameof(child));

            HighlightFrom(index, 1);
        }

        /// <summary>
        /// Removes highlighted area.
        /// </summary>
        public void RemoveHighlight() => HighlightContainer.ForEach(l => l.Reset());

        /// <summary>
        /// Creates a container to store <see cref="HighlightLine"/>s at.
        /// </summary>
        protected virtual Container<HighlightLine> CreateHighlightContainer() => new Container<HighlightLine>
        {
            RelativeSizeAxes = Axes.Both,
        };

        /// <summary>
        /// Creates a highlight line to be added to <see cref="HighlightContainer"/> and used in this component.
        /// </summary>
        protected abstract HighlightLine CreateHighlightLine();

        public abstract class HighlightLine : CompositeDrawable
        {
            /// <summary>
            /// Resets this to be re-used as a new component.
            /// This method should be responsible for bring this <see cref="HighlightLine"/> into a state before <see cref="DisplayAt"/> is ever called.
            /// </summary>
            public abstract void Reset();

            /// <summary>
            /// Displays this <see cref="HighlightLine"/> at a specified <paramref name="position"/> with specified <paramref name="size"/>.
            /// </summary>
            /// <param name="position">The position.</param>
            /// <param name="size">The size.</param>
            public abstract void DisplayAt(Vector2 position, Vector2 size);
        }
    }
}
