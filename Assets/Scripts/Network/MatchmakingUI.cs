using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Shakki.UI;
using Shakki.Core;

namespace Shakki.Network
{
    /// <summary>
    /// UI for the matchmaking queue.
    /// Shows search status, level range, and allows cancellation.
    /// </summary>
    public class MatchmakingUI : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject mainPanel;
        private GameObject searchingPanel;
        private GameObject errorPanel;

        private TextMeshProUGUI levelText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI levelRangeText;
        private TextMeshProUGUI errorText;
        private Button findMatchButton;
        private Button cancelButton;
        private Button retryButton;

        private MatchmakingManager matchmaking;
        private RunManager runManager;
        private float searchTimer;
        private bool isSearching;

        private void Awake()
        {
            CreateUI();
        }

        private void Start()
        {
            matchmaking = MatchmakingManager.Instance;
            runManager = RunManager.Instance;

            if (matchmaking != null)
            {
                matchmaking.OnStateChanged += HandleStateChanged;
                matchmaking.OnMatchFound += HandleMatchFound;
                matchmaking.OnError += HandleError;
            }

            // Start hidden
            canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (matchmaking != null)
            {
                matchmaking.OnStateChanged -= HandleStateChanged;
                matchmaking.OnMatchFound -= HandleMatchFound;
                matchmaking.OnError -= HandleError;
            }
        }

        private void Update()
        {
            if (isSearching)
            {
                searchTimer += Time.deltaTime;
                UpdateSearchingText();
            }
        }

        private void CreateUI()
        {
            // Create Canvas
            var canvasObj = new GameObject("MatchmakingCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 350;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            MobileUIConstants.ConfigureCanvasScaler(scaler);
            canvasObj.AddComponent<GraphicRaycaster>();

            // Main panel (find match)
            mainPanel = CreatePanel(canvasObj.transform, "MainPanel");
            CreateMainPanelContent();

            // Searching panel
            searchingPanel = CreatePanel(canvasObj.transform, "SearchingPanel");
            CreateSearchingPanelContent();

            // Error panel
            errorPanel = CreatePanel(canvasObj.transform, "ErrorPanel");
            CreateErrorPanelContent();

            ShowMainPanel();
        }

        private GameObject CreatePanel(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Safe area
            var safeObj = new GameObject("SafeArea");
            safeObj.transform.SetParent(obj.transform, false);
            var safeRect = safeObj.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.sizeDelta = Vector2.zero;
            SafeAreaHandler.AddTo(safeRect);

            return obj;
        }

        private Transform GetSafeArea(GameObject panel)
        {
            return panel.transform.Find("SafeArea") ?? panel.transform;
        }

        private void CreateMainPanelContent()
        {
            var content = GetSafeArea(mainPanel);

            // Title
            var title = CreateText(content, "Find Match", MobileUIConstants.HeadingFontSize, new Vector2(0, 250));
            title.fontStyle = FontStyles.Bold;

            // Current level display
            levelText = CreateText(content, "Your Level: 1", MobileUIConstants.SubheadingFontSize, new Vector2(0, 150));

            // Level range info
            levelRangeText = CreateText(content, "Matching with levels 1-4", MobileUIConstants.BodyFontSize, new Vector2(0, 80));
            levelRangeText.color = new Color(0.7f, 0.7f, 0.7f);

            // Find Match button
            findMatchButton = CreateButton(content, "Find Match", new Vector2(0, -50), OnFindMatchClicked);

            // Back button
            CreateButton(content, "Back", new Vector2(0, -180), OnBackClicked,
                (int)MobileUIConstants.SecondaryButtonWidth, (int)MobileUIConstants.SecondaryButtonHeight);
        }

        private void CreateSearchingPanelContent()
        {
            var content = GetSafeArea(searchingPanel);

            // Status text
            statusText = CreateText(content, "Searching for opponent...", MobileUIConstants.HeadingFontSize, new Vector2(0, 150));

            // Animated dots will be added via Update()

            // Level range
            var rangeText = CreateText(content, "Looking for players level 1-4", MobileUIConstants.BodyFontSize, new Vector2(0, 50));
            rangeText.color = new Color(0.7f, 0.7f, 0.7f);

            // Cancel button
            cancelButton = CreateButton(content, "Cancel", new Vector2(0, -100), OnCancelClicked,
                (int)MobileUIConstants.SecondaryButtonWidth, (int)MobileUIConstants.SecondaryButtonHeight);
        }

        private void CreateErrorPanelContent()
        {
            var content = GetSafeArea(errorPanel);

            // Error title
            var title = CreateText(content, "Connection Error", MobileUIConstants.HeadingFontSize, new Vector2(0, 150));
            title.color = new Color(0.9f, 0.3f, 0.3f);

            // Error message
            errorText = CreateText(content, "Failed to connect", MobileUIConstants.BodyFontSize, new Vector2(0, 50));

            // Retry button
            retryButton = CreateButton(content, "Retry", new Vector2(0, -80), OnRetryClicked);

            // Back button
            CreateButton(content, "Back", new Vector2(0, -200), OnBackClicked,
                (int)MobileUIConstants.SecondaryButtonWidth, (int)MobileUIConstants.SecondaryButtonHeight);
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Vector2 position)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(800, 60);
            rect.anchoredPosition = position;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return tmp;
        }

