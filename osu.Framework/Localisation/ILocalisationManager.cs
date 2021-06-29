// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// Provides read-only access to the <see cref="LocalisationManager"/>.
    /// </summary>
    public interface ILocalisationManager
    {
        /// <summary>
        /// Whether unicode (i.e. original) strings are preferred over romanised strings.
        /// </summary>
        IBindable<bool> PreferUnicode { get; }
    }
}
