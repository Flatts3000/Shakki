using UnityEngine;

namespace Shakki.Core
{
    /// <summary>
    /// Configuration for a single Shakki match.
    /// Defines win conditions and limits for a level.
    /// </summary>
    [System.Serializable]
    public class ShakkiMatchConfig
    {
        [Tooltip("Score required to win (checked at end of each round)")]
        public int TargetScore = 10;

        [Tooltip("Maximum rounds before forced end (0 = unlimited)")]
        public int RoundLimit = 30;

        public static ShakkiMatchConfig Default => new ShakkiMatchConfig
        {
            TargetScore = 10,
            RoundLimit = 30
        };

        /// <summary>
        /// Creates a config for a specific level with scaling difficulty.
        /// </summary>
        public static ShakkiMatchConfig ForLevel(int level)
        {
            // Scale target score and round limit with level
            // Level 1: 10 points, 30 rounds
            // Higher levels: more points needed, fewer rounds
            int targetScore = 10 + (level - 1) * 3;
            int roundLimit = Mathf.Max(20, 35 - level);

            return new ShakkiMatchConfig
            {
                TargetScore = targetScore,
                RoundLimit = roundLimit
            };
        }
    }
}
