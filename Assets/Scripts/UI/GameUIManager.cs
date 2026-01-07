using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Shakki.Core;
using Shakki.Network;

namespace Shakki.UI
{
    /// <summary>
    /// Manages all game UI screens based on game state.
    /// Creates UI elements programmatically.
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        private Canvas mainCanvas;
        private GameObject mainMenuScreen;
        private GameObject preMatchScreen;
        private GameObject postMatchModal;
        private GameObject runEndModal;

        // References for updating
        private TextMeshProUGUI preMatchLevelText;
        private TextMeshProUGUI preMatchTargetText;
        private TextMeshProUGUI postMatchResultText;
        private TextMeshProUGUI postMatchScoreText;
        private TextMeshProUGUI postMatchCoinsText;
        private TextMeshProUGUI runEndTitleText;
        private TextMeshProUGUI runEndSummaryText;

        // Modal panels (for minimize/expand)
        private GameObject postMatchPanel;
        private GameObject runEndPanel;
        private Button postMatchMinimizeBtn;
        private Button runEndMinimizeBtn;
        private bool postMatchMinimized = false;
        private bool runEndMinimized = false;

        private GameFlowController flowController;
        private RunManager runManager;

        private void Awake()
        {
            CreateMainCanvas();
            CreateAllScreens();
        }

