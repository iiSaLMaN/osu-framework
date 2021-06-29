// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#pragma warning disable 8632 // TODO: can be #nullable enable when Bindables are updated to also be.

using osu.Framework.Bindables;

namespace osu.Framework.Localisation
{
    public partial class LocalisationManager
    {
        private class LocalisedBindableString : Bindable<string>, ILocalisedBindableString
        {
            private readonly LocalisationManager manager;

            private readonly IBindable<ILocalisationStore?> storage = new Bindable<ILocalisationStore?>();
            private readonly IBindable<bool> preferUnicode = new Bindable<bool>();

            private LocalisableString text;

            public LocalisedBindableString(LocalisableString text, LocalisationManager manager)
            {
                this.text = text;
                this.manager = manager;

                storage.BindTo(manager.currentStorage);
                storage.BindValueChanged(_ => updateValue());

                preferUnicode.BindTo(manager.PreferUnicode);
                preferUnicode.BindValueChanged(_ => updateValue(), true);
            }

            private void updateValue()
            {
                switch (text.Data)
                {
                    case string plain:
                        Value = plain;
                        break;

                    case ILocalisableStringData data:
                        Value = data.GetLocalised(storage.Value, manager);
                        break;

                    default:
                        Value = string.Empty;
                        break;
                }
            }

            LocalisableString ILocalisedBindableString.Text
            {
                set
                {
                    if (text.Equals(value))
                        return;

                    text = value;

                    updateValue();
                }
            }
        }
    }
}
