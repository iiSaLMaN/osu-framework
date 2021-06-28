// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System;
using System.Globalization;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A string that has a reloadable function accepting any <see cref="CultureInfo"/> and returning a string using that.
    /// </summary>
    public class CulturedString : IEquatable<CulturedString>
    {
        private readonly Func<CultureInfo, string> getString;

        public CulturedString(Func<CultureInfo, string> getString)
        {
            this.getString = getString;
        }

        /// <summary>
        /// Returns the string with a specified <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The culture info</param>
        /// <returns></returns>
        public string GetOnCulture(CultureInfo? culture)
        {
            if (culture == null)
                return ToString();

            return getString(culture);
        }

        public override string ToString() => getString(CultureInfo.InvariantCulture);

        public bool Equals(CulturedString? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return getString == other.getString;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((CulturedString)obj);
        }

        public override int GetHashCode() => getString.GetHashCode();
    }
}
