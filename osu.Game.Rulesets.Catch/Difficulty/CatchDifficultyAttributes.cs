// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Catch.Difficulty
{
    public class CatchDifficultyAttributes : DifficultyAttributes
    {
        /// <summary>
        /// The difficulty corresponding to the movement skill.
        /// </summary>
        [JsonProperty("movement_difficulty")]
        public double MovementDifficulty { get; set; }

        /// <summary>
        /// The difficulty corresponding to the flashlight skill.
        /// </summary>
        [JsonProperty("flashlight_difficulty")]
        public double FlashlightDifficulty { get; set; }

        /// <summary>
        /// The perceived approach rate inclusive of rate-adjusting mods (DT/HT/etc).
        /// </summary>
        /// <remarks>
        /// Rate-adjusting mods don't directly affect the approach rate difficulty value, but have a perceived effect as a result of adjusting audio timing.
        /// </remarks>
        [JsonProperty("approach_rate")]
        public double ApproachRate { get; set; }

        public override IEnumerable<(int attributeId, object value)> ToDatabaseAttributes()
        {
            foreach (var v in base.ToDatabaseAttributes())
                yield return v;

            yield return (ATTRIB_ID_DIFFICULTY, StarRating);
            yield return (ATTRIB_ID_AIM, MovementDifficulty);
            yield return (ATTRIB_ID_FLASHLIGHT, FlashlightDifficulty);
            yield return (ATTRIB_ID_APPROACH_RATE, ApproachRate);
        }

        public override void FromDatabaseAttributes(IReadOnlyDictionary<int, double> values, IBeatmapOnlineInfo onlineInfo)
        {
            base.FromDatabaseAttributes(values, onlineInfo);

            StarRating = values[ATTRIB_ID_DIFFICULTY];
            MovementDifficulty = values[ATTRIB_ID_AIM];
            FlashlightDifficulty = values[ATTRIB_ID_FLASHLIGHT];
            ApproachRate = values[ATTRIB_ID_APPROACH_RATE];
        }
    }
}
