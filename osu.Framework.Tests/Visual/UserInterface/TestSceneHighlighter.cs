// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneHighlighter : FrameworkTestScene
    {
        private TestHighlighter highlighter;

        [Test]
        public void TestTextFlowHiglighting()
        {
            TextFlowContainer textFlow = new TextFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
            };

            AddStep("load components", () => addComponents(textFlow));
            AddUntilStep("wait for components", () => textFlow.IsLoaded && highlighter.IsLoaded);

            AddSliderStep("higlight", 0, textFlow.Count - 1, 0, i => highlighter?.HighlightFrom(i, 1));
        }

        [Test]
        public void TestFlowHighlighting()
        {
            FillFlowContainer fillFlow = null;
            AddStep("load components", () => addComponents(fillFlow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Children = new[]
                {
                    new Circle { Size = new Vector2(128) },
                    new Circle { Size = new Vector2(128) },
                    new Circle { Size = new Vector2(128) },
                }
            }));
            AddUntilStep("wait for components", () => fillFlow.IsLoaded && highlighter.IsLoaded);

            AddStep("highlight all", () => highlighter.HighlightFrom(0, 3));
            AddAssert("is highlighted", () => isCorrectlyHighlighted(fillFlow, (0, 3, 128)));
            AddStep("highlight second", () => highlighter.Highlight(fillFlow[1]));
            AddAssert("is highlighted", () => isCorrectlyHighlighted(fillFlow, (1, 1, 128)));
        }

        [Test]
        public void TestRandomlyMeasuredHighlighting()
        {
            Container container = null;
            AddStep("load components", () => addComponents(container = new Container
            {
                AutoSizeAxes = Axes.Both,
                Children = new[]
                {
                    new Circle { Size = new Vector2(75), Position = new Vector2(0, 0) },
                    new Circle { Size = new Vector2(75), Position = new Vector2(75, 10) },
                    new Circle { Size = new Vector2(128), Position = new Vector2(150, 10) },
                    new Circle { Size = new Vector2(64), Position = new Vector2(278, 10) },
                    new Circle { Size = new Vector2(150), Position = new Vector2(342, -10) },
                }
            }));
            AddUntilStep("wait for components", () => container.IsLoaded && highlighter.IsLoaded);

            AddStep("highlight all", () => highlighter.HighlightFrom(0, 5));
            AddAssert("is highlighted", () => isCorrectlyHighlighted(container, (0, 1, 75), (1, 3, 128), (4, 1, 150)));
            AddStep("highlight third", () => highlighter.Highlight(container[2]));
            AddAssert("is highlighted", () => isCorrectlyHighlighted(container, (2, 1, 128)));
        }

        [Test]
        public void TestMultiRowsHighlighting()
        {
            FillFlowContainer fillFlow = null;
            AddStep("load components", () => addComponents(fillFlow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Y,
                Width = 256, // force 2 circles per row
                Direction = FillDirection.Full,
                Children = new[]
                {
                    new Circle { Size = new Vector2(128, 128), },
                    new Circle { Size = new Vector2(128, 128), },
                    new Circle { Size = new Vector2(128, 32), },
                    new Circle { Size = new Vector2(128, 64), },
                    new Circle { Size = new Vector2(128, 75), },
                    new Circle { Size = new Vector2(128, 75), },
                },
            }));
            AddUntilStep("wait for components", () => fillFlow.IsLoaded && highlighter.IsLoaded);

            AddStep("highlight all", () => highlighter.HighlightFrom(0, 6));
            AddAssert("is highlighted", () => isCorrectlyHighlighted(fillFlow, (0, 2, 128), (2, 2, 64), (4, 2, 75)));
            AddStep("highlight 3-4", () => highlighter.HighlightFrom(3, 2));
            AddAssert("is highlighted", () => isCorrectlyHighlighted(fillFlow, (3, 1, 64), (4, 1, 75)));
        }

        [Test]
        public void TestRemoveHighlight()
        {
            Container source = null;
            AddStep("load components", () => addComponents(source = new Container
            {
                AutoSizeAxes = Axes.Both,
                Child = new Circle { Size = new Vector2(256) },
            }));
            AddUntilStep("wait for components", () => source.IsLoaded && highlighter.IsLoaded);

            AddStep("highlight", () => highlighter.Highlight(source[0]));
            AddAssert("is highlighted", () => isCorrectlyHighlighted(source, (0, 1, 256)));
            AddStep("remove highlight", () => highlighter.RemoveHighlight());
            AddAssert("no visible highlight line", () => highlighter.HighlightContainer.All(hl => hl.Alpha == 0));
        }

        /// <summary>
        /// Checks whether the <see cref="highlighter"/> correctly highlighted the <see cref="source"/>.
        /// </summary>
        /// <param name="source">The source the <see cref="highlighter"/> is highlighting onto.</param>
        /// <param name="linesMeasure">The index, length and height of each highlight line in the source.</param>
        /// <returns></returns>
        private bool isCorrectlyHighlighted(Container<Drawable> source, params (int index, int length, float height)[] linesMeasure)
        {
            var highlightContainer = highlighter.HighlightContainer.Where(hl => hl.Alpha != 0);

            for (int i = 0; i < linesMeasure.Length; i++)
            {
                (int index, int length, float height) = linesMeasure[i];
                var line = highlightContainer.ElementAt(i);
                Vector2 position = source[index].DrawPosition;
                if (line.Position != position)
                    return false;

                Drawable last = source[index + length - 1];
                var size = new Vector2(last.DrawPosition.X + last.DrawWidth - position.X, height);
                if (line.Size != size)
                    return false;
            }

            return true;
        }

        private void addComponents(Container<Drawable> source)
        {
            Children = new Drawable[]
            {
                source,
                highlighter = new TestHighlighter(source),
            };
        }

        private class TestHighlighter : Highlighter
        {
            public new Container<HighlightLine> HighlightContainer => base.HighlightContainer;

            public TestHighlighter(Container<Drawable> source)
                : base(source)
            {
            }

            protected override HighlightLine CreateHighlightLine() => new TestHighlightLine();

            public class TestHighlightLine : HighlightLine
            {
                public TestHighlightLine()
                {
                    InternalChild = new Box
                    {
                        Blending = new BlendingParameters
                        {
                            Source = BlendingType.SrcAlpha,
                            Destination = BlendingType.One,
                            SourceAlpha = BlendingType.One,
                            DestinationAlpha = BlendingType.One,
                            RGBEquation = BlendingEquation.Subtract,
                            AlphaEquation = BlendingEquation.Subtract,
                        },
                        Colour = Color4.LightBlue,
                        RelativeSizeAxes = Axes.Both,
                    };
                }

                public override void Reset()
                {
                    Alpha = 0;
                    Position = Vector2.Zero;
                    Size = Vector2.Zero;
                }

                public override void DisplayAt(Vector2 position, Vector2 size)
                {
                    Alpha = 1;
                    Position = position;
                    Size = size;
                }
            }
        }
    }
}