        private Button CreateButton(Transform parent, string text, Vector2 position, System.Action onClick,
            int width = 0, int height = 0)
        {
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

        private void ShowMainPanel()
        {
            mainPanel.SetActive(true);
            searchingPanel.SetActive(false);
            errorPanel.SetActive(false);
            isSearching = false;

            // Update level display
            int level = runManager?.CurrentRun?.CurrentLevel ?? 1;
            levelText.text = $"Your Level: {level}";

            if (matchmaking != null)
            {
                var range = matchmaking.GetLevelBucketRange(level);
                levelRangeText.text = $"Matching with levels {range.min}-{range.max}";
            }
        }

        private void ShowSearchingPanel()
        {
            mainPanel.SetActive(false);
            searchingPanel.SetActive(true);
            errorPanel.SetActive(false);
            isSearching = true;
            searchTimer = 0f;
        }

        private void ShowErrorPanel(string error)
        {
            mainPanel.SetActive(false);
            searchingPanel.SetActive(false);
            errorPanel.SetActive(true);
            isSearching = false;

            errorText.text = error;
        }

        private void UpdateSearchingText()
        {
            int dots = ((int)(searchTimer * 2)) % 4;
            string dotString = new string('.', dots);

            var state = matchmaking?.CurrentState ?? MatchmakingState.Searching;
            string baseText = state switch
            {
                MatchmakingState.CreatingLobby => "Creating lobby",
                MatchmakingState.WaitingForOpponent => "Waiting for opponent",
                MatchmakingState.JoiningLobby => "Joining match",
                _ => "Searching"
            };

            int seconds = (int)searchTimer;
            statusText.text = $"{baseText}{dotString}\n({seconds}s)";
        }

        // Button handlers
        private void OnFindMatchClicked()
        {
            int level = runManager?.CurrentRun?.CurrentLevel ?? 1;
            ShowSearchingPanel();
            matchmaking?.StartMatchmaking(level);
        }

        private void OnCancelClicked()
        {
            matchmaking?.CancelMatchmaking();
            ShowMainPanel();
        }

        private void OnRetryClicked()
        {
            OnFindMatchClicked();
        }

        private void OnBackClicked()
        {
            Hide();
        }

        // Event handlers
        private void HandleStateChanged(MatchmakingState state)
        {
            switch (state)
            {
                case MatchmakingState.Searching:
                case MatchmakingState.CreatingLobby:
                case MatchmakingState.WaitingForOpponent:
                case MatchmakingState.JoiningLobby:
                    ShowSearchingPanel();
                    break;
                case MatchmakingState.Connected:
                    // Match found - hide UI, game will start
                    Hide();
                    break;
                case MatchmakingState.Error:
                    ShowErrorPanel("Connection failed. Please try again.");
                    break;
            }
        }

        private void HandleMatchFound()
        {
            Debug.Log("[MatchmakingUI] Match found!");
            Hide();
        }

        private void HandleError(string error)
        {
            ShowErrorPanel(error);
        }

        /// <summary>
        /// Shows the matchmaking UI.
        /// </summary>
        public void Show()
        {
            canvas.gameObject.SetActive(true);
            ShowMainPanel();
        }

        /// <summary>
        /// Hides the matchmaking UI.
        /// </summary>
        public void Hide()
        {
            canvas.gameObject.SetActive(false);
            isSearching = false;
        }
    }
}
