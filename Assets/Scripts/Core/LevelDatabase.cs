using UnityEngine;
using System.Collections.Generic;

namespace Shakki.Core
{
    /// <summary>
    /// Database of all levels with progression curve.
    /// Can use predefined LevelConfigs or generate procedurally.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "Shakki/Level Database", order = 0)]
    public class LevelDatabase : ScriptableObject
    {
        [Header("Predefined Levels")]
        [Tooltip("Manually configured levels (used first)")]
        public List<LevelConfig> PredefinedLevels = new List<LevelConfig>();

        [Header("Procedural Generation (after predefined levels)")]
        [Tooltip("Base target score for procedural levels")]
        public int BaseTargetScore = 10;

        [Tooltip("Target score increase per level")]
        public int TargetScorePerLevel = 3;

        [Tooltip("Base round limit for procedural levels")]
        public int BaseRoundLimit = 35;

        [Tooltip("Round limit decrease per level (makes later levels harder)")]
        public int RoundLimitDecreasePerLevel = 1;

        [Tooltip("Minimum round limit")]
        public int MinRoundLimit = 15;

        [Header("Rewards Scaling")]
        public int BaseWinCoins = 10;
        public int WinCoinsPerLevel = 5;
        public int CoinsPerPoint = 1;

        /// <summary>
        /// Gets the configuration for a specific level.
        /// Uses predefined if available, otherwise generates procedurally.
        /// </summary>
        public LevelConfig GetLevel(int levelNumber)
        {
            // Check predefined levels first (1-indexed)
            int index = levelNumber - 1;
            if (index >= 0 && index < PredefinedLevels.Count && PredefinedLevels[index] != null)
            {
                return PredefinedLevels[index];
            }

            // Generate procedural level
            return GenerateProceduralLevel(levelNumber);
        }

        /// <summary>
        /// Generates a procedural level configuration.
        /// </summary>
        private LevelConfig GenerateProceduralLevel(int levelNumber)
        {
            var config = ScriptableObject.CreateInstance<LevelConfig>();

            config.LevelName = $"Level {levelNumber}";
            config.LevelNumber = levelNumber;

            // Scale difficulty
            config.TargetScore = BaseTargetScore + (levelNumber - 1) * TargetScorePerLevel;
            config.RoundLimit = Mathf.Max(MinRoundLimit, BaseRoundLimit - (levelNumber - 1) * RoundLimitDecreasePerLevel);

            // Scale rewards
            config.WinCoins = BaseWinCoins + (levelNumber - 1) * WinCoinsPerLevel;
            config.CoinsPerPoint = CoinsPerPoint;

            config.UseStandardOpponent = true;

            return config;
        }

        /// <summary>
        /// Gets a ShakkiMatchConfig for a specific level.
        /// </summary>
        public ShakkiMatchConfig GetMatchConfig(int levelNumber)
        {
            return GetLevel(levelNumber).ToMatchConfig();
        }

        /// <summary>
        /// Creates a default database with standard progression.
        /// </summary>
        public static LevelDatabase CreateDefault()
        {
            var db = ScriptableObject.CreateInstance<LevelDatabase>();

            db.BaseTargetScore = 10;
            db.TargetScorePerLevel = 3;
            db.BaseRoundLimit = 35;
            db.RoundLimitDecreasePerLevel = 1;
            db.MinRoundLimit = 15;
            db.BaseWinCoins = 10;
            db.WinCoinsPerLevel = 5;
            db.CoinsPerPoint = 1;

            return db;
        }
    }
}
