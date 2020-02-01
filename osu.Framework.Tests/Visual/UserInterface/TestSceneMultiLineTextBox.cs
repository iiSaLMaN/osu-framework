// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneMultilineTextBox : FrameworkTestScene
    {
        public TestSceneMultilineTextBox()
        {
            Child = new BasicMultilineTextBox
            {
                Size = new Vector2(200, 120),
            };
        }
    }
}
