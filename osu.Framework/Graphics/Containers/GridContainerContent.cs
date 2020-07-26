// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Lists;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A wrapper that provides access to the <see cref="GridContainer"/>'s content with notifying the container about any change to it.
    /// </summary>
    public class GridContainerContent : ElementChangeNotifierArray<ElementChangeNotifierArray<Drawable>>
    {
        private GridContainerContent(Drawable[][] source)
            : base(new ElementChangeNotifierArray<Drawable>[source.Length])
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null)
                    continue;

                var notifier = new ElementChangeNotifierArray<Drawable>(source[i]);
                this[i] = notifier;
            }
        }

        public static implicit operator GridContainerContent(Drawable[][] source)
        {
            if (source == null)
                return null;

            return new GridContainerContent(source);
        }
    }
}
