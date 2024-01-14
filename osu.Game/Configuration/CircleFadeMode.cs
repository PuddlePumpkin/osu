// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Configuration
{
    public enum CircleFadeMode
    {
        [LocalisableDescription(typeof(GameplaySettingsStrings), nameof(GameplaySettingsStrings.DefaultSpeed))]
        Default,

        [LocalisableDescription(typeof(GameplaySettingsStrings), nameof(GameplaySettingsStrings.FastSpeed))]
        Fast,

        [LocalisableDescription(typeof(GameplaySettingsStrings), nameof(GameplaySettingsStrings.InstantSpeed))]
        Instant,
    }
}
