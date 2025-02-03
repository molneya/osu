// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Catch.Difficulty.Skills;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;

namespace osu.Game.Rulesets.Catch.Difficulty
{
    public class CatchPerformanceCalculator : PerformanceCalculator
    {
        private int num300;
        private int num100;
        private int num50;
        private int numKatu;
        private int numMiss;

        public CatchPerformanceCalculator()
            : base(new CatchRuleset())
        {
        }

        protected override PerformanceAttributes CreatePerformanceAttributes(ScoreInfo score, DifficultyAttributes attributes)
        {
            var catchAttributes = (CatchDifficultyAttributes)attributes;

            num300 = score.GetCount300() ?? 0; // HitResult.Great
            num100 = score.GetCount100() ?? 0; // HitResult.LargeTickHit
            num50 = score.GetCount50() ?? 0; // HitResult.SmallTickHit
            numKatu = score.GetCountKatu() ?? 0; // HitResult.SmallTickMiss
            numMiss = score.GetCountMiss() ?? 0; // HitResult.Miss PLUS HitResult.LargeTickMiss

            double multiplier = 1.0;

            if (score.Mods.Any(m => m is ModNoFail))
                multiplier = Math.Max(0.90, 1.0 - 0.02 * numMiss);

            double movementValue = computeMovementValue(score, catchAttributes);
            double flashlightValue = computeFlashlightValue(score, catchAttributes);

            double totalValue =
                Math.Pow(
                    Math.Pow(movementValue, 1.1) +
                    Math.Pow(flashlightValue, 1.1), 1.0 / 1.1
                ) * multiplier;

            return new CatchPerformanceAttributes
            {
                Movement = movementValue,
                Flashlight = flashlightValue,
                Total = totalValue,
            };
        }

        private double computeMovementValue(ScoreInfo score, CatchDifficultyAttributes attributes)
        {
            double movementValue = Movement.DifficultyToPerformance(attributes.MovementDifficulty);

            // Longer maps are worth more. "Longer" means how many hits there are which can contribute to combo
            int numTotalHits = totalComboHits();

            double lengthBonus =
                0.95 + 0.3 * Math.Min(1.0, numTotalHits / 2500.0) +
                (numTotalHits > 2500 ? Math.Log10(numTotalHits / 2500.0) * 0.475 : 0.0);
            movementValue *= lengthBonus;

            movementValue *= Math.Pow(0.97, numMiss);

            // Combo scaling
            if (attributes.MaxCombo > 0)
                movementValue *= Math.Min(Math.Pow(score.MaxCombo, 0.8) / Math.Pow(attributes.MaxCombo, 0.8), 1.0);

            double approachRate = attributes.ApproachRate;
            double approachRateFactor = 1.0;

            if (approachRate > 9.0)
                approachRateFactor += 0.1 * (approachRate - 9.0); // 10% for each AR above 9
            if (approachRate > 10.0)
                approachRateFactor += 0.1 * (approachRate - 10.0); // Additional 10% at AR 11, 30% total
            else if (approachRate < 8.0)
                approachRateFactor += 0.025 * (8.0 - approachRate); // 2.5% for each AR below 8

            movementValue *= approachRateFactor;

            if (score.Mods.Any(m => m is ModHidden))
            {
                // Hiddens gives almost nothing on max approach rate, and more the lower it is
                if (approachRate <= 10.0)
                    movementValue *= 1.05 + 0.075 * (10.0 - approachRate); // 7.5% for each AR below 10
                else if (approachRate > 10.0)
                    movementValue *= 1.01 + 0.04 * (11.0 - Math.Min(11.0, approachRate)); // 5% at AR 10, 1% at AR 11
            }

            movementValue *= Math.Pow(accuracy(), 5.5);

            return movementValue;
        }

        private double computeFlashlightValue(ScoreInfo score, CatchDifficultyAttributes attributes)
        {
            if (!score.Mods.Any(h => h is CatchModFlashlight))
                return 0.0;

            double flashlightValue = Flashlight.DifficultyToPerformance(attributes.FlashlightDifficulty);

            flashlightValue *= Math.Pow(0.97, numMiss);
            flashlightValue *= Math.Pow(accuracy(), 5.5);

            return flashlightValue;
        }

        private double accuracy() => totalHits() == 0 ? 0 : Math.Clamp((double)totalSuccessfulHits() / totalHits(), 0, 1);
        private int totalHits() => num50 + num100 + num300 + numMiss + numKatu;
        private int totalSuccessfulHits() => num50 + num100 + num300;
        private int totalComboHits() => numMiss + num100 + num300;
    }
}
