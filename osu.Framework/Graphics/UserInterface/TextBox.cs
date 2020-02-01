// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Utils;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Platform;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class TextBox : TabbableContainer, IHasCurrentValue<string>, IKeyBindingHandler<PlatformAction>
    {
        protected Container TextContainer { get; private set; }
        protected Caret Caret { get; private set; }
        protected Highlighter Highlighter { get; private set; }
        protected FillFlowContainer TextFlow { get; private set; }

        public override bool HandleNonPositionalInput => HasFocus;

        /// <summary>
        /// Whether this TextBox should accept left and right arrow keys for navigation.
        /// </summary>
        public virtual bool HandleLeftRightArrows => true;

        /// <summary>
        /// Padding to be used within the TextContainer. Requires special handling due to the sideways scrolling of text content.
        /// </summary>
        protected virtual float LeftRightPadding => 5;

        /// <summary>
        /// Whether clipboard copying functionality is allowed.
        /// </summary>
        protected virtual bool AllowClipboardExport => true;

        /// <summary>
        /// Whether seeking to word boundaries is allowed.
        /// </summary>
        protected virtual bool AllowWordNavigation => true;

        //represents the left/right selection coordinates of the word double clicked on when dragging
        private int[] doubleClickWord;

        public int? LengthLimit;

        [Resolved]
        private AudioManager audio { get; set; }

        /// <summary>
        /// Check if a character can be added to this TextBox.
        /// </summary>
        /// <param name="character">The pending character.</param>
        /// <returns>Whether the character is allowed to be added.</returns>
        protected virtual bool CanAddCharacter(char character) => !char.IsControl(character);

        public bool ReadOnly;

        /// <summary>
        /// Whether the textbox should rescind focus on commit.
        /// </summary>
        public bool ReleaseFocusOnCommit { get; set; } = true;

        /// <summary>
        /// Whether a commit should be triggered whenever the textbox loses focus.
        /// </summary>
        public bool CommitOnFocusLost { get; set; }

        public override bool CanBeTabbedTo => !ReadOnly;

        private ITextInputSource textInput;

        private Clipboard clipboard;

        public delegate void OnCommitHandler(TextBox sender, bool newText);

        public OnCommitHandler OnCommit;

        private readonly Scheduler textUpdateScheduler = new Scheduler();

        protected TextBox()
        {
            Masking = true;

            Child = CreateTextContainerSubTree();
            TextContainer.Children = new Drawable[]
            {
                Placeholder = CreatePlaceholder(),
                Caret = CreateCaret(),
                TextFlow = CreateTextFlowContainer(),
                Highlighter = CreateSelectionHighlighter(TextFlow),
            };

            Current.ValueChanged += e => { Text = e.NewValue; };
            Caret.Hide();
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            textInput = host.GetTextInput();
            clipboard = host.GetClipboard();

            if (textInput != null)
            {
                textInput.OnNewImeComposition += s =>
                {
                    textUpdateScheduler.Add(() => onImeComposition(s));
                    CursorAndLayout.Invalidate();
                };
                textInput.OnNewImeResult += s =>
                {
                    textUpdateScheduler.Add(onImeResult);
                    CursorAndLayout.Invalidate();
                };
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            textUpdateScheduler.SetCurrentThread(MainThread);
        }

        public virtual bool OnPressed(PlatformAction action)
        {
            int? amount = null;

            if (!HasFocus)
                return false;

            if (!HandleLeftRightArrows &&
                action.ActionMethod == PlatformActionMethod.Move &&
                (action.ActionType == PlatformActionType.CharNext || action.ActionType == PlatformActionType.CharPrevious))
                return false;

            switch (action.ActionType)
            {
                // Clipboard
                case PlatformActionType.Cut:
                case PlatformActionType.Copy:
                    if (string.IsNullOrEmpty(SelectedText) || !AllowClipboardExport) return true;

                    clipboard?.SetText(SelectedText);
                    if (action.ActionType == PlatformActionType.Cut)
                        RemoveCharacterOrSelection();
                    return true;

                case PlatformActionType.Paste:
                    //the text may get pasted into the hidden textbox, so we don't need any direct clipboard interaction here.
                    string pending = textInput?.GetPendingText();

                    if (string.IsNullOrEmpty(pending))
                        pending = clipboard?.GetText();

                    InsertString(pending);
                    return true;

                case PlatformActionType.SelectAll:
                    SelectionStart = 0;
                    SelectionEnd = text.Length;
                    CursorAndLayout.Invalidate();
                    return true;

                // Cursor Manipulation
                case PlatformActionType.CharNext:
                    amount = 1;
                    break;

                case PlatformActionType.CharPrevious:
                    amount = -1;
                    break;

                case PlatformActionType.LineEnd:
                    amount = text.Length;
                    break;

                case PlatformActionType.LineStart:
                    amount = -text.Length;
                    break;

                case PlatformActionType.WordNext:
                    if (!AllowWordNavigation)
                        amount = 1;
                    else
                    {
                        int searchNext = Math.Clamp(SelectionEnd, 0, Math.Max(0, Text.Length - 1));
                        while (searchNext < Text.Length && text[searchNext] == ' ')
                            searchNext++;
                        int nextSpace = text.IndexOf(' ', searchNext);
                        amount = (nextSpace >= 0 ? nextSpace : text.Length) - SelectionEnd;
                    }

                    break;

                case PlatformActionType.WordPrevious:
                    if (!AllowWordNavigation)
                        amount = -1;
                    else
                    {
                        int searchPrev = Math.Clamp(SelectionEnd - 2, 0, Math.Max(0, Text.Length - 1));
                        while (searchPrev > 0 && text[searchPrev] == ' ')
                            searchPrev--;
                        int lastSpace = text.LastIndexOf(' ', searchPrev);
                        amount = lastSpace > 0 ? -(SelectionEnd - lastSpace - 1) : -SelectionEnd;
                    }

                    break;
            }

            if (amount.HasValue)
            {
                switch (action.ActionMethod)
                {
                    case PlatformActionMethod.Move:
                        ResetSelection();
                        MoveSelection(amount.Value, false);
                        break;

                    case PlatformActionMethod.Select:
                        MoveSelection(amount.Value, true);
                        break;

                    case PlatformActionMethod.Delete:
                        if (SelectionLength == 0)
                            SelectionEnd = Math.Clamp(SelectionStart + amount.Value, 0, text.Length);
                        if (SelectionLength > 0)
                            RemoveCharacterOrSelection();
                        break;
                }

                return true;
            }

            return false;
        }

        public virtual void OnReleased(PlatformAction action)
        {
        }

        internal override void UpdateClock(IFrameBasedClock clock)
        {
            base.UpdateClock(clock);
            textUpdateScheduler.UpdateClock(Clock);
        }

        /// <summary>
        /// Resets selection positions.
        /// </summary>
        protected void ResetSelection()
        {
            SelectionStart = SelectionEnd;
            CursorAndLayout.Invalidate();
        }

        protected override void Dispose(bool isDisposing)
        {
            OnCommit = null;

            unbindInput();

            base.Dispose(isDisposing);
        }

        private float textContainerPosX;

        private string textAtLastLayout = string.Empty;

        private float getPositionAt(int index)
        {
            if (index > 0)
            {
                if (index < text.Length)
                    return TextFlow[index].DrawPosition.X + TextFlow.DrawPosition.X;

                var d = TextFlow[index - 1];
                return d.DrawPosition.X + d.DrawSize.X + TextFlow.Spacing.X + TextFlow.DrawPosition.X;
            }

            return 0;
        }

        /// <summary>
        /// Computes the caret position at a specified index.
        /// </summary>
        /// <param name="caretIndex">The caret index.</param>
        protected virtual Vector2 ComputeCaretPositionAt(int caretIndex)
            => new Vector2(text.Length == 0 ? 0 : getPositionAt(caretIndex), 0);

        protected virtual void UpdateCursorAndLayout()
        {
            Placeholder.Font = Placeholder.Font.With(size: TextSize);

            textUpdateScheduler.Update();

            float cursorPosEnd = getPositionAt(SelectionEnd);

            float cursorRelativePositionAxesInBox = (cursorPosEnd - textContainerPosX) / DrawWidth;

            //we only want to reposition the view when the cursor reaches near the extremities.
            if (cursorRelativePositionAxesInBox < 0.1 || cursorRelativePositionAxesInBox > 0.9)
            {
                textContainerPosX = cursorPosEnd - DrawWidth / 2 + LeftRightPadding * 2;
            }

            textContainerPosX = Math.Clamp(textContainerPosX, 0, Math.Max(0, TextFlow.DrawWidth - DrawWidth + LeftRightPadding * 2));

            TextContainer.MoveToX(LeftRightPadding - textContainerPosX, 300, Easing.OutExpo);

            if (HasFocus)
            {
                if (SelectionLength > 0)
                {
                    Caret.Hide();
                    Highlighter.HighlightFrom(SelectionLeft, SelectionLength);
                }
                else
                {
                    Highlighter.RemoveHighlight();
                    Caret.DisplayAt(ComputeCaretPositionAt(SelectionEnd));
                }
            }

            if (textAtLastLayout != text)
                Current.Value = text;

            if (textAtLastLayout.Length == 0 || text.Length == 0)
            {
                if (text.Length == 0)
                    Placeholder.Show();
                else
                    Placeholder.Hide();
            }

            textAtLastLayout = text;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            //have to run this after children flow
            if (!CursorAndLayout.IsValid)
            {
                UpdateCursorAndLayout();
                CursorAndLayout.Validate();
            }
        }

        private int getCharacterClosestTo(Vector2 pos)
        {
            pos = Parent.ToSpaceOfOtherDrawable(pos, TextFlow);

            int i = 0;

            foreach (Drawable d in TextFlow.Children)
            {
                if (d.DrawPosition.X + d.DrawSize.X / 2 > pos.X)
                    break;

                i++;
            }

            return i;
        }

        protected int SelectionStart;
        protected int SelectionEnd;

        protected int SelectionLength => Math.Abs(SelectionEnd - SelectionStart);

        protected int SelectionLeft => Math.Min(SelectionStart, SelectionEnd);
        protected int SelectionRight => Math.Max(SelectionStart, SelectionEnd);

        protected readonly Cached CursorAndLayout = new Cached();

        protected void MoveSelection(int offset, bool expand)
        {
            if (textInput?.ImeActive == true) return;

            int oldStart = SelectionStart;
            int oldEnd = SelectionEnd;

            if (expand)
                SelectionEnd = Math.Clamp(SelectionEnd + offset, 0, text.Length);
            else
            {
                if (SelectionLength > 0 && Math.Abs(offset) <= 1)
                {
                    //we don't want to move the location when "removing" an existing selection, just set the new location.
                    if (offset > 0)
                        SelectionEnd = SelectionStart = SelectionRight;
                    else
                        SelectionEnd = SelectionStart = SelectionLeft;
                }
                else
                    SelectionEnd = SelectionStart = Math.Clamp((offset > 0 ? SelectionRight : SelectionLeft) + offset, 0, text.Length);
            }

            if (oldStart != SelectionStart || oldEnd != SelectionEnd)
            {
                audio.Samples.Get(@"Keyboard/key-movement")?.Play();
                CursorAndLayout.Invalidate();
            }
        }

        protected bool RemoveCharacterOrSelection(bool sound = true)
        {
            if (Current.Disabled)
                return false;

            if (text.Length == 0) return false;
            if (SelectionLength == 0 && SelectionLeft == 0) return false;

            int count = Math.Clamp(SelectionLength, 1, text.Length);
            int start = Math.Clamp(SelectionLength > 0 ? SelectionLeft : SelectionLeft - 1, 0, text.Length - count);

            if (count == 0) return false;

            if (sound)
                audio.Samples.Get(@"Keyboard/key-delete")?.Play();

            foreach (var d in TextFlow.Children.Skip(start).Take(count).ToArray()) //ToArray since we are removing items from the children in this block.
            {
                TextFlow.Remove(d);

                TextContainer.Add(d);

                // account for potentially altered height of textbox
                d.Y += TextFlow.BoundingBox.Y;

                d.Hide();
                d.Expire();
            }

            text = text.Remove(start, count);

            // Reorder characters depth after removal to avoid ordering issues with newly added characters.
            for (int i = start; i < TextFlow.Count; i++)
                TextFlow.ChangeChildDepth(TextFlow[i], getDepthForCharacterIndex(i));

            if (SelectionLength > 0)
                SelectionStart = SelectionEnd = SelectionLeft;
            else
                SelectionStart = SelectionEnd = SelectionLeft - 1;

            CursorAndLayout.Invalidate();
            return true;
        }

        /// <summary>
        /// Creates a single character. Override <see cref="Drawable.Show"/> and <see cref="Drawable.Hide"/> for custom behavior.
        /// </summary>
        /// <param name="c">The character that this <see cref="Drawable"/> should represent.</param>
        /// <returns>A <see cref="Drawable"/> that represents the character <paramref name="c"/> </returns>
        protected virtual Drawable GetDrawableCharacter(char c) => new SpriteText { Text = c.ToString(), Font = new FontUsage(size: TextSize) };

        protected virtual Drawable AddCharacterToFlow(char c)
        {
            // Remove all characters to the right and store them in a local list,
            // such that their depth can be updated.
            List<Drawable> charsRight = new List<Drawable>();
            foreach (Drawable d in TextFlow.Children.Skip(SelectionLeft))
                charsRight.Add(d);
            TextFlow.RemoveRange(charsRight);

            // Update their depth to make room for the to-be inserted character.
            int i = SelectionLeft;
            foreach (Drawable d in charsRight)
                d.Depth = getDepthForCharacterIndex(i++);

            // Add the character
            Drawable ch = GetDrawableCharacter(c);
            ch.Depth = getDepthForCharacterIndex(SelectionLeft);

            TextFlow.Add(ch);

            // Add back all the previously removed characters
            TextFlow.AddRange(charsRight);

            return ch;
        }

        private float getDepthForCharacterIndex(int index) => -index;

        protected virtual float TextSize => TextFlow.DrawSize.Y - (TextFlow.Padding.Top + TextFlow.Padding.Bottom);

        /// <summary>
        /// Insert an arbitrary string into the text at the current position.
        /// </summary>
        /// <param name="text">The new text to insert.</param>
        protected void InsertString(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            foreach (char c in text)
                InsertCharacter(c);
        }

        /// <summary>
        /// Insert a character into the text at the current position.
        /// </summary>
        /// <param name="c">The character to insert.</param>
        protected void InsertCharacter(char c)
        {
            var ch = addCharacter(c);

            if (ch == null)
            {
                NotifyInputError();
                return;
            }

            ch.Show();
        }

        private Drawable addCharacter(char c)
        {
            if (Current.Disabled || !CanAddCharacter(c))
                return null;

            if (SelectionLength > 0)
                RemoveCharacterOrSelection();

            if (text.Length + 1 > LengthLimit)
            {
                NotifyInputError();
                return null;
            }

            Drawable ch = AddCharacterToFlow(c);

            text = text.Insert(SelectionLeft, c.ToString());
            SelectionStart = SelectionEnd = SelectionLeft + 1;

            CursorAndLayout.Invalidate();

            return ch;
        }

        /// <summary>
        /// Called whenever an invalid character has been entered
        /// </summary>
        protected abstract void NotifyInputError();

        /// <summary>
        /// Creates the text container sub-tree.
        /// This should contain all the components required for this <see cref="TextBox"/> to function correctly.
        /// </summary>
        /// <returns>A container that contains the sub-tree to be added directly to this <see cref="TextBox"/>.</returns>
        protected virtual Container<Drawable> CreateTextContainerSubTree() => TextContainer = CreateTextContainer();

        protected virtual Container CreateTextContainer() => new Container
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            AutoSizeAxes = Axes.X,
            RelativeSizeAxes = Axes.Y,
            X = LeftRightPadding,
        };

        protected virtual FillFlowContainer CreateTextFlowContainer() => new FillFlowContainer
        {
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            AutoSizeAxes = Axes.X,
            RelativeSizeAxes = Axes.Y,
            Direction = FillDirection.Horizontal,
        };

        /// <summary>
        /// Creates a placeholder that shows whenever the textbox is empty. Override <see cref="Drawable.Show"/> or <see cref="Drawable.Hide"/> for custom behavior.
        /// </summary>
        /// <returns>The placeholder</returns>
        protected abstract SpriteText CreatePlaceholder();

        protected SpriteText Placeholder;

        public string PlaceholderText
        {
            get => Placeholder.Text;
            set => Placeholder.Text = value;
        }

        protected abstract Caret CreateCaret();

        protected abstract Highlighter CreateSelectionHighlighter(FillFlowContainer textFlow);

        private readonly BindableWithCurrent<string> current = new BindableWithCurrent<string>();

        public Bindable<string> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private string text = string.Empty;

        public virtual string Text
        {
            get => text;
            set
            {
                if (Current.Disabled)
                    return;

                if (value == text)
                    return;

                lastCommitText = value ??= string.Empty;

                if (value.Length == 0)
                    Placeholder.Show();
                else
                    Placeholder.Hide();

                if (!IsLoaded)
                    Current.Value = text = value;

                textUpdateScheduler.Add(delegate
                {
                    int startBefore = SelectionStart;
                    SelectionStart = SelectionEnd = 0;
                    TextFlow?.Clear();
                    text = string.Empty;

                    foreach (char c in value)
                        addCharacter(c);

                    SelectionStart = Math.Clamp(startBefore, 0, text.Length);
                });

                CursorAndLayout.Invalidate();
            }
        }

        public string SelectedText => SelectionLength > 0 ? Text.Substring(SelectionLeft, SelectionLength) : string.Empty;

        private bool consumingText;

        /// <summary>
        /// Begin consuming text from an <see cref="ITextInputSource"/>.
        /// Continues to consume every <see cref="Drawable.Update"/> loop until <see cref="EndConsumingText"/> is called.
        /// </summary>
        protected void BeginConsumingText()
        {
            consumingText = true;
            Schedule(consumePendingText);
        }

        /// <summary>
        /// Stops consuming text from an <see cref="ITextInputSource"/>.
        /// </summary>
        protected void EndConsumingText()
        {
            consumingText = false;
        }

        /// <summary>
        /// Consumes any pending characters and adds them to the textbox if not <see cref="ReadOnly"/>.
        /// </summary>
        /// <returns>Whether any characters were consumed.</returns>
        private void consumePendingText()
        {
            string pendingText = textInput?.GetPendingText();

            if (!string.IsNullOrEmpty(pendingText) && !ReadOnly)
            {
                if (pendingText.Any(char.IsUpper))
                    audio.Samples.Get(@"Keyboard/key-caps")?.Play();
                else
                    audio.Samples.Get($@"Keyboard/key-press-{RNG.Next(1, 5)}")?.Play();

                InsertString(pendingText);
            }

            if (consumingText)
                Schedule(consumePendingText);
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (textInput?.ImeActive == true || ReadOnly) return true;

            if (e.ControlPressed || e.SuperPressed || e.AltPressed)
                return false;

            // we only care about keys which can result in text output.
            if (keyProducesCharacter(e.Key))
                BeginConsumingText();

            switch (e.Key)
            {
                case Key.Escape:
                    KillFocus();
                    return true;

                case Key.KeypadEnter:
                case Key.Enter:
                    if (!e.ShiftPressed)
                    {
                        Commit();
                        return true;
                    }

                    break;
            }

            return base.OnKeyDown(e) || consumingText;
        }

        private bool keyProducesCharacter(Key key) => (key == Key.Space || key >= Key.Keypad0 && key <= Key.NonUSBackSlash) && key != Key.KeypadEnter;

        /// <summary>
        /// Removes focus from this <see cref="TextBox"/> if it currently has focus.
        /// </summary>
        protected virtual void KillFocus() => killFocus();

        private string lastCommitText;

        private bool hasNewComittableText => text != lastCommitText;

        private void killFocus()
        {
            var manager = GetContainingInputManager();
            if (manager.FocusedDrawable == this)
                manager.ChangeFocus(null);
        }

        protected virtual void Commit()
        {
            if (ReleaseFocusOnCommit && HasFocus)
            {
                killFocus();
                if (CommitOnFocusLost)
                    // the commit will happen as a result of the focus loss.
                    return;
            }

            audio.Samples.Get(@"Keyboard/key-confirm")?.Play();

            OnCommit?.Invoke(this, hasNewComittableText);
            lastCommitText = text;
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            if (!e.HasAnyKeyPressed)
                EndConsumingText();

            base.OnKeyUp(e);
        }

        protected override void OnDrag(DragEvent e)
        {
            //if (textInput?.ImeActive == true) return true;

            if (doubleClickWord != null)
            {
                //select words at a time
                if (getCharacterClosestTo(e.MousePosition) > doubleClickWord[1])
                {
                    SelectionStart = doubleClickWord[0];
                    SelectionEnd = findSeparatorIndex(text, getCharacterClosestTo(e.MousePosition) - 1, 1);
                    SelectionEnd = SelectionEnd >= 0 ? SelectionEnd : text.Length;
                }
                else if (getCharacterClosestTo(e.MousePosition) < doubleClickWord[0])
                {
                    SelectionStart = doubleClickWord[1];
                    SelectionEnd = findSeparatorIndex(text, getCharacterClosestTo(e.MousePosition), -1);
                    SelectionEnd = SelectionEnd >= 0 ? SelectionEnd + 1 : 0;
                }
                else
                {
                    //in the middle
                    SelectionStart = doubleClickWord[0];
                    SelectionEnd = doubleClickWord[1];
                }

                CursorAndLayout.Invalidate();
            }
            else
            {
                if (text.Length == 0) return;

                SelectionEnd = getCharacterClosestTo(e.MousePosition);
                if (SelectionLength > 0)
                    GetContainingInputManager().ChangeFocus(this);

                CursorAndLayout.Invalidate();
            }
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (HasFocus) return true;

            Vector2 posDiff = e.MouseDownPosition - e.MousePosition;

            return Math.Abs(posDiff.X) > Math.Abs(posDiff.Y);
        }

        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            if (textInput?.ImeActive == true) return true;

            if (text.Length == 0) return true;

            if (AllowClipboardExport)
            {
                int hover = Math.Min(text.Length - 1, getCharacterClosestTo(e.MousePosition));

                int lastSeparator = findSeparatorIndex(text, hover, -1);
                int nextSeparator = findSeparatorIndex(text, hover, 1);

                SelectionStart = lastSeparator >= 0 ? lastSeparator + 1 : 0;
                SelectionEnd = nextSeparator >= 0 ? nextSeparator : text.Length;
            }
            else
            {
                SelectionStart = 0;
                SelectionEnd = text.Length;
            }

            //in order to keep the home word selected
            doubleClickWord = new[] { SelectionStart, SelectionEnd };

            CursorAndLayout.Invalidate();
            return true;
        }

        private static int findSeparatorIndex(string input, int searchPos, int direction)
        {
            bool isLetterOrDigit = char.IsLetterOrDigit(input[searchPos]);

            for (int i = searchPos; i >= 0 && i < input.Length; i += direction)
            {
                if (char.IsLetterOrDigit(input[i]) != isLetterOrDigit)
                    return i;
            }

            return -1;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (textInput?.ImeActive == true) return true;

            SelectionStart = SelectionEnd = getCharacterClosestTo(e.MousePosition);

            CursorAndLayout.Invalidate();

            return false;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            doubleClickWord = null;
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            unbindInput();

            Caret.Hide();
            CursorAndLayout.Invalidate();

            if (CommitOnFocusLost)
                Commit();
        }

        public override bool AcceptsFocus => true;

        protected override bool OnClick(ClickEvent e) => !ReadOnly;

        protected override void OnFocus(FocusEvent e)
        {
            bindInput();

            Caret.Show();
            CursorAndLayout.Invalidate();
        }

        #region Native TextBox handling (winform specific)

        private void unbindInput()
        {
            textInput?.Deactivate(this);
        }

        private void bindInput()
        {
            textInput?.Activate(this);
        }

        private void onImeResult()
        {
            //we only succeeded if there is pending data in the textbox
            if (imeDrawables.Count > 0)
            {
                foreach (Drawable d in imeDrawables)
                {
                    d.Colour = Color4.White;
                    d.FadeTo(1, 200, Easing.Out);
                }
            }

            imeDrawables.Clear();
        }

        private readonly List<Drawable> imeDrawables = new List<Drawable>();

        private void onImeComposition(string s)
        {
            //search for unchanged characters..
            int matchCount = 0;
            bool matching = true;
            bool didDelete = false;

            int searchStart = text.Length - imeDrawables.Count;

            //we want to keep processing to the end of the longest string (the current displayed or the new composition).
            int maxLength = Math.Max(imeDrawables.Count, s.Length);

            for (int i = 0; i < maxLength; i++)
            {
                if (matching && searchStart + i < text.Length && i < s.Length && text[searchStart + i] == s[i])
                {
                    matchCount = i + 1;
                    continue;
                }

                matching = false;

                if (matchCount < imeDrawables.Count)
                {
                    //if we are no longer matching, we want to remove all further characters.
                    RemoveCharacterOrSelection(false);
                    imeDrawables.RemoveAt(matchCount);
                    didDelete = true;
                }
            }

            if (matchCount == s.Length)
            {
                //in the case of backspacing (or a NOP), we can exit early here.
                if (didDelete)
                    audio.Samples.Get(@"Keyboard/key-delete")?.Play();
                return;
            }

            //add any new or changed characters
            for (int i = matchCount; i < s.Length; i++)
            {
                Drawable dr = addCharacter(s[i]);

                if (dr != null)
                {
                    dr.Colour = Color4.Aqua;
                    dr.Alpha = 0.6f;
                    imeDrawables.Add(dr);
                }
            }

            audio.Samples.Get($@"Keyboard/key-press-{RNG.Next(1, 5)}")?.Play();
        }

        #endregion
    }
}
