// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Catch.Difficulty
{
    public class CatchPerformanceAttributes : PerformanceAttributes
    {
        [JsonProperty("movement")]
        public double Movement { get; set; }

        [JsonProperty("flashlight")]
        public double Flashlight { get; set; }

        public override IEnumerable<PerformanceDisplayAttribute> GetAttributesForDisplay()
        {
            foreach (var attribute in base.GetAttributesForDisplay())
                yield return attribute;

            yield return new PerformanceDisplayAttribute(nameof(Movement), "Movement", Movement);
            yield return new PerformanceDisplayAttribute(nameof(Flashlight), "Flashlight Bonus", Flashlight);
        }
    }
}
