// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace osu.Framework.Lists
{
    /// <summary>
    /// Wraps an array and provides a custom indexer with element change notification
    /// </summary>
    /// <typeparam name="T">An array data type</typeparam>
    public class ElementChangeNotifierArray<T> : IReadOnlyList<T>, IEquatable<ElementChangeNotifierArray<T>>, ElementChangeNotifierArray<T>.INotifyElementChanged
    {
        [NotNull]
        private readonly T[] source;

        public event Action ElementChanged;

        public int Count => ((IReadOnlyCollection<T>)source).Count;

        public T this[int index]
        {
            get => source[index];
            set
            {
                if (EqualityComparer<T>.Default.Equals(source[index], value))
                    return;

                var lastValue = source[index];
                if (lastValue is INotifyElementChanged lastNotifier)
                    lastNotifier.ElementChanged -= onElementChange;

                source[index] = value;
                if (value is INotifyElementChanged notifier)
                    notifier.ElementChanged += onElementChange;

                onElementChange();
            }
        }

        public ElementChangeNotifierArray([NotNull] T[] source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public static implicit operator ElementChangeNotifierArray<T>(T[] source)
        {
            if (source == null)
                return null;

            return new ElementChangeNotifierArray<T>(source);
        }

        private void onElementChange() => ElementChanged?.Invoke();

        #region Other interface implementations

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)source).GetEnumerator();
        }

        int IReadOnlyCollection<T>.Count => ((IReadOnlyCollection<T>)source).Count;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Equality implementation

        public bool Equals(ElementChangeNotifierArray<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return source == other.source;
        }

        public override bool Equals(object obj) => obj is ElementChangeNotifierArray<T> other && Equals(other);

        public static bool operator ==(ElementChangeNotifierArray<T> left, ElementChangeNotifierArray<T> right) => left?.source == right?.source;
        public static bool operator !=(ElementChangeNotifierArray<T> left, ElementChangeNotifierArray<T> right) => !(left == right);

        public override int GetHashCode() => source.GetHashCode();

        #endregion

        private interface INotifyElementChanged
        {
            event Action ElementChanged;
        }
    }
}
