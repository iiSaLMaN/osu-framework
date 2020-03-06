// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Android.Views;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Android.Input
{
    public class AndroidTouchHandler : InputHandler
    {
        private readonly AndroidGameView view;

        public override bool IsActive => true;

        public override int Priority => 0;

        public AndroidTouchHandler(AndroidGameView view)
        {
            this.view = view;
            view.Touch += onTouch;
        }

        public override bool Initialize(GameHost host) => true;

        private int pointerIndexFor(MotionEventActions action) => (int)(((uint)action & (uint)MotionEventActions.PointerIndexMask) >> (int)MotionEventActions.PointerIndexShift);

        private void onTouch(object sender, View.TouchEventArgs e)
        {
            var pointers = new List<TouchPointer>(e.Event.PointerCount);

            for (int i = 0; i < e.Event.PointerCount; i++)
            {
                pointers.Add(new TouchPointer(
                        MouseButton.Touch1 + e.Event.GetPointerId(i),
                        new Vector2(e.Event.GetX(i) * view.ScaleX, e.Event.GetY(i) * view.ScaleY)
                    ));
            }

            switch (e.Event.Action & e.Event.ActionMasked)
            {
                case MotionEventActions.Move:
                case MotionEventActions.Down:
                    PendingInputs.Enqueue(new TouchPositionInput { Pointers = pointers });
                    PendingInputs.Enqueue(new TouchButtonInput(pointers.Select(p => new ButtonInputEntry<TouchPointer>(p, true))));
                    break;

                case MotionEventActions.Up:
                    PendingInputs.Enqueue(new TouchButtonInput(pointers.Select(p => new ButtonInputEntry<TouchPointer>(p, false))));
                    break;

                case MotionEventActions.PointerDown:
                    PendingInputs.Enqueue(new TouchButtonInput(pointers[pointerIndexFor(e.Event.Action)], true));
                    break;

                case MotionEventActions.PointerUp:
                    PendingInputs.Enqueue(new TouchButtonInput(pointers[pointerIndexFor(e.Event.Action)], false));
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            view.Touch -= onTouch;
        }
    }
}
