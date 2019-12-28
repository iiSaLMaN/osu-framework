// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneInternalContent : FrameworkTestScene
    {
        public TestSceneInternalContent()
        {
            Add(new TestComposite() { Size = new Vector2(50, 200) });
        }

        private class TestComposite : CompositeDrawable
        {
            private readonly CompositeDrawable internalContent;

            protected override CompositeDrawable InternalContent => internalContent;

            public TestComposite()
            {
                InternalChild = internalContent = new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                };

                InternalChild = new Box
                {
                    Colour = Color4.White,
                    RelativeSizeAxes = Axes.Both,
                };
            }
        }
    }
}
