using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Shakki.Core;

namespace Shakki.UI
{
    /// <summary>
    /// Displays current level information during a match.
    /// Shows level number, target score, and current coin count.
    /// </summary>
    public class LevelInfoHUD : MonoBehaviour
    {
        private Canvas hudCanvas;
        private TextMeshProUGUI levelText;
        private TextMeshProUGUI targetText;
        private TextMeshProUGUI coinsText;

        private RunManager runManager;
        private GameFlowController flowController;

        private void Awake()
        {
            CreateHUD();
        }

        private void Start()
        {
            runManager = RunManager.Instance;
            flowController = GameFlowController.Instance;

            if (runManager != null && runManager.CurrentRun != null)
            {
                runManager.CurrentRun.OnCoinsChanged += UpdateCoins;
                runManager.CurrentRun.OnLevelChanged += UpdateLevel;
            }

            if (flowController != null)
            {
                flowController.OnStateChanged += HandleStateChanged;
                // Set initial visibility based on current state
                UpdateVisibility(flowController.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (runManager?.CurrentRun != null)
            {
                runManager.CurrentRun.OnCoinsChanged -= UpdateCoins;
                runManager.CurrentRun.OnLevelChanged -= UpdateLevel;
            }

            if (flowController != null)
            {
                flowController.OnStateChanged -= HandleStateChanged;
            }
        }

        private void CreateHUD()
        {
            // Create canvas for level info
            var canvasObj = new GameObject("LevelInfoCanvas");
            canvasObj.transform.SetParent(transform);
            hudCanvas = canvasObj.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            MobileUIConstants.ConfigureCanvasScaler(scaler);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create safe area container
            var safeObj = new GameObject("SafeArea");
            safeObj.transform.SetParent(canvasObj.transform, false);
            var safeRect = safeObj.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;
            SafeAreaHandler.AddTo(safeRect, top: true, bottom: false, left: true, right: true);

            // Create background panel at top
            var panel = CreatePanel(safeObj.transform);

            // Level text (left side)
            levelText = CreateText(panel.transform, "Level 1", TextAlignmentOptions.Left, new Vector2(20, 0));
            levelText.fontStyle = FontStyles.Bold;

            // Target text (center)
            targetText = CreateText(panel.transform, "Target: 10", TextAlignmentOptions.Center, Vector2.zero);

            // Coins text (right side)
            coinsText = CreateText(panel.transform, "0 Coins", TextAlignmentOptions.Right, new Vector2(-20, 0));
            coinsText.color = new Color(1f, 0.85f, 0.3f);

            // Start hidden
            hudCanvas.gameObject.SetActive(false);
        }

        private GameObject CreatePanel(Transform parent)
        {
            var panelObj = new GameObject("LevelPanel");
            panelObj.transform.SetParent(parent, false);

            var rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 60);

            var bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            return panelObj;
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, TextAlignmentOptions alignment, Vector2 offset)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);

            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = offset;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = MobileUIConstants.BodyFontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }

        private void HandleStateChanged(GameFlowController.GameState oldState, GameFlowController.GameState newState)
        {
            UpdateVisibility(newState);

            if (newState == GameFlowController.GameState.InMatch)
            {
                RefreshDisplay();
            }
        }

        private void UpdateVisibility(GameFlowController.GameState state)
        {
            // Only show during match
            bool visible = state == GameFlowController.GameState.InMatch;
            hudCanvas.gameObject.SetActive(visible);
        }

        private void RefreshDisplay()
        {
            var config = runManager?.CurrentLevelConfig;
            var run = runManager?.CurrentRun;

            if (config != null && run != null)
            {
                levelText.text = $"Level {run.CurrentLevel}";
                targetText.text = $"Target: {config.TargetScore}";
                coinsText.text = $"{run.Coins} Coins";
            }
        }

        private void UpdateCoins(int coins)
        {
            coinsText.text = $"{coins} Coins";
        }

        private void UpdateLevel(int level)
        {
            var config = runManager?.CurrentLevelConfig;
            levelText.text = $"Level {level}";
            if (config != null)
            {
                targetText.text = $"Target: {config.TargetScore}";
            }
        }
    }
}
