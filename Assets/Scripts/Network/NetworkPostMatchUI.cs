using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Shakki.UI;
using Shakki.Core;

namespace Shakki.Network
{
    /// <summary>
    /// Post-match UI for networked games.
    /// Shows results and allows rematch or exit.
    /// </summary>
    public class NetworkPostMatchUI : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject resultPanel;
        private GameObject waitingPanel;

        private TextMeshProUGUI resultTitleText;
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI statusText;
        private Button rematchButton;
        private Button exitButton;

        private NetworkMatchController matchController;
        private NetworkBoard networkBoard;
        private NetworkGameManager networkGameManager;
        private RunManager runManager;

        private void Awake()
        {
            CreateUI();
        }

        private void Start()
        {
            matchController = NetworkMatchController.Instance;
            networkBoard = NetworkBoard.Instance;
            networkGameManager = NetworkGameManager.Instance;
            runManager = RunManager.Instance;

            if (matchController != null)
            {
                matchController.OnMatchComplete += HandleMatchComplete;
                matchController.OnRematchAccepted += HandleRematchAccepted;
                matchController.OnOpponentDeclinedRematch += HandleRematchDeclined;
                matchController.OnPlayerDisconnected += HandlePlayerDisconnected;
            }

            if (networkBoard != null)
            {
                networkBoard.OnMatchEnded += HandleNetworkMatchEnded;
            }

            canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (matchController != null)
            {
                matchController.OnMatchComplete -= HandleMatchComplete;
                matchController.OnRematchAccepted -= HandleRematchAccepted;
                matchController.OnOpponentDeclinedRematch -= HandleRematchDeclined;
                matchController.OnPlayerDisconnected -= HandlePlayerDisconnected;
            }

            if (networkBoard != null)
            {
                networkBoard.OnMatchEnded -= HandleNetworkMatchEnded;
            }
        }

        private void CreateUI()
        {
            // Create Canvas
            var canvasObj = new GameObject("NetworkPostMatchCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            MobileUIConstants.ConfigureCanvasScaler(scaler);
            canvasObj.AddComponent<GraphicRaycaster>();

            // Result panel
            resultPanel = CreatePanel(canvasObj.transform, "ResultPanel");
            CreateResultPanelContent();

            // Waiting for rematch response panel
            waitingPanel = CreatePanel(canvasObj.transform, "WaitingPanel");
            CreateWaitingPanelContent();

            resultPanel.SetActive(false);
            waitingPanel.SetActive(false);
        }

        private GameObject CreatePanel(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 500);

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.98f);

            return obj;
        }

        private void CreateResultPanelContent()
        {
            // Result title
            resultTitleText = CreateText(resultPanel.transform, "Victory!", 56, new Vector2(0, 180));
            resultTitleText.fontStyle = FontStyles.Bold;

            // Score
            scoreText = CreateText(resultPanel.transform, "10 - 5", 36, new Vector2(0, 100));

            // Rematch button
            rematchButton = CreateButton(resultPanel.transform, "Rematch", new Vector2(0, -20), OnRematchClicked);

            // Exit button
            exitButton = CreateButton(resultPanel.transform, "Exit", new Vector2(0, -130), OnExitClicked,
                (int)MobileUIConstants.SecondaryButtonWidth, (int)MobileUIConstants.SecondaryButtonHeight);
        }

        private void CreateWaitingPanelContent()
        {
            // Status text
            statusText = CreateText(waitingPanel.transform, "Waiting for opponent...", 36, new Vector2(0, 50));

            // Cancel button
            CreateButton(waitingPanel.transform, "Cancel", new Vector2(0, -80), OnCancelRematchClicked,
                (int)MobileUIConstants.SecondaryButtonWidth, (int)MobileUIConstants.SecondaryButtonHeight);
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Vector2 position)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(550, 80);
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

        private void ShowResultPanel(NetworkMatchResult result)
        {
            canvas.gameObject.SetActive(true);
            resultPanel.SetActive(true);
            waitingPanel.SetActive(false);

            // Determine if local player won
            var localColor = networkGameManager?.LocalPlayerColor;
            bool won = false;

            if (localColor.HasValue)
            {
                won = (localColor.Value == PieceColor.White && result == NetworkMatchResult.WhiteWins) ||
                      (localColor.Value == PieceColor.Black && result == NetworkMatchResult.BlackWins);
            }

            // Update UI
            resultTitleText.text = won ? "Victory!" : (result == NetworkMatchResult.Draw ? "Draw" : "Defeat");
            resultTitleText.color = won ? new Color(0.3f, 0.8f, 0.3f) :
                                   (result == NetworkMatchResult.Draw ? Color.white : new Color(0.8f, 0.3f, 0.3f));

            int whiteScore = 0;
            int blackScore = 0;
            if (networkBoard != null)
            {
                whiteScore = networkBoard.WhiteScore;
                blackScore = networkBoard.BlackScore;
                scoreText.text = $"{whiteScore} - {blackScore}";
            }

            // Report result to RunManager for run progression
            if (runManager != null && localColor.HasValue)
            {
                runManager.ReportNetworkMatchResult(result, localColor.Value, whiteScore, blackScore);
            }
        }

        private void ShowWaitingPanel(string message)
        {
            canvas.gameObject.SetActive(true);
            resultPanel.SetActive(false);
            waitingPanel.SetActive(true);
            statusText.text = message;
        }

        private void Hide()
        {
            canvas.gameObject.SetActive(false);
        }

        // Button handlers
        private void OnRematchClicked()
        {
            ShowWaitingPanel("Waiting for opponent's response...");
            matchController?.RequestRematch();
        }

        private void OnExitClicked()
        {
            // Disconnect and return to main menu
            networkGameManager?.Disconnect();
            Hide();

            var flowController = GameFlowController.Instance;
            flowController?.ReturnToMenu();
        }

        private void OnCancelRematchClicked()
        {
            matchController?.DeclineRematch();
            OnExitClicked();
        }

        // Event handlers
        private void HandleNetworkMatchEnded(NetworkMatchResult result)
        {
            ShowResultPanel(result);
        }

        private void HandleMatchComplete(NetworkMatchResult result)
        {
            ShowResultPanel(result);
        }

        private void HandleRematchAccepted()
        {
            Hide();
            // NetworkBoard will handle starting new match
        }

        private void HandleRematchDeclined()
        {
            statusText.text = "Opponent declined rematch";
            // Show exit option
            resultPanel.SetActive(true);
            waitingPanel.SetActive(false);
        }

        private void HandlePlayerDisconnected(PieceColor disconnectedColor)
        {
            var localColor = networkGameManager?.LocalPlayerColor;
            if (localColor.HasValue && localColor.Value != disconnectedColor)
            {
                // Opponent disconnected - we win
                ShowResultPanel(localColor.Value == PieceColor.White ?
                    NetworkMatchResult.WhiteWins : NetworkMatchResult.BlackWins);

                // Disable rematch since opponent left
                rematchButton.interactable = false;
                var buttonText = rematchButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Opponent Left";
            }
        }
    }
}
