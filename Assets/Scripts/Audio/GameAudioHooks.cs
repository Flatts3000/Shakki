using UnityEngine;
using Shakki.Core;

namespace Shakki.Audio
{
    /// <summary>
    /// Connects game events to audio playback.
    /// Automatically plays appropriate sounds for game actions.
    /// </summary>
    public class GameAudioHooks : MonoBehaviour
    {
        private GameManager gameManager;
        private GameFlowController flowController;
        private RunManager runManager;
        private AudioManager audioManager;

        private void Start()
        {
            audioManager = AudioManager.Instance;
            gameManager = GameManager.Instance;
            flowController = GameFlowController.Instance;
            runManager = RunManager.Instance;

            // Subscribe to game events
            if (gameManager != null)
            {
                gameManager.OnMoveMade += HandleMoveMade;
                gameManager.OnMatchEnded += HandleMatchEnded;
            }

            if (flowController != null)
            {
                flowController.OnStateChanged += HandleStateChanged;
            }

            if (runManager != null)
            {
                runManager.OnMatchCompleted += HandleMatchCompleted;
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnMoveMade -= HandleMoveMade;
                gameManager.OnMatchEnded -= HandleMatchEnded;
            }

            if (flowController != null)
            {
                flowController.OnStateChanged -= HandleStateChanged;
            }

            if (runManager != null)
            {
                runManager.OnMatchCompleted -= HandleMatchCompleted;
            }
        }

        private void HandleMoveMade(Move move, bool isCapture)
        {
            if (audioManager == null) return;

            if (isCapture)
            {
                audioManager.PlaySFX(AudioManager.SoundEffect.PieceCapture);
            }
            else
            {
                audioManager.PlaySFX(AudioManager.SoundEffect.PieceMove);
            }
        }

        private void HandleMatchEnded(ShakkiMatchResult result)
        {
            if (audioManager == null) return;

            // Check for checkmate specifically
            if (result == ShakkiMatchResult.WhiteWinsByCheckmate ||
                result == ShakkiMatchResult.BlackWinsByCheckmate)
            {
                audioManager.PlaySFX(AudioManager.SoundEffect.Checkmate);
            }
        }

        private void HandleMatchCompleted(MatchResult result)
        {
            if (audioManager == null) return;

            if (result.Won)
            {
                audioManager.PlaySFX(AudioManager.SoundEffect.MatchWin);

                // Level up sound if we advanced
                if (result.LevelNumber > 1)
                {
                    audioManager.PlaySFX(AudioManager.SoundEffect.LevelUp, 0.8f);
                }
            }
            else
            {
                audioManager.PlaySFX(AudioManager.SoundEffect.MatchLose);
            }
        }

        private void HandleStateChanged(GameFlowController.GameState oldState, GameFlowController.GameState newState)
        {
            if (audioManager == null) return;

            switch (newState)
            {
                case GameFlowController.GameState.Shop:
                    audioManager.PlaySFX(AudioManager.SoundEffect.ShopOpen);
                    break;
            }
        }

        /// <summary>
        /// Call this when a UI button is clicked.
        /// </summary>
        public static void PlayButtonClick()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SoundEffect.ButtonClick);
        }

        /// <summary>
        /// Call this when coins are earned.
        /// </summary>
        public static void PlayCoinEarn()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SoundEffect.CoinEarn);
        }

        /// <summary>
        /// Call this when a shop purchase is made.
        /// </summary>
        public static void PlayShopPurchase()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SoundEffect.ShopPurchase);
        }

        /// <summary>
        /// Call this when a check occurs.
        /// </summary>
        public static void PlayCheck()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SoundEffect.Check);
        }
    }
}
