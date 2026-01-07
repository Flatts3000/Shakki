using UnityEngine;
using System;

namespace Shakki.Core
{
    /// <summary>
    /// Controls the flow between game states: Menu, Match, Results, Shop.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        public enum GameState
        {
            MainMenu,
            PreMatch,       // Level intro / ready screen
            InMatch,
            PostMatch,      // Results screen
            Shop,
            RunEnd          // Run summary screen
        }

        [Header("References")]
        [SerializeField] private RunManager runManager;

        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public event Action<GameState, GameState> OnStateChanged; // old, new

        private MatchResult? lastMatchResult;

        private static GameFlowController instance;
        public static GameFlowController Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            if (runManager == null)
                runManager = GetComponent<RunManager>() ?? FindFirstObjectByType<RunManager>();
        }

        private void Start()
        {
            // Subscribe to run manager events
            if (runManager != null)
            {
                runManager.OnRunStarted += HandleRunStarted;
                runManager.OnMatchCompleted += HandleMatchCompleted;
                runManager.OnRunEnded += HandleRunEnded;
                runManager.OnReadyForShop += HandleReadyForShop;
                runManager.OnReadyForNextMatch += HandleReadyForNextMatch;
            }

            SetState(GameState.MainMenu);
        }

        private void OnDestroy()
        {
            if (runManager != null)
            {
                runManager.OnRunStarted -= HandleRunStarted;
                runManager.OnMatchCompleted -= HandleMatchCompleted;
                runManager.OnRunEnded -= HandleRunEnded;
                runManager.OnReadyForShop -= HandleReadyForShop;
                runManager.OnReadyForNextMatch -= HandleReadyForNextMatch;
            }
        }

        /// <summary>
        /// Called from UI to start a new run.
        /// </summary>
        public void StartNewRun()
        {
            runManager.StartNewRun();
        }

        /// <summary>
        /// Called when match ends to proceed to results.
        /// </summary>
        public void ShowResults(MatchResult result)
        {
            lastMatchResult = result;
            SetState(GameState.PostMatch);
        }

        /// <summary>
        /// Called from results screen to proceed.
        /// </summary>
        public void ProceedFromResults()
        {
            if (lastMatchResult?.Won == true)
            {
                // Won - go to shop
                runManager.ProceedToShop();
            }
            else
            {
                // Lost - go to run end screen
                SetState(GameState.RunEnd);
            }
        }

        /// <summary>
        /// Called from shop to proceed to next match.
        /// </summary>
        public void ProceedFromShop()
        {
            runManager.ProceedToNextMatch();
        }

        /// <summary>
        /// Called from run end screen to return to menu.
        /// </summary>
        public void ReturnToMenu()
        {
            SetState(GameState.MainMenu);
        }

        /// <summary>
        /// Called to start the actual match gameplay.
        /// </summary>
        public void BeginMatch()
        {
            SetState(GameState.InMatch);
        }

        private void HandleRunStarted()
        {
            SetState(GameState.PreMatch);
        }

        private void HandleMatchCompleted(MatchResult result)
        {
            ShowResults(result);
        }

        private void HandleRunEnded(RunSummary summary)
        {
            SetState(GameState.RunEnd);
        }

        private void HandleReadyForShop()
        {
            SetState(GameState.Shop);
        }

        private void HandleReadyForNextMatch()
        {
            SetState(GameState.PreMatch);
        }

        private void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            var oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"Game State: {oldState} -> {newState}");

            OnStateChanged?.Invoke(oldState, newState);
        }

        public MatchResult? GetLastMatchResult() => lastMatchResult;

        public RunState GetCurrentRun() => runManager?.CurrentRun;

        public LevelConfig GetCurrentLevelConfig() => runManager?.CurrentLevelConfig;
    }
}
