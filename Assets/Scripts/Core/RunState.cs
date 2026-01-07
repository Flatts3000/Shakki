using System;

namespace Shakki.Core
{
    /// <summary>
    /// Represents the state of a single run through the game.
    /// Tracks level progression, inventory, and currency.
    /// </summary>
    [Serializable]
    public class RunState
    {
        public int CurrentLevel { get; private set; } = 1;
        public int HighestLevelReached { get; private set; } = 1;
        public int Coins { get; private set; } = 0;
        public PieceInventory Inventory { get; private set; }
        public bool IsRunActive { get; private set; } = false;
        public int TotalMatchesPlayed { get; private set; } = 0;
        public int TotalMatchesWon { get; private set; } = 0;

        public event Action<int> OnCoinsChanged;
        public event Action<int> OnLevelChanged;
        public event Action OnRunStarted;
        public event Action<RunEndReason> OnRunEnded;

        public RunState()
        {
            Inventory = PieceInventory.CreateStandardSet();
        }

        /// <summary>
        /// Starts a new run from level 1 with standard inventory.
        /// </summary>
        public void StartNewRun()
        {
            CurrentLevel = 1;
            HighestLevelReached = 1;
            Coins = 0;
            Inventory = PieceInventory.CreateStandardSet();
            IsRunActive = true;
            TotalMatchesPlayed = 0;
            TotalMatchesWon = 0;

            OnRunStarted?.Invoke();
            OnLevelChanged?.Invoke(CurrentLevel);
            OnCoinsChanged?.Invoke(Coins);
        }

        /// <summary>
        /// Called when a match is completed.
        /// </summary>
        public void CompleteMatch(bool won, int coinsEarned)
        {
            TotalMatchesPlayed++;

            if (won)
            {
                TotalMatchesWon++;
                AddCoins(coinsEarned);
                AdvanceLevel();
            }
            else
            {
                // Run ends on loss
                EndRun(RunEndReason.Defeated);
            }
        }

        /// <summary>
        /// Advances to the next level after a win.
        /// </summary>
        private void AdvanceLevel()
        {
            CurrentLevel++;
            if (CurrentLevel > HighestLevelReached)
            {
                HighestLevelReached = CurrentLevel;
            }
            OnLevelChanged?.Invoke(CurrentLevel);
        }

        /// <summary>
        /// Adds coins to the run.
        /// </summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            OnCoinsChanged?.Invoke(Coins);
        }

        /// <summary>
        /// Spends coins if available.
        /// Returns true if successful.
        /// </summary>
        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;

            Coins -= amount;
            OnCoinsChanged?.Invoke(Coins);
            return true;
        }

        /// <summary>
        /// Ends the current run.
        /// </summary>
        public void EndRun(RunEndReason reason)
        {
            if (!IsRunActive) return;

            IsRunActive = false;
            OnRunEnded?.Invoke(reason);
        }

        /// <summary>
        /// Gets a summary of the run for display.
        /// </summary>
        public RunSummary GetSummary()
        {
            return new RunSummary
            {
                HighestLevel = HighestLevelReached,
                TotalCoinsEarned = Coins,
                MatchesPlayed = TotalMatchesPlayed,
                MatchesWon = TotalMatchesWon
            };
        }
    }

    public enum RunEndReason
    {
        Defeated,       // Lost a match
        Abandoned,      // Player quit
        Completed       // Future: beat all levels
    }

    [Serializable]
    public struct RunSummary
    {
        public int HighestLevel;
        public int TotalCoinsEarned;
        public int MatchesPlayed;
        public int MatchesWon;
    }
}
