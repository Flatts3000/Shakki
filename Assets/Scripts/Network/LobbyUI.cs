using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Shakki.UI;

namespace Shakki.Network
{
    /// <summary>
    /// UI for hosting and joining networked games.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject mainPanel;
        private GameObject connectingPanel;
        private GameObject waitingPanel;

        private TMP_InputField ipInputField;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI playerColorText;
        private Button hostButton;
        private Button joinButton;
        private Button disconnectButton;

        private NetworkGameManager networkManager;

        private void Awake()
        {
            CreateUI();
        }

        private void Start()
        {
            networkManager = NetworkGameManager.Instance;

            if (networkManager != null)
            {
                networkManager.OnPhaseChanged += HandlePhaseChanged;
                networkManager.OnConnectionError += HandleConnectionError;
                networkManager.OnLocalPlayerAssigned += HandlePlayerAssigned;
            }

            // Subscribe to NetworkManager events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnected;
            }

            ShowMainPanel();
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnPhaseChanged -= HandlePhaseChanged;
                networkManager.OnConnectionError -= HandleConnectionError;
                networkManager.OnLocalPlayerAssigned -= HandlePlayerAssigned;
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnected;
            }
        }

        private void CreateUI()
        {
            // Create Canvas
            var canvasObj = new GameObject("LobbyCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            MobileUIConstants.ConfigureCanvasScaler(scaler);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Main panel
            mainPanel = CreatePanel(canvasObj.transform, "MainPanel");
            CreateMainPanelContent();

            // Connecting panel
            connectingPanel = CreatePanel(canvasObj.transform, "ConnectingPanel");
            CreateConnectingPanelContent();

            // Waiting for opponent panel
            waitingPanel = CreatePanel(canvasObj.transform, "WaitingPanel");
            CreateWaitingPanelContent();

            // Hide all initially
            mainPanel.SetActive(false);
            connectingPanel.SetActive(false);
            waitingPanel.SetActive(false);
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

            // Add safe area
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
            var title = CreateText(content, "Multiplayer", MobileUIConstants.HeadingFontSize, new Vector2(0, 300));
            title.fontStyle = FontStyles.Bold;

            // IP Input
            CreateText(content, "Server IP:", MobileUIConstants.BodyFontSize, new Vector2(0, 100));
            ipInputField = CreateInputField(content, "127.0.0.1", new Vector2(0, 40));

            // Host button
            hostButton = CreateButton(content, "Host Game", new Vector2(0, -80), OnHostClicked);

            // Join button
            joinButton = CreateButton(content, "Join Game", new Vector2(0, -180), OnJoinClicked);

            // Back button
            CreateButton(content, "Back", new Vector2(0, -320), OnBackClicked,
                (int)MobileUIConstants.SecondaryButtonWidth, (int)MobileUIConstants.SecondaryButtonHeight);
        }

        private void CreateConnectingPanelContent()
        {
            var content = GetSafeArea(connectingPanel);

            statusText = CreateText(content, "Connecting...", MobileUIConstants.HeadingFontSize, new Vector2(0, 100));

            // Cancel button
            CreateButton(content, "Cancel", new Vector2(0, -100), OnDisconnectClicked,
                (int)MobileUIConstants.SecondaryButtonWidth, (int)MobileUIConstants.SecondaryButtonHeight);
        }

        private void CreateWaitingPanelContent()
        {
            var content = GetSafeArea(waitingPanel);

            var title = CreateText(content, "Waiting for Opponent", MobileUIConstants.HeadingFontSize, new Vector2(0, 200));

            playerColorText = CreateText(content, "You are: White", MobileUIConstants.SubheadingFontSize, new Vector2(0, 80));

            statusText = CreateText(content, "Waiting for another player to join...", MobileUIConstants.BodyFontSize, new Vector2(0, 0));

            // Disconnect button
            disconnectButton = CreateButton(content, "Disconnect", new Vector2(0, -150), OnDisconnectClicked,
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

        private TMP_InputField CreateInputField(Transform parent, string placeholder, Vector2 position)
        {
            var obj = new GameObject("InputField");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 70);
            rect.anchoredPosition = position;

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f);

            var inputField = obj.AddComponent<TMP_InputField>();

            // Text area
            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(obj.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 5);
            textAreaRect.offsetMax = new Vector2(-10, -5);
            textArea.AddComponent<RectMask2D>();

            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            // Placeholder
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            var phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = Vector2.zero;

            var phTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
            phTmp.text = placeholder;
            phTmp.fontSize = 28;
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;
            phTmp.color = new Color(0.5f, 0.5f, 0.5f);

            inputField.textViewport = textAreaRect;
            inputField.textComponent = tmp;
            inputField.placeholder = phTmp;
            inputField.text = placeholder;

            return inputField;
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
            connectingPanel.SetActive(false);
            waitingPanel.SetActive(false);
        }

        private void ShowConnectingPanel(string message)
        {
            mainPanel.SetActive(false);
            connectingPanel.SetActive(true);
            waitingPanel.SetActive(false);

            var text = connectingPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = message;
        }

        private void ShowWaitingPanel()
        {
            mainPanel.SetActive(false);
            connectingPanel.SetActive(false);
            waitingPanel.SetActive(true);
        }

        private void HideAll()
        {
            mainPanel.SetActive(false);
            connectingPanel.SetActive(false);
            waitingPanel.SetActive(false);
        }

        // Button handlers
        private void OnHostClicked()
        {
            ShowConnectingPanel("Starting host...");
            networkManager?.StartHost();
        }

        private void OnJoinClicked()
        {
            string ip = ipInputField?.text ?? "127.0.0.1";
            ShowConnectingPanel($"Connecting to {ip}...");
            networkManager?.StartClient(ip);
        }

        private void OnDisconnectClicked()
        {
            networkManager?.Disconnect();
            ShowMainPanel();
        }

        private void OnBackClicked()
        {
            // Return to main menu
            canvas.gameObject.SetActive(false);
            // Could trigger an event or call GameFlowController here
        }

        // Event handlers
        private void HandleConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                ShowWaitingPanel();
            }
        }

        private void HandleDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                ShowMainPanel();
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Playing)
            {
                // Game started - hide lobby
                HideAll();
            }
            else if (phase == GamePhase.WaitingForPlayers)
            {
                ShowWaitingPanel();
            }
        }

        private void HandleConnectionError(string error)
        {
            Debug.LogError($"[LobbyUI] Connection error: {error}");
            ShowMainPanel();
            // Could show error dialog
        }

        private void HandlePlayerAssigned(Shakki.Core.PieceColor color)
        {
            if (playerColorText != null)
            {
                playerColorText.text = $"You are: {color}";
            }
        }

        /// <summary>
        /// Shows the lobby UI.
        /// </summary>
        public void Show()
        {
            canvas.gameObject.SetActive(true);
            ShowMainPanel();
        }

        /// <summary>
        /// Hides the lobby UI.
        /// </summary>
        public void Hide()
        {
            canvas.gameObject.SetActive(false);
        }
    }
}
