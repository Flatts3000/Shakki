using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Shakki.Core;

namespace Shakki.UI
{
    /// <summary>
    /// Displays match state: scores, rounds, turn indicator during a match.
    /// Creates its own UI Canvas programmatically.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color whitePlayerColor = new Color(1f, 1f, 1f);
        [SerializeField] private Color blackPlayerColor = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color activePlayerHighlight = new Color(0.4f, 0.8f, 0.4f);
        [SerializeField] private Color targetReachedColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color suddenDeathColor = new Color(0.9f, 0.3f, 0.3f);

        private Canvas canvas;
        private TextMeshProUGUI whiteScoreText;
        private TextMeshProUGUI blackScoreText;
        private TextMeshProUGUI roundText;
        private TextMeshProUGUI turnText;
        private TextMeshProUGUI targetText;
        private TextMeshProUGUI statusText;

        private ShakkiMatchState currentMatch;
        private GameFlowController flowController;

        private void Awake()
        {
            CreateUI();
        }

        private void Start()
        {
            flowController = GameFlowController.Instance;
            if (flowController != null)
            {
                flowController.OnStateChanged += HandleStateChanged;
                UpdateVisibility(flowController.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (flowController != null)
            {
                flowController.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameFlowController.GameState oldState, GameFlowController.GameState newState)
        {
            UpdateVisibility(newState);
        }

        private void UpdateVisibility(GameFlowController.GameState state)
        {
            bool visible = state == GameFlowController.GameState.InMatch;
            if (canvas != null)
            {
                canvas.gameObject.SetActive(visible);
            }
        }

        private void CreateUI()
        {
            // Create Canvas
            var canvasObj = new GameObject("HUD Canvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            MobileUIConstants.ConfigureCanvasScaler(scaler);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create safe area container for top elements
            var safeTop = CreatePanel(canvasObj.transform, "SafeAreaTop",
                new Vector2(0, 0), new Vector2(1, 1));
            SafeAreaHandler.AddTo(safeTop, top: true, bottom: false, left: true, right: true);

            // Top panel for scores
            var topPanel = CreatePanel(safeTop, "TopPanel",
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -20), new Vector2(0, -20),
                new Vector2(0.5f, 1f));
            topPanel.sizeDelta = new Vector2(-40, 120);

            var topLayout = topPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.childAlignment = TextAnchor.MiddleCenter;
            topLayout.spacing = 20;
            topLayout.padding = new RectOffset(20, 20, 10, 10);
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = true;

            // White score
            var whitePanel = CreateScorePanel(topPanel, "White", out whiteScoreText);

            // Center info (round, target)
            var centerPanel = CreatePanel(topPanel, "CenterPanel");
            var centerLayout = centerPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            centerLayout.childAlignment = TextAnchor.MiddleCenter;
            centerLayout.spacing = 5;

            roundText = CreateText(centerPanel, "RoundText", "Round 1/30", 28);
            targetText = CreateText(centerPanel, "TargetText", "Target: 10", 22);

            // Black score
            var blackPanel = CreateScorePanel(topPanel, "Black", out blackScoreText);

            // Create safe area container for bottom elements
            var safeBottom = CreatePanel(canvasObj.transform, "SafeAreaBottom",
                new Vector2(0, 0), new Vector2(1, 1));
            SafeAreaHandler.AddTo(safeBottom, top: false, bottom: true, left: true, right: true);

            // Turn indicator (below board)
            var turnPanel = CreatePanel(safeBottom, "TurnPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 180), new Vector2(0, 180),
                new Vector2(0.5f, 0f));
            turnPanel.sizeDelta = new Vector2(300, 50);

            turnText = CreateText(turnPanel, "TurnText", "White to move", MobileUIConstants.SubheadingFontSize);
            turnText.alignment = TextAlignmentOptions.Center;

            // Status text (for check, sudden death, etc.)
            var statusPanel = CreatePanel(safeBottom, "StatusPanel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 120), new Vector2(0, 120),
                new Vector2(0.5f, 0f));
            statusPanel.sizeDelta = new Vector2(400, 40);

            statusText = CreateText(statusPanel, "StatusText", "", MobileUIConstants.BodyFontSize);
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = suddenDeathColor;
        }

        private RectTransform CreatePanel(Transform parent, string name,
            Vector2 anchorMin = default, Vector2 anchorMax = default,
            Vector2 anchoredPos = default, Vector2 pivot = default,
            Vector2? setPivot = null)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();

            if (anchorMin != default || anchorMax != default)
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
            }
            if (anchoredPos != default)
                rt.anchoredPosition = anchoredPos;
            if (setPivot.HasValue)
                rt.pivot = setPivot.Value;

            return rt;
        }

        private RectTransform CreateScorePanel(Transform parent, string label, out TextMeshProUGUI scoreText)
        {
            var panel = CreatePanel(parent, $"{label}ScorePanel");
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 2;

            var labelText = CreateText(panel, "Label", label, 24);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = label == "White" ? whitePlayerColor : blackPlayerColor;

            scoreText = CreateText(panel, "Score", "0", 48);
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.fontStyle = FontStyles.Bold;

            return panel;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return tmp;
        }

        public void BindMatch(ShakkiMatchState match)
        {
            if (currentMatch != null)
            {
                currentMatch.OnPieceCaptured -= HandlePieceCaptured;
                currentMatch.OnRoundComplete -= HandleRoundComplete;
                currentMatch.OnMatchEnd -= HandleMatchEnd;
            }

            currentMatch = match;

            if (currentMatch != null)
            {
                currentMatch.OnPieceCaptured += HandlePieceCaptured;
                currentMatch.OnRoundComplete += HandleRoundComplete;
                currentMatch.OnMatchEnd += HandleMatchEnd;
            }

            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (currentMatch == null) return;

            // Scores
            whiteScoreText.text = currentMatch.WhiteScore.ToString();
            blackScoreText.text = currentMatch.BlackScore.ToString();

            // Highlight if reached target
            whiteScoreText.color = currentMatch.WhiteScore >= currentMatch.Config.TargetScore
                ? targetReachedColor : Color.white;
            blackScoreText.color = currentMatch.BlackScore >= currentMatch.Config.TargetScore
                ? targetReachedColor : Color.white;

            // Round info
            if (currentMatch.Config.RoundLimit > 0)
                roundText.text = $"Round {currentMatch.CurrentRound}/{currentMatch.Config.RoundLimit}";
            else
                roundText.text = $"Round {currentMatch.CurrentRound}";

            targetText.text = $"Target: {currentMatch.Config.TargetScore}";

            // Turn
            bool isWhiteTurn = currentMatch.CurrentPlayer == PieceColor.White;
            turnText.text = isWhiteTurn ? "White to move" : "Black to move";
            turnText.color = isWhiteTurn ? whitePlayerColor : Color.gray;

            // Status
            if (currentMatch.IsInSuddenDeath)
            {
                statusText.text = "SUDDEN DEATH";
                statusText.color = suddenDeathColor;
            }
            else if (currentMatch.IsInCheck)
            {
                statusText.text = "CHECK";
                statusText.color = suddenDeathColor;
            }
            else
            {
                statusText.text = "";
            }
        }

        private void HandlePieceCaptured(Piece piece, PieceColor capturer, int points)
        {
            UpdateDisplay();
            // Could add capture animation/feedback here
        }

        private void HandleRoundComplete(int round)
        {
            UpdateDisplay();
        }

        private void HandleMatchEnd(ShakkiMatchResult result)
        {
            UpdateDisplay();
        }
    }
}
