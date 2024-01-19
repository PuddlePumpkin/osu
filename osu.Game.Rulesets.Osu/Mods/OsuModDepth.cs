// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModDepth : ModWithVisibilityAdjustment, IUpdatableByPlayfield, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableToDifficulty
    {

        public override string Name => "Depth";
        public override string Acronym => "DP";
        public override IconUsage? Icon => FontAwesome.Solid.Cube;
        public override ModType Type => ModType.Fun;
        public override LocalisableString Description => "3D. Almost.";
        public override double ScoreMultiplier => 1;
        public override Type[] IncompatibleMods => base.IncompatibleMods.Concat(new[] { typeof(OsuModMagnetised), typeof(OsuModRepel), typeof(OsuModFreezeFrame), typeof(ModWithVisibilityAdjustment), typeof(IRequiresApproachCircles) }).ToArray();

        private static readonly Vector3 camera_position = new Vector3(OsuPlayfield.BASE_SIZE.X * 0.5f, OsuPlayfield.BASE_SIZE.Y * 0.5f, -200);
        private readonly float sliderMinDepth = depthForScale(1.5f); // Depth at which slider's scale will be 1.5f

        [SettingSource("Scroll Speed", "How fast to scroll.", 0)]
        public BindableFloat ScrollSpeed { get; } = new BindableFloat(5)
        {
            Precision = 0.5f,
            MinValue = 1,
            MaxValue = 10
        };

        [SettingSource("Parralax Amount", "Ratio of cursor to circle motion.", 0)]
        public BindableFloat ParaAmount { get; } = new BindableFloat(0.3f)
        {
            Precision = 0.1f,
            MinValue = 0,
            MaxValue = 1
        };

        [SettingSource("Circle Opacity", "How opaque should objects be.", 0)]
        public BindableFloat CircleOpacity { get; } = new BindableFloat(85)
        {
            Precision = 5,
            MinValue = 10,
            MaxValue = 100
        };

        [SettingSource("Show Judgements", "Whether judgements should be visible.", 1)]
        public BindableBool ShowJudgements { get; } = new BindableBool(false);

        protected override void ApplyIncreasedVisibilityState(DrawableHitObject hitObject, ArmedState state) => applyTransform(hitObject, state);

        protected override void ApplyNormalVisibilityState(DrawableHitObject hitObject, ArmedState state) => applyTransform(hitObject, state);

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            // Hide judgment displays and follow points as they won't make any sense.
            // Judgements can potentially be turned on in a future where they display at a position relative to their drawable counterpart.
            drawableRuleset.Playfield.DisplayJudgements.Value = ShowJudgements.Value;
            (drawableRuleset.Playfield as OsuPlayfield)?.FollowPoints.Hide();
        }

        private void applyTransform(DrawableHitObject drawable, ArmedState state)
        {
            drawable.Alpha = MapRange(CircleOpacity.Value, 10, 100, 0.1f, 1f);
            switch (drawable)
            {
                case DrawableHitCircle circle:
                    if (!false)
                    {
                        var hitObject = (OsuHitObject)drawable.HitObject;
                        double appearTime = hitObject.StartTime - hitObject.TimePreempt;

                        using (circle.BeginAbsoluteSequence(appearTime))
                            circle.ApproachCircle.Hide();
                    }

                    break;
            }
        }
        public Vector2 PlayfieldSize = new Vector2(0, 0);
        public Vector2 CursorPosition = new Vector2(0, 0);
        public void Update(Playfield playfield)
        {
            double time = playfield.Time.Current;
            PlayfieldSize = playfield.DrawSize;
            CursorPosition = playfield.Cursor.AsNonNull().ActiveCursor.DrawPosition;
            foreach (var drawable in playfield.HitObjectContainer.AliveObjects)
            {
                switch (drawable)
                {
                    case DrawableHitCircle circle:
                        processHitObject(time, circle);
                        break;

                    case DrawableSlider slider:
                        processSlider(time, slider);
                        break;
                }
            }
        }

        private void processHitObject(double time, DrawableOsuHitObject drawable)
        {
            var hitObject = drawable.HitObject;
            // Circles are always moving at the constant speed. They'll fade out before reaching the camera even at extreme conditions (AR 11, max depth).
            double speed = 2000 / hitObject.TimePreempt;
            double appearTime = hitObject.StartTime - hitObject.TimePreempt;
            float z = 2000 - (float)((Math.Max(time, appearTime) - appearTime) * speed);

            float scale = scaleForDepth(z);
            drawable.Position = toPlayfieldPosition(scale, hitObject.StackedPosition);
            drawable.Scale = new Vector2(scale);
        }
        public static float MapRange(float from, float fromMin, float fromMax, float toMin, float toMax)
        {
            float fromAbs = from - fromMin;
            float fromMaxAbs = fromMax - fromMin;

            float normal = fromAbs / fromMaxAbs;

            float toMaxAbs = toMax - toMin;
            float toAbs = toMaxAbs * normal;

            float to = toAbs + toMin;

            return to;
        }
        private void processSlider(double time, DrawableSlider drawableSlider)
        {
            var hitObject = drawableSlider.HitObject;

            double baseSpeed = 2000 / hitObject.TimePreempt;
            double appearTime = hitObject.StartTime - hitObject.TimePreempt;

            // Allow slider to move at a constant speed if its scale at the end time will be lower than 1.5f
            float zEnd = 2000 - (float)((Math.Max(hitObject.StartTime + hitObject.Duration, appearTime) - appearTime) * baseSpeed);

            if (zEnd > sliderMinDepth)
            {
                processHitObject(time, drawableSlider);
                return;
            }

            double offsetAfterStartTime = hitObject.Duration + 500;
            double slowSpeed = Math.Min(-sliderMinDepth / offsetAfterStartTime, baseSpeed);

            double decelerationTime = hitObject.TimePreempt * 0.05;
            float decelerationDistance = (float)(decelerationTime * (baseSpeed + slowSpeed) * 0.5);

            float z;

            if (time < hitObject.StartTime - decelerationTime)
            {
                float fullDistance = decelerationDistance + (float)(baseSpeed * (hitObject.TimePreempt - decelerationTime));
                z = fullDistance - (float)((Math.Max(time, appearTime) - appearTime) * baseSpeed);
            }
            else if (time < hitObject.StartTime)
            {
                double timeOffset = time - (hitObject.StartTime - decelerationTime);
                double deceleration = (slowSpeed - baseSpeed) / decelerationTime;
                z = decelerationDistance - (float)(baseSpeed * timeOffset + deceleration * timeOffset * timeOffset * 0.5);
            }
            else
            {
                double endTime = hitObject.StartTime + offsetAfterStartTime;
                z = -(float)((Math.Min(time, endTime) - hitObject.StartTime) * slowSpeed);
            }

            float scale = scaleForDepth(z);
            drawableSlider.Position = toPlayfieldPosition(scale, hitObject.StackedPosition);
            drawableSlider.Scale = new Vector2(scale);
        }

        private static float scaleForDepth(float depth) => -camera_position.Z / Math.Max(1f, depth - camera_position.Z);

        private static float depthForScale(float scale) => -camera_position.Z / scale + camera_position.Z;

        private Vector2 toPlayfieldPosition(float scale, Vector2 positionAtZeroDepth)
        {
            Vector2 CursorRelativePlayfieldPosition = CursorPosition - (PlayfieldSize / 2);
            return (positionAtZeroDepth - camera_position.Xy) * scale + camera_position.Xy + ((CursorRelativePlayfieldPosition * -ParaAmount.Value) * (new Vector2(scale, scale) / 1f));
        }

        public void ApplyToDifficulty(BeatmapDifficulty difficulty) => ApplySettings(difficulty);

        /// <summary>
        /// Apply all custom settings to the provided beatmap.
        /// </summary>
        /// <param name="difficulty">The beatmap to have settings applied.</param>
        protected virtual void ApplySettings(BeatmapDifficulty difficulty)
        {
            float ApproachRate = MapRange(ScrollSpeed.Value, 1, 10, -10, 10);
            difficulty.ApproachRate = ApproachRate;
        }


    }
}
