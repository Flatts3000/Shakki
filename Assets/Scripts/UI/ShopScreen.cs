using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Shakki.Core;

namespace Shakki.UI
{
    /// <summary>
    /// UI for the between-match shop.
    /// </summary>
    public class ShopScreen : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject mainPanel;
        private GameObject boxSelectionPanel;
        private GameObject boxContentsPanel;
        private GameObject inventoryPanel;

        private TextMeshProUGUI coinsText;
        private TextMeshProUGUI inventoryCountText;
        private Transform boxContentsContainer;
        private Transform inventoryContainer;

        private ShopManager shopManager;
        private GameFlowController flowController;

        private List<GameObject> boxContentItems = new List<GameObject>();
        private List<GameObject> inventoryItems = new List<GameObject>();

        private InventoryPiece? selectedBoxPiece;
        private InventoryPiece? selectedInventoryPiece;

        private void Awake()
        {
            CreateUI();
        }

        private void Start()
        {
            shopManager = ShopManager.Instance;
            flowController = GameFlowController.Instance;

            if (shopManager != null)
            {
                shopManager.OnShopUpdated += RefreshUI;
                shopManager.OnBoxOpened += HandleBoxOpened;
                shopManager.OnPieceAcquired += HandlePieceAcquired;
                shopManager.OnPieceSold += HandlePieceSold;
            }

            if (flowController != null)
            {
                flowController.OnStateChanged += HandleStateChanged;
                HandleStateChanged(GameFlowController.GameState.MainMenu, flowController.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (shopManager != null)
            {
                shopManager.OnShopUpdated -= RefreshUI;
                shopManager.OnBoxOpened -= HandleBoxOpened;
                shopManager.OnPieceAcquired -= HandlePieceAcquired;
                shopManager.OnPieceSold -= HandlePieceSold;
            }

            if (flowController != null)
            {
                flowController.OnStateChanged -= HandleStateChanged;
            }
        }

        private void CreateUI()
        {
            // Create Canvas
            var canvasObj = new GameObject("ShopCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            MobileUIConstants.ConfigureCanvasScaler(scaler);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Main panel (full screen background)
            mainPanel = CreatePanel(canvasObj.transform, "MainPanel", Vector2.zero, Vector2.one);
            var mainBg = mainPanel.AddComponent<Image>();
            mainBg.color = new Color(0.1f, 0.1f, 0.15f, 0.98f);

            // Create safe area container - use this for all content
            var safePanel = CreatePanel(mainPanel.transform, "SafeArea", Vector2.zero, Vector2.one);
            SafeAreaHandler.AddTo(safePanel.GetComponent<RectTransform>());
            var contentParent = safePanel.transform;

            // Title
            var title = CreateText(contentParent, "SHOP", MobileUIConstants.HeadingFontSize, new Vector2(0.5f, 1f), new Vector2(0, -40));
            title.fontStyle = FontStyles.Bold;

            // Coins display
            coinsText = CreateText(contentParent, "Coins: 0", MobileUIConstants.SubheadingFontSize, new Vector2(0.5f, 1f), new Vector2(0, -100));
            coinsText.color = new Color(1f, 0.85f, 0.3f);

            // Box selection panel
            CreateBoxSelectionPanel();

            // Box contents panel (hidden initially)
            CreateBoxContentsPanel();

            // Inventory panel
            CreateInventoryPanel();

            // Continue button - mobile-friendly size
            CreateButton(contentParent, "Continue", new Vector2(0.5f, 0f), new Vector2(0, 100),
                (int)MobileUIConstants.PrimaryButtonWidth, (int)MobileUIConstants.PrimaryButtonHeight, () => {
                shopManager?.ExitShop();
                flowController?.ProceedFromShop();
            });

            canvas.gameObject.SetActive(false);
        }

        private void CreateBoxSelectionPanel()
        {
            boxSelectionPanel = CreatePanel(mainPanel.transform, "BoxSelection",
                new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.85f));

            var bg = boxSelectionPanel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            var label = CreateText(boxSelectionPanel.transform, "Buy a Box", 28, new Vector2(0.5f, 1f), new Vector2(0, -20));

            // Basic box button
            CreateBoxButton(boxSelectionPanel.transform, "Basic Box", new Vector2(0.25f, 0.5f), () => {
                if (shopManager?.AvailableBoxes.Count > 0)
                    shopManager.TryOpenBox(shopManager.AvailableBoxes[0]);
            }, true);

            // Premium box button
            CreateBoxButton(boxSelectionPanel.transform, "Premium Box", new Vector2(0.75f, 0.5f), () => {
                if (shopManager?.AvailableBoxes.Count > 1)
                    shopManager.TryOpenBox(shopManager.AvailableBoxes[1]);
            }, false);
        }

        private void CreateBoxButton(Transform parent, string label, Vector2 anchor, System.Action onClick, bool isBasic)
        {
            var obj = new GameObject(label + "Btn");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor - new Vector2(0.2f, 0.3f);
            rect.anchorMax = anchor + new Vector2(0.2f, 0.3f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = obj.AddComponent<Image>();
            bg.color = isBasic ? new Color(0.3f, 0.4f, 0.5f) : new Color(0.5f, 0.4f, 0.2f);

            var button = obj.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => onClick?.Invoke());

            var text = CreateText(obj.transform, label, 24, new Vector2(0.5f, 0.7f), Vector2.zero);
            var costText = CreateText(obj.transform, isBasic ? "10 Coins" : "25 Coins", 20, new Vector2(0.5f, 0.3f), Vector2.zero);
            costText.color = new Color(1f, 0.85f, 0.3f);
        }

        private void CreateBoxContentsPanel()
        {
            boxContentsPanel = CreatePanel(mainPanel.transform, "BoxContents",
                new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.58f));

            var bg = boxContentsPanel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            var label = CreateText(boxContentsPanel.transform, "Box Contents", 24, new Vector2(0.5f, 1f), new Vector2(0, -15));

            // Container for piece items
            var containerObj = new GameObject("Container");
            containerObj.transform.SetParent(boxContentsPanel.transform, false);
            var containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, 0.1f);
            containerRect.anchorMax = new Vector2(0.95f, 0.8f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            var layout = containerObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;

            boxContentsContainer = containerObj.transform;

            boxContentsPanel.SetActive(false);
        }

        private void CreateInventoryPanel()
        {
            inventoryPanel = CreatePanel(mainPanel.transform, "Inventory",
                new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.33f));

            var bg = inventoryPanel.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            inventoryCountText = CreateText(inventoryPanel.transform, "Inventory (0/16)", 24, new Vector2(0.5f, 1f), new Vector2(0, -15));

            // Container for inventory items
            var containerObj = new GameObject("Container");
            containerObj.transform.SetParent(inventoryPanel.transform, false);
            var containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.02f, 0.1f);
            containerRect.anchorMax = new Vector2(0.98f, 0.8f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            var layout = containerObj.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(60, 70);
            layout.spacing = new Vector2(5, 5);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            layout.constraintCount = 2;

            inventoryContainer = containerObj.transform;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return obj;
        }