        private void Start()
        {
            flowController = GameFlowController.Instance;
            runManager = RunManager.Instance;

            if (flowController != null)
            {
                flowController.OnStateChanged += HandleStateChanged;
                HandleStateChanged(GameFlowController.GameState.MainMenu, flowController.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (flowController != null)
            {
                flowController.OnStateChanged -= HandleStateChanged;
            }
        }

        private void CreateMainCanvas()
        {
            var canvasObj = new GameObject("UI Canvas");
            canvasObj.transform.SetParent(transform);
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 200;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            MobileUIConstants.ConfigureCanvasScaler(scaler);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        private void CreateAllScreens()
        {
            mainMenuScreen = CreateMainMenuScreen();
            preMatchScreen = CreatePreMatchScreen();
            postMatchModal = CreatePostMatchModal();
            runEndModal = CreateRunEndModal();

            HideAllScreens();
        }

        private GameObject CreateMainMenuScreen()
        {
            var screen = CreateFullScreen("MainMenu");
            var content = GetSafeArea(screen);

            var title = CreateText(content, "SHAKKI", 72, new Vector2(0, 200));
            title.fontStyle = FontStyles.Bold;

            var subtitle = CreateText(content, "Chess Roguelike", 36, new Vector2(0, 120));
            subtitle.color = new Color(0.7f, 0.7f, 0.7f);

            CreateButton(content, "Start Run", new Vector2(0, -50), () => {
                flowController?.StartNewRun();
            });

            CreateButton(content, "Multiplayer", new Vector2(0, -160), () => {
                ShowMultiplayerLobby();
            });

            return screen;
        }

        private void ShowMultiplayerLobby()
        {
            // Prefer the new matchmaking UI with level-based matchmaking
            var matchmakingUI = FindFirstObjectByType<Network.MatchmakingUI>();
            if (matchmakingUI != null)
            {
                matchmakingUI.Show();
                return;
            }

            // Fall back to basic LobbyUI
            var lobbyUI = FindFirstObjectByType<Network.LobbyUI>();
            if (lobbyUI != null)
            {
                lobbyUI.Show();
            }
            else
            {
                Debug.LogWarning("MatchmakingUI/LobbyUI not found. Add networking components to the scene.");
            }
        }

        private GameObject CreatePreMatchScreen()
        {
            var screen = CreateFullScreen("PreMatch");
            var content = GetSafeArea(screen);

            preMatchLevelText = CreateText(content, "Level 1", 56, new Vector2(0, 150));
            preMatchLevelText.fontStyle = FontStyles.Bold;

            preMatchTargetText = CreateText(content, "Target: 10 points\nRound Limit: 30", 32, new Vector2(0, 50));

            CreateButton(content, "Begin Match", new Vector2(0, -100), () => {
                flowController?.BeginMatch();
            });

            return screen;
        }

        private GameObject CreatePostMatchModal()
        {
            var modal = new GameObject("PostMatchModal");
            modal.transform.SetParent(mainCanvas.transform, false);

            var modalRect = modal.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 1f);
            modalRect.anchorMax = new Vector2(0.5f, 1f);
            modalRect.pivot = new Vector2(0.5f, 1f);
            modalRect.anchoredPosition = new Vector2(0, -20);
            modalRect.sizeDelta = new Vector2(500, 320);

            // Panel background
            postMatchPanel = new GameObject("Panel");
            postMatchPanel.transform.SetParent(modal.transform, false);
            var panelRect = postMatchPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            var panelBg = postMatchPanel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Result text
            postMatchResultText = CreateText(postMatchPanel.transform, "Victory!", 48, new Vector2(0, 100));
            postMatchResultText.fontStyle = FontStyles.Bold;

            postMatchScoreText = CreateText(postMatchPanel.transform, "Score: 10 - 5", 28, new Vector2(0, 40));

            postMatchCoinsText = CreateText(postMatchPanel.transform, "+15 Coins", 24, new Vector2(0, 0));
            postMatchCoinsText.color = new Color(1f, 0.85f, 0.3f);

            // Buttons row
            CreateButton(postMatchPanel.transform, "Continue", new Vector2(0, -80), () => {
                flowController?.ProceedFromResults();
            }, 200, 50);

            // Minimize button (top right)
            postMatchMinimizeBtn = CreateMinimizeButton(modal.transform, () => TogglePostMatchMinimize());

            return modal;
        }

        private GameObject CreateRunEndModal()
        {
            var modal = new GameObject("RunEndModal");
            modal.transform.SetParent(mainCanvas.transform, false);

            var modalRect = modal.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 1f);
            modalRect.anchorMax = new Vector2(0.5f, 1f);
            modalRect.pivot = new Vector2(0.5f, 1f);
            modalRect.anchoredPosition = new Vector2(0, -20);
            modalRect.sizeDelta = new Vector2(500, 380);

            // Panel background
            runEndPanel = new GameObject("Panel");
            runEndPanel.transform.SetParent(modal.transform, false);
            var panelRect = runEndPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            var panelBg = runEndPanel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            runEndTitleText = CreateText(runEndPanel.transform, "Run Over", 48, new Vector2(0, 130));
            runEndTitleText.fontStyle = FontStyles.Bold;

            runEndSummaryText = CreateText(runEndPanel.transform, "Summary", 24, new Vector2(0, 30));

            CreateButton(runEndPanel.transform, "New Run", new Vector2(0, -100), () => {
                flowController?.ReturnToMenu();
            }, 200, 50);

            // Minimize button
            runEndMinimizeBtn = CreateMinimizeButton(modal.transform, () => ToggleRunEndMinimize());

            return modal;
        }

        private Button CreateMinimizeButton(Transform parent, System.Action onClick)
        {
            var obj = new GameObject("MinimizeBtn");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10, -10);
            rect.sizeDelta = new Vector2(40, 40);

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.4f, 0.4f, 0.4f);

