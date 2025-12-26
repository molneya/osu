// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Utils;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mania.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Mania.Difficulty.Evaluators
{
    public class IndividualStrainEvaluator
    {
        private const double release_threshold = 30;

        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            var maniaCurrent = (ManiaDifficultyHitObject)current;
            double startTime = maniaCurrent.StartTime;
            double endTime = maniaCurrent.EndTime;

            bool isHeld = false;

            double furthestHoldStartTime = 0;
            double closestHoldEndTime = 10000;

            double holdBonus = 0;

            foreach (var maniaPrevious in maniaCurrent.PreviousHitObjects)
            {
                if (maniaPrevious is null)
                    continue;

                // A note is held if a previous note ends after the current note
                if (Precision.DefinitelyBigger(maniaPrevious.EndTime, endTime, 1) &&
                    Precision.DefinitelyBigger(startTime, maniaPrevious.StartTime, 1))
                {
                    isHeld = true;
                    furthestHoldStartTime = Math.Max(furthestHoldStartTime, startTime - maniaPrevious.StartTime);
                    closestHoldEndTime = Math.Min(closestHoldEndTime, Math.Abs(startTime - maniaPrevious.EndTime));
                }
            }

            // Scale hold bonus so holds very close to the start or end of the held note do not reward as much
            if (isHeld)
            {
                double holdStartScale = DifficultyCalculationUtils.Logistic(x: furthestHoldStartTime, multiplier: 0.27, midpointOffset: release_threshold);
                double holdEndScale = DifficultyCalculationUtils.Logistic(x: closestHoldEndTime, multiplier: 0.27, midpointOffset: release_threshold);
                holdBonus = holdStartScale * holdEndScale;
            }

            return 2 * (1 + 0.25 * holdBonus);
        }
    }
}