        private TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Vector2 anchor, Vector2 position)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(400, 50);
            rect.anchoredPosition = position;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return tmp;
        }

        private Button CreateButton(Transform parent, string text, Vector2 anchor, Vector2 position, int width, int height, System.Action onClick)
        {
            var obj = new GameObject("Button");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
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
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }

        private void HandleStateChanged(GameFlowController.GameState oldState, GameFlowController.GameState newState)
        {
            bool showShop = newState == GameFlowController.GameState.Shop;
            canvas.gameObject.SetActive(showShop);

            if (showShop)
            {
                var run = flowController?.GetCurrentRun();
                if (run != null)
                {
                    shopManager?.InitializeShop(run);
                }
                RefreshUI();
            }
        }

        private void HandleBoxOpened(ShopBox box)
        {
            boxSelectionPanel.SetActive(false);
            boxContentsPanel.SetActive(true);
            RefreshBoxContents();
        }

        private void HandlePieceAcquired(InventoryPiece piece)
        {
            RefreshUI();
        }

        private void HandlePieceSold(InventoryPiece piece, int value)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (shopManager?.CurrentRun != null)
            {
                coinsText.text = $"Coins: {shopManager.CurrentRun.Coins}";
                inventoryCountText.text = $"Inventory ({shopManager.CurrentRun.Inventory.Count}/16)";
            }

            // Show appropriate panel
            if (shopManager?.CurrentlyOpenBox != null)
            {
                boxSelectionPanel.SetActive(false);
                boxContentsPanel.SetActive(true);
                RefreshBoxContents();
            }
            else
            {
                boxSelectionPanel.SetActive(true);
                boxContentsPanel.SetActive(false);
            }

            RefreshInventory();
        }

        private void RefreshBoxContents()
        {
            // Clear existing items
            foreach (var item in boxContentItems)
            {
                Destroy(item);
            }
            boxContentItems.Clear();

            var box = shopManager?.CurrentlyOpenBox;
            if (box == null) return;

            foreach (var piece in box.Contents)
            {
                var item = CreatePieceItem(boxContentsContainer, piece, true);
                boxContentItems.Add(item);
            }
        }

        private void RefreshInventory()
        {
            // Clear existing items
            foreach (var item in inventoryItems)
            {
                Destroy(item);
            }
            inventoryItems.Clear();

            var inventory = shopManager?.CurrentRun?.Inventory;
            if (inventory == null) return;

            foreach (var piece in inventory.Pieces)
            {
                var item = CreatePieceItem(inventoryContainer, piece, false);
                inventoryItems.Add(item);
            }
        }

        private GameObject CreatePieceItem(Transform parent, InventoryPiece piece, bool isFromBox)
        {
            var obj = new GameObject("PieceItem");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 90);

            var bg = obj.AddComponent<Image>();
            bg.color = GetTierColor(piece.Material);

            var button = obj.AddComponent<Button>();
            button.targetGraphic = bg;

            // Piece type text
            var typeText = CreateText(obj.transform, GetPieceSymbol(piece.Type), 32, new Vector2(0.5f, 0.6f), Vector2.zero);

            // Sell value text
            int sellValue = shopManager?.GetSellValue(piece) ?? 0;
            var valueText = CreateText(obj.transform, $"{sellValue}c", 16, new Vector2(0.5f, 0.15f), Vector2.zero);
            valueText.color = new Color(1f, 0.85f, 0.3f);

            // Click handler
            var capturedPiece = piece;
            var capturedIsFromBox = isFromBox;
            button.onClick.AddListener(() => OnPieceClicked(capturedPiece, capturedIsFromBox));

            return obj;
        }

        private void OnPieceClicked(InventoryPiece piece, bool isFromBox)
        {
            if (isFromBox)
            {
                // Try to take piece, or sell if inventory full
                if (!shopManager.TryTakePiece(piece))
                {
                    // Inventory full - sell instead
                    shopManager.TrySellBoxPiece(piece);
                }
            }
            else
            {
                // Sell from inventory
                shopManager.TrySellInventoryPiece(piece);
            }
        }

        private Color GetTierColor(MaterialTier tier)
        {
            return tier switch
            {
                MaterialTier.Plastic => new Color(0.4f, 0.4f, 0.4f),
                MaterialTier.Wood => new Color(0.6f, 0.4f, 0.2f),
                MaterialTier.Stone => new Color(0.5f, 0.5f, 0.55f),
                MaterialTier.Metal => new Color(0.6f, 0.6f, 0.7f),
                MaterialTier.Gold => new Color(0.9f, 0.75f, 0.3f),
                _ => new Color(0.3f, 0.3f, 0.3f)
            };
        }

        private string GetPieceSymbol(PieceType type)
        {
            return type switch
            {
                PieceType.King => "K",
                PieceType.Queen => "Q",
                PieceType.Rook => "R",
                PieceType.Bishop => "B",
                PieceType.Knight => "N",
                PieceType.Pawn => "P",
                _ => "?"
            };
        }
    }
}
