using UnityEngine;

namespace Shakki.Core
{
    /// <summary>
    /// ScriptableObject defining a single level's configuration.
    /// Create via Assets > Create > Shakki > Level Config
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "Shakki/Level Config", order = 1)]
    public class LevelConfig : ScriptableObject
    {
        [Header("Level Info")]
        [Tooltip("Display name for this level")]
        public string LevelName = "Level 1";

        [Tooltip("Level number (1-based)")]
        [Min(1)]
        public int LevelNumber = 1;

        [Header("Win Conditions")]
        [Tooltip("Score required to win by points")]
        [Min(1)]
        public int TargetScore = 10;

        [Tooltip("Maximum rounds before forced end (0 = unlimited)")]
        [Min(0)]
        public int RoundLimit = 30;

        [Header("Rewards")]
        [Tooltip("Base coins earned for winning this level")]
        [Min(0)]
        public int WinCoins = 10;

        [Tooltip("Coins earned per point scored")]
        [Min(0)]
        public int CoinsPerPoint = 1;

        [Header("Opponent Settings")]
        [Tooltip("Use standard opponent army or custom")]
        public bool UseStandardOpponent = true;

        /// <summary>
        /// Converts this config to a ShakkiMatchConfig for use in matches.
        /// </summary>
        public ShakkiMatchConfig ToMatchConfig()
        {
            return new ShakkiMatchConfig
            {
                TargetScore = TargetScore,
                RoundLimit = RoundLimit
            };
        }

        /// <summary>
        /// Calculates coins earned from a match result.
        /// </summary>
        public int CalculateCoinsEarned(int playerScore, bool won)
        {
            if (!won) return playerScore * CoinsPerPoint; // Only point bonus on loss
            return WinCoins + (playerScore * CoinsPerPoint);
        }
    }
}
