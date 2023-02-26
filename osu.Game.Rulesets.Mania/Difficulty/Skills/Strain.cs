// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Utils;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mania.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.Difficulty.Skills
{
    public class Strain : StrainDecaySkill
    {
        private const double individual_decay_base = 0.125;
        private const double overall_decay_base = 0.30;
        private const double release_threshold = 24;

        protected override double SkillMultiplier => 1;
        protected override double StrainDecayBase => 1;

        private readonly double[] startTimes;
        private readonly double[] endTimes;
        private readonly double[] individualStrains;

        private double holdFactor; // Factor to all additional strains in case something else is held
        private double holdAddition; // Addition to the current note in case it's a hold and has to be released awkwardly
        private int chordSize; // Count of how many concurrent notes there are

        private double individualStrain;
        private double overallStrain;

        public Strain(Mod[] mods, int totalColumns)
            : base(mods)
        {
            startTimes = new double[totalColumns];
            endTimes = new double[totalColumns];
            individualStrains = new double[totalColumns];
            overallStrain = 1;

            holdFactor = 1;
            holdAddition = 0;
            chordSize = 0;
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var maniaCurrent = (ManiaDifficultyHitObject)current;
            var maniaNext = (ManiaDifficultyHitObject)current.Next(0);
            double startTime = maniaCurrent.StartTime;
            double endTime = maniaCurrent.EndTime;
            int column = maniaCurrent.BaseObject.Column;
            bool isOverlapping = false;
            double closestEndTime = Math.Abs(endTime - startTime); // Lowest value we can assume with the current information
            chordSize++;

            for (int i = 0; i < endTimes.Length; ++i)
            {
                // The current note is overlapped if a previous note or end is overlapping the current note body
                if (Precision.DefinitelyBigger(endTimes[i], startTime, 1) && Precision.DefinitelyBigger(endTime, endTimes[i], 1))
                    isOverlapping = true;

                // We give a slight bonus to everything if something is held meanwhile
                if (Precision.DefinitelyBigger(endTimes[i], endTime, 1) && !Precision.AlmostEquals(startTimes[i], startTime, 1))
                    holdFactor = 1.25;

                closestEndTime = Math.Min(closestEndTime, Math.Abs(endTime - endTimes[i]));
            }

            // The hold addition is given if there was an overlap, however it is only valid if there are no other note with a similar ending.
            // Releasing multiple notes is just as easy as releasing 1. Nerfs the hold addition by half if the closest release is release_threshold away.
            // holdAddition
            //     ^
            // 1.0 + - - - - - -+-----------
            //     |           /
            // 0.5 + - - - - -/   Sigmoid Curve
            //     |         /|
            // 0.0 +--------+-+---------------> Release Difference / ms
            //         release_threshold
            if (isOverlapping)
                holdAddition += 1 / (1 + Math.Exp(0.5 * (release_threshold - closestEndTime)));

            // Decay and increase individualStrains in own column
            overallStrain = applyDecay(overallStrain, current.DeltaTime, overall_decay_base);
            individualStrains[column] = applyDecay(individualStrains[column], startTime - startTimes[column], individual_decay_base);
            individualStrains[column] += 2.0 * holdFactor;

            // For notes at the same time (in a chord), the individualStrain should be the hardest individualStrain out of those columns
            individualStrain = maniaCurrent.DeltaTime <= 1 ? Math.Max(individualStrain, individualStrains[column]) : individualStrains[column];

            // Update startTimes and endTimes arrays
            startTimes[column] = startTime;
            endTimes[column] = endTime;

            // We are still adding strain at the current time so there is no need to return difficulty just yet
            if ((maniaNext != null) && (Precision.AlmostEquals(maniaNext.DeltaTime, 0, 1)))
                return 0;

            // Now that we have accounted for all concurrent notes, we can add to the overallStrain
            overallStrain += (chordSize + holdAddition) * holdFactor;
            holdFactor = 1;
            holdAddition = 0;
            chordSize = 0;

            // By subtracting CurrentStrain, this skill effectively only considers the maximum strain of any one hitobject within each strain section.
            return individualStrain + overallStrain - CurrentStrain;
        }

        protected override double CalculateInitialStrain(double offset, DifficultyHitObject current)
            => applyDecay(individualStrain, offset - current.Previous(0).StartTime, individual_decay_base)
               + applyDecay(overallStrain, offset - current.Previous(0).StartTime, overall_decay_base);

        private double applyDecay(double value, double deltaTime, double decayBase)
            => value * Math.Pow(decayBase, deltaTime / 1000);
    }
}
