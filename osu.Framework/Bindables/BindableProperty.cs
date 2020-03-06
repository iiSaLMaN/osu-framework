// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Represents a property that is able to propagate value changes to a <typeparamref name="TBindable"/>'s bindings.
    /// </summary>
    /// <typeparam name="TBindable">The type of the <see cref="IBindable"/> of this property.</typeparam>
    /// <typeparam name="T">The type of the property this class holds.</typeparam>
    public class BindableProperty<T>
    {
        /// <summary>
        /// The bindable source of this property.
        /// </summary>
        public readonly IBindable Source;

        /// <summary>
        /// Gets a <see cref="BindableProperty{T}"/> equivalant to this property for propagating value to.
        /// </summary>
        public readonly Func<IBindable, BindableProperty<T>> GetPropertyOf;

        /// <summary>
        /// Invoked when a value has changed without any propagation rejection.
        /// Used for invoking value changed events.
        /// </summary>
        public Action<T, T> OnValueChange;

        private T value;

        public T Value
        {
            get => value;
            set
            {
                if (Source is ILeasedBindable lb && !lb.IsValid)
                    throw new InvalidOperationException($"Cannot mutate a property of an invalid {nameof(ILeasedBindable)}.");

                if (Source.Disabled && !(Source is ILeasedBindable))
                    throw new InvalidOperationException($"Cannot set value to \"{value.ToString()}\" as bindable is disabled.");

                if (EqualityComparer<T>.Default.Equals(this.value, value)) return;

                Set(this.value, value);
            }
        }

        public BindableProperty(IBindable source, Func<IBindable, BindableProperty<T>> getPropertyOf)
        {
            Source = source;
            GetPropertyOf = getPropertyOf;

            Debug.Assert(getPropertyOf.Invoke(source) == this);
        }

        public void Set(T oldValue, T newValue, IBindable source = null, bool bypassChecks = false)
        {
            value = newValue;
            TriggerChange(oldValue, source ?? Source, true, bypassChecks);
        }

        public void TriggerChange(T oldValue, IBindable source, bool allowPropagation = true, bool bypassChecks = false)
        {
            var beforePropagation = value;

            if (Source.Bindings != null && allowPropagation)
            {
                foreach (var b in Source.Bindings)
                {
                    if (ReferenceEquals(b, source)) continue;

                    GetPropertyOf(b)?.Set(oldValue, value, source, bypassChecks);
                }
            }

            if (EqualityComparer<T>.Default.Equals(beforePropagation, value))
                OnValueChange?.Invoke(oldValue, value);
        }
    }
}