            var button = obj.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "_";
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }

        private void TogglePostMatchMinimize()
        {
            postMatchMinimized = !postMatchMinimized;
            postMatchPanel.SetActive(!postMatchMinimized);
            UpdateMinimizeButtonText(postMatchMinimizeBtn, postMatchMinimized);
        }

        private void ToggleRunEndMinimize()
        {
            runEndMinimized = !runEndMinimized;
            runEndPanel.SetActive(!runEndMinimized);
            UpdateMinimizeButtonText(runEndMinimizeBtn, runEndMinimized);
        }

        private void UpdateMinimizeButtonText(Button btn, bool isMinimized)
        {
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = isMinimized ? "+" : "_";
            }
        }

        private GameObject CreateFullScreen(string name)
        {
            var obj = new GameObject(name + "Screen");
            obj.transform.SetParent(mainCanvas.transform, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Create safe area container for content
            var safeContainer = new GameObject("SafeArea");
            safeContainer.transform.SetParent(obj.transform, false);
            var safeRect = safeContainer.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;
            SafeAreaHandler.AddTo(safeRect);

            return obj; // Return screen object for show/hide - content added via GetSafeArea()
        }

        /// <summary>
        /// Gets the safe area container from a full screen object.
        /// </summary>
        private Transform GetSafeArea(GameObject screen)
        {
            var safeArea = screen.transform.Find("SafeArea");
            return safeArea != null ? safeArea : screen.transform;
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Vector2 position)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(450, 80);
            rect.anchoredPosition = position;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return tmp;
        }

        private Button CreateButton(Transform parent, string text, Vector2 position, System.Action onClick, int width = 0, int height = 0)
        {
            // Use mobile-friendly defaults if not specified
            if (width <= 0) width = (int)MobileUIConstants.PrimaryButtonWidth;
            if (height <= 0) height = (int)MobileUIConstants.PrimaryButtonHeight;

            var obj = new GameObject("Button");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = position;

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.5f, 0.3f);

            var button = obj.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => onClick?.Invoke());

            var colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.4f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.2f);
            button.colors = colors;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = MobileUIConstants.BodyFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }

        private void HideAllScreens()
        {
            mainMenuScreen?.SetActive(false);
            preMatchScreen?.SetActive(false);
            postMatchModal?.SetActive(false);
            runEndModal?.SetActive(false);
        }

        private void HandleStateChanged(GameFlowController.GameState oldState, GameFlowController.GameState newState)
        {
            HideAllScreens();

            // Reset minimize states
            postMatchMinimized = false;
            runEndMinimized = false;
            if (postMatchPanel != null) postMatchPanel.SetActive(true);
            if (runEndPanel != null) runEndPanel.SetActive(true);
            if (postMatchMinimizeBtn != null) UpdateMinimizeButtonText(postMatchMinimizeBtn, false);
            if (runEndMinimizeBtn != null) UpdateMinimizeButtonText(runEndMinimizeBtn, false);

            switch (newState)
            {
                case GameFlowController.GameState.MainMenu:
                    mainMenuScreen.SetActive(true);
                    break;

                case GameFlowController.GameState.PreMatch:
                    UpdatePreMatchScreen();
                    preMatchScreen.SetActive(true);
                    break;

                case GameFlowController.GameState.InMatch:
                    // No overlay screen during match
                    break;

                case GameFlowController.GameState.PostMatch:
                    UpdatePostMatchScreen();
                    postMatchModal.SetActive(true);
                    break;

                case GameFlowController.GameState.Shop:
                    // Shop screen is handled by ShopScreen component
                    break;

                case GameFlowController.GameState.RunEnd:
                    UpdateRunEndScreen();
                    runEndModal.SetActive(true);
                    break;
            }
        }

        private void UpdatePreMatchScreen()
        {
            var config = flowController?.GetCurrentLevelConfig();
            var run = flowController?.GetCurrentRun();

            if (config != null)
            {
                preMatchLevelText.text = $"Level {run?.CurrentLevel ?? 1}";
                preMatchTargetText.text = $"Target: {config.TargetScore} points\nRound Limit: {config.RoundLimit}";
            }
        }

        private void UpdatePostMatchScreen()
        {
            var result = flowController?.GetLastMatchResult();
            if (result.HasValue)
            {
                var r = result.Value;
                postMatchResultText.text = r.Won ? "Victory!" : "Defeat";
                postMatchResultText.color = r.Won ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
                postMatchScoreText.text = $"Score: {r.PlayerScore} - {r.OpponentScore}";
                postMatchCoinsText.text = $"+{r.CoinsEarned} Coins";
            }
        }

        private void UpdateRunEndScreen()
        {
            var result = flowController?.GetLastMatchResult();
            var run = flowController?.GetCurrentRun();

            // Set title based on win/loss
            if (result.HasValue)
            {
                runEndTitleText.text = result.Value.Won ? "Victory!" : "Defeat";
                runEndTitleText.color = result.Value.Won ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
            }

            if (run != null)
            {
                var summary = run.GetSummary();
                runEndSummaryText.text =
                    $"Highest Level: {summary.HighestLevel}\n" +
                    $"Matches Won: {summary.MatchesWon}/{summary.MatchesPlayed}\n" +
                    $"Total Coins: {summary.TotalCoinsEarned}";
            }
        }
    }
}
