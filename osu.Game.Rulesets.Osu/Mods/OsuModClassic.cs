// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModClassic : ModClassic, IApplicableToHitObject, IApplicableToDrawableHitObject, IApplicableToDrawableRuleset<OsuHitObject>
    {
        public override Type[] IncompatibleMods => base.IncompatibleMods.Append(typeof(OsuModStrictTracking)).ToArray();

        [SettingSource("No slider head accuracy requirement", "Scores sliders proportionally to the number of ticks hit.")]
        public Bindable<bool> NoSliderHeadAccuracy { get; } = new BindableBool(true);

        [SettingSource("No slider head movement", "Pins slider heads at their starting position, regardless of time.")]
        public Bindable<bool> NoSliderHeadMovement { get; } = new BindableBool(true);

        [SettingSource("Apply classic note lock", "Applies note lock to the full hit window.")]
        public Bindable<bool> ClassicNoteLock { get; } = new BindableBool(true);

        [SettingSource("Always play a slider's tail sample", "Always plays a slider's tail sample regardless of whether it was hit or not.")]
        public Bindable<bool> AlwaysPlayTailSample { get; } = new BindableBool(true);

        [SettingSource("Fade out hit circles earlier", "Make hit circles fade out into a miss, rather than after it.")]
        public Bindable<bool> FadeHitCircleEarly { get; } = new Bindable<bool>(true);

        public void ApplyToHitObject(HitObject hitObject)
        {
            switch (hitObject)
            {
                case Slider slider:
                    slider.OnlyJudgeNestedObjects = !NoSliderHeadAccuracy.Value;

                    foreach (var head in slider.NestedHitObjects.OfType<SliderHeadCircle>())
                        head.JudgeAsNormalHitCircle = !NoSliderHeadAccuracy.Value;

                    break;
            }
        }

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            var osuRuleset = (DrawableOsuRuleset)drawableRuleset;

            if (ClassicNoteLock.Value)
                osuRuleset.Playfield.HitPolicy = new ObjectOrderedHitPolicy();
        }

        public void ApplyToDrawableHitObject(DrawableHitObject obj)
        {
            switch (obj)
            {
                case DrawableSliderHead head:
                    head.TrackFollowCircle = !NoSliderHeadMovement.Value;
                    if (FadeHitCircleEarly.Value)
                        applyEarlyFading(head);
                    break;

                case DrawableSliderTail tail:
                    tail.SamplePlaysOnlyOnHit = !AlwaysPlayTailSample.Value;
                    break;

                case DrawableHitCircle circle:
                    if (FadeHitCircleEarly.Value)
                        applyEarlyFading(circle);
                    break;
            }
        }

        private void applyEarlyFading(DrawableHitCircle circle)
        {
            circle.ApplyCustomUpdateState += (o, _) =>
            {
                using (o.BeginAbsoluteSequence(o.StateUpdateTime))
                {
                    double mehWindow = o.HitObject.HitWindows.WindowFor(HitResult.Meh);
                    double lateMissFadeTime = mehWindow / 4 + 15;
                    o.Delay(mehWindow - lateMissFadeTime).FadeOut(lateMissFadeTime);
                }
            };
        }
    }
}
