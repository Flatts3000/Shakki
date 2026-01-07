using UnityEngine;
using System;
using Shakki.Network;

namespace Shakki.Core
{
    /// <summary>
    /// Manages the run lifecycle, level progression, and game flow.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private LevelDatabase levelDatabase;

        public RunState CurrentRun { get; private set; }
        public LevelConfig CurrentLevelConfig { get; private set; }

        public event Action OnRunStarted;
        public event Action<MatchResult> OnMatchCompleted;
        public event Action<RunSummary> OnRunEnded;
        public event Action OnReadyForNextMatch;
        public event Action OnReadyForShop;

        private static RunManager instance;
        public static RunManager Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // Create default level database if not assigned
            if (levelDatabase == null)
            {
                levelDatabase = LevelDatabase.CreateDefault();
            }

            CurrentRun = new RunState();
        }

        /// <summary>
        /// Starts a new run from level 1.
        /// </summary>
        public void StartNewRun()
        {
            CurrentRun = new RunState();
            CurrentRun.StartNewRun();
            CurrentRun.OnRunEnded += HandleRunEnded;

            UpdateCurrentLevelConfig();

            Debug.Log($"=== NEW RUN STARTED ===");
            Debug.Log($"Level {CurrentRun.CurrentLevel}: Target {CurrentLevelConfig.TargetScore}, Rounds {CurrentLevelConfig.RoundLimit}");

            OnRunStarted?.Invoke();
        }

        /// <summary>
        /// Gets the match configuration for the current level.
        /// </summary>
        public ShakkiMatchConfig GetCurrentMatchConfig()
        {
            UpdateCurrentLevelConfig();
            return CurrentLevelConfig.ToMatchConfig();
        }

        /// <summary>
        /// Reports the result of a completed match.
        /// </summary>
        public void ReportMatchResult(ShakkiMatchResult result, int playerScore, int opponentScore)
        {
            bool won = result == ShakkiMatchResult.WhiteWinsByScore ||
                       result == ShakkiMatchResult.WhiteWinsByCheckmate ||
                       result == ShakkiMatchResult.WhiteWinsByRoundLimit;

            int coinsEarned = CurrentLevelConfig.CalculateCoinsEarned(playerScore, won);

            var matchResult = new MatchResult
            {
                Won = won,
                Result = result,
                PlayerScore = playerScore,
                OpponentScore = opponentScore,
                CoinsEarned = coinsEarned,
                LevelNumber = CurrentRun.CurrentLevel
            };

            Debug.Log($"Match {(won ? "WON" : "LOST")}: {playerScore} - {opponentScore}, +{coinsEarned} coins");

            CurrentRun.CompleteMatch(won, coinsEarned);

            OnMatchCompleted?.Invoke(matchResult);

            if (won && CurrentRun.IsRunActive)
            {
                UpdateCurrentLevelConfig();
                Debug.Log($"Advancing to Level {CurrentRun.CurrentLevel}: Target {CurrentLevelConfig.TargetScore}, Rounds {CurrentLevelConfig.RoundLimit}");
            }
        }

        /// <summary>
        /// Reports the result of a networked match.
        /// Converts NetworkMatchResult to ShakkiMatchResult based on local player color.
        /// </summary>
        public void ReportNetworkMatchResult(NetworkMatchResult networkResult, PieceColor localPlayerColor, int whiteScore, int blackScore)
        {
            // Determine player and opponent scores based on local color
            int playerScore = localPlayerColor == PieceColor.White ? whiteScore : blackScore;
            int opponentScore = localPlayerColor == PieceColor.White ? blackScore : whiteScore;

            // Convert network result to Shakki result
            ShakkiMatchResult result;
            if (networkResult == NetworkMatchResult.Draw)
            {
                result = ShakkiMatchResult.Draw;
            }
            else
            {
                bool localWon = (localPlayerColor == PieceColor.White && networkResult == NetworkMatchResult.WhiteWins) ||
                               (localPlayerColor == PieceColor.Black && networkResult == NetworkMatchResult.BlackWins);

                if (localWon)
                {
                    result = localPlayerColor == PieceColor.White
                        ? ShakkiMatchResult.WhiteWinsByScore
                        : ShakkiMatchResult.BlackWinsByScore;
                }
                else
                {
                    result = localPlayerColor == PieceColor.White
                        ? ShakkiMatchResult.BlackWinsByScore
                        : ShakkiMatchResult.WhiteWinsByScore;
                }
            }

            Debug.Log($"[Network Match] LocalColor={localPlayerColor}, Result={networkResult}, Scores={whiteScore}-{blackScore}");

            // Use existing match result flow
            ReportMatchResult(result, playerScore, opponentScore);
        }

        /// <summary>
        /// Called when player is ready to proceed to the shop.
        /// </summary>
        public void ProceedToShop()
        {
            OnReadyForShop?.Invoke();
        }

        /// <summary>
        /// Called when player is ready to proceed to the next match.
        /// </summary>
        public void ProceedToNextMatch()
        {
            OnReadyForNextMatch?.Invoke();
        }

        /// <summary>
        /// Abandons the current run.
        /// </summary>
        public void AbandonRun()
        {
            if (CurrentRun != null && CurrentRun.IsRunActive)
            {
                CurrentRun.EndRun(RunEndReason.Abandoned);
            }
        }

        private void UpdateCurrentLevelConfig()
        {
            CurrentLevelConfig = levelDatabase.GetLevel(CurrentRun.CurrentLevel);
        }

        private void HandleRunEnded(RunEndReason reason)
        {
            var summary = CurrentRun.GetSummary();

            Debug.Log($"=== RUN ENDED ({reason}) ===");
            Debug.Log($"Highest Level: {summary.HighestLevel}");
            Debug.Log($"Matches: {summary.MatchesWon}/{summary.MatchesPlayed}");
            Debug.Log($"Coins Earned: {summary.TotalCoinsEarned}");

            OnRunEnded?.Invoke(summary);
        }

        /// <summary>
        /// Gets the player's inventory for the current run.
        /// </summary>
        public PieceInventory GetPlayerInventory()
        {
            return CurrentRun?.Inventory ?? PieceInventory.CreateStandardSet();
        }
    }

    [Serializable]
    public struct MatchResult
    {
        public bool Won;
        public ShakkiMatchResult Result;
        public int PlayerScore;
        public int OpponentScore;
        public int CoinsEarned;
        public int LevelNumber;
    }
}
