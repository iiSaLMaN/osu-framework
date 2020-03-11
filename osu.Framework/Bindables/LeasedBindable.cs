// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// A bindable carrying a mutually exclusive lease on another bindable.
    /// Can only be retrieved via <see cref="Bindable{T}.BeginLease"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LeasedBindable<T> : Bindable<T>
    {
        private readonly Bindable<T> source;

        private readonly T valueBeforeLease;
        private readonly bool disabledBeforeLease;
        private readonly bool revertValueOnReturn;

        internal LeasedBindable([NotNull] Bindable<T> source, bool revertValueOnReturn)
        {
            BindTo(source);

            this.source = source ?? throw new ArgumentNullException(nameof(source));

            if (revertValueOnReturn)
            {
                this.revertValueOnReturn = true;
                valueBeforeLease = Value;
            }

            disabledBeforeLease = Disabled;

            Disabled = true;
        }

        public LeasedBindable(T value)
            : base(value)
        {
            // used for GetBoundCopy, where we don't want a source.
        }

        protected override void CheckPropertyValueChange<TValue>(IBindableProperty<TValue> property, TValue value)
        {
            if (source != null && hasBeenReturned)
                throw new InvalidOperationException($"Cannot perform operations on a {nameof(LeasedBindable<T>)} that has been {nameof(Return)}ed.");
        }

        private bool hasBeenReturned;

        /// <summary>
        /// End the lease on the source <see cref="Bindable{T}"/>.
        /// </summary>
        public void Return()
        {
            if (source == null)
                throw new InvalidOperationException($"Must {nameof(Return)} from original leased source");

            if (hasBeenReturned)
                throw new InvalidOperationException($"This bindable has already been {nameof(Return)}ed.");

            UnbindAll();
        }

        public override void UnbindAll()
        {
            if (source != null && !hasBeenReturned)
            {
                if (revertValueOnReturn)
                    Value = valueBeforeLease;

                Disabled = disabledBeforeLease;

                source.EndLease(this);
                hasBeenReturned = true;
            }

            base.UnbindAll();
        }
    }
}
