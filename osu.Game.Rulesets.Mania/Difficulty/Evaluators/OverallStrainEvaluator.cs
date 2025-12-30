// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Utils;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Utils;
using osu.Game.Rulesets.Mania.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Mania.Difficulty.Evaluators
{
    public class OverallStrainEvaluator
    {
        private const double release_threshold = 30;

        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            var maniaCurrent = (ManiaDifficultyHitObject)current;
            double startTime = maniaCurrent.StartTime;
            double endTime = maniaCurrent.EndTime;

            bool isOverlapping = false;
            bool isHeld = false;

            double closestOverlapStartTime = Math.Abs(endTime - startTime);
            double closestOverlapEndTime = Math.Abs(endTime - startTime);
            double furthestHoldStartTime = 0;
            double closestHoldEndTime = 10000;

            double overlapBonus = 0;
            double holdBonus = 0;

            foreach (var maniaPrevious in maniaCurrent.PreviousHitObjects)
            {
                if (maniaPrevious is null)
                    continue;

                // A note is overlapped if a previous note ends during the current note body
                if (Precision.DefinitelyBigger(maniaPrevious.EndTime, startTime, 1) &&
                    Precision.DefinitelyBigger(endTime, maniaPrevious.EndTime, 1))
                {
                    isOverlapping = true;
                }

                closestOverlapStartTime = Math.Min(closestOverlapStartTime, Math.Abs(startTime - maniaPrevious.StartTime));
                closestOverlapEndTime = Math.Min(closestOverlapEndTime, Math.Abs(endTime - maniaPrevious.EndTime));

                // A note is held if a previous note ends after the current note
                if (Precision.DefinitelyBigger(maniaPrevious.EndTime, endTime, 1) &&
                    Precision.DefinitelyBigger(startTime, maniaPrevious.StartTime, 1))
                {
                    isHeld = true;
                    furthestHoldStartTime = Math.Max(furthestHoldStartTime, startTime - maniaPrevious.StartTime);
                    closestHoldEndTime = Math.Min(closestHoldEndTime, Math.Abs(startTime - maniaPrevious.EndTime));
                }
            }

            // Scale overlap bonus so extremely brief overlaps do not reward as much 
            if (isOverlapping)
            {
                double overlapStartScale = DifficultyCalculationUtils.Logistic(x: closestOverlapStartTime, multiplier: 0.27, midpointOffset: release_threshold);
                double overlapEndScale = DifficultyCalculationUtils.Logistic(x: closestOverlapEndTime, multiplier: 0.27, midpointOffset: release_threshold);
                overlapBonus = overlapStartScale * overlapEndScale;
            }

            // Scale hold bonus so holds very close to the start or end of the held note do not reward as much
            if (isHeld)
            {
                double holdStartScale = DifficultyCalculationUtils.Logistic(x: furthestHoldStartTime, multiplier: 0.27, midpointOffset: release_threshold);
                double holdEndScale = DifficultyCalculationUtils.Logistic(x: closestHoldEndTime, multiplier: 0.27, midpointOffset: release_threshold);
                holdBonus = holdStartScale * holdEndScale;
            }

            return (1 + overlapBonus) * (1 + 0.25 * holdBonus);
        }
    }
}
