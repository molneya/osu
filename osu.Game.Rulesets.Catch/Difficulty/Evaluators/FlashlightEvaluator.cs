// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Catch.Difficulty.Evaluators
{
    public static class FlashlightEvaluator
    {
        /// <summary>
        /// Evaluates the difficulty of memorising and catching a fruit, based on:
        /// <list type="bullet">
        /// <item><description>distance between a number of previous objects and the current object</description></item>
        /// </list>
        /// </summary>
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            var catchCurrent = (CatchDifficultyHitObject)current;
            CatchDifficultyHitObject lastObj = catchCurrent;

            double cumulativeStrainTime = 0.0;
            double result = 0.0;

            // This is iterating backwards in time from the current object.
            for (int i = 0; i < Math.Min(current.Index, 10); i++)
            {
                var currentObj = (CatchDifficultyHitObject)current.Previous(i);

                cumulativeStrainTime += lastObj.StrainTime;

                double jumpDistance = Math.Abs(catchCurrent.BaseObject.EffectiveX - currentObj.BaseObject.EffectiveX);

                result += jumpDistance / cumulativeStrainTime;
            }

            return result;
        }
    }
}