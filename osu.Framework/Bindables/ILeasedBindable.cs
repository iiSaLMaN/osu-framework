// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Bindables
{
    /// <summary>
    /// An <see cref="IBindable"/> with properties and methods of the leased bindable.
    /// </summary>
    public interface ILeasedBindable : IBindable
    {
        /// <summary>
        /// Whether this <see cref="ILeasedBindable"/> is valid for use.
        /// </summary>
        bool IsValid { get; }
    }

    /// <summary>
    /// An <see cref="IBindable{T}"/> with properties and methods of the leased bindable.
    /// </summary>
    public interface ILeasedBindable<T> : ILeasedBindable, IBindable<T>
    {
    }
}
