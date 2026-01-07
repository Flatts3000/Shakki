using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shakki.Core
{
    /// <summary>
    /// Manages the between-match shop where players can buy boxes and sell pieces.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        [Header("Box Pricing")]
        [SerializeField] private int basicBoxCost = 10;
        [SerializeField] private int premiumBoxCost = 25;

        [Header("Box Contents")]
        [SerializeField] private int basicBoxPieceCount = 1;
        [SerializeField] private int premiumBoxPieceCount = 3;

        public event Action OnShopUpdated;
        public event Action<ShopBox> OnBoxOpened;
        public event Action<InventoryPiece> OnPieceAcquired;
        public event Action<InventoryPiece, int> OnPieceSold;

        private RunState currentRun;
        private List<ShopBox> availableBoxes = new List<ShopBox>();
        private ShopBox currentlyOpenBox;

        private static ShopManager instance;
        public static ShopManager Instance => instance;

        public int BasicBoxCost => basicBoxCost;
        public int PremiumBoxCost => premiumBoxCost;
        public IReadOnlyList<ShopBox> AvailableBoxes => availableBoxes;
        public ShopBox CurrentlyOpenBox => currentlyOpenBox;
        public RunState CurrentRun => currentRun;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        /// <summary>
        /// Initializes the shop for a run.
        /// </summary>
        public void InitializeShop(RunState run)
        {
            currentRun = run;
            currentlyOpenBox = null;
            RefreshBoxes();
        }

        /// <summary>
        /// Refreshes available boxes (called at start of each shop visit).
        /// </summary>
        public void RefreshBoxes()
        {
            availableBoxes.Clear();

            // Always offer basic and premium boxes
            availableBoxes.Add(new ShopBox(ShopBoxType.Basic, basicBoxCost, basicBoxPieceCount));
            availableBoxes.Add(new ShopBox(ShopBoxType.Premium, premiumBoxCost, premiumBoxPieceCount));

            OnShopUpdated?.Invoke();
        }

        /// <summary>
        /// Attempts to purchase and open a box.
        /// </summary>
        public bool TryOpenBox(ShopBox box)
        {
            if (currentRun == null) return false;
            if (!currentRun.TrySpendCoins(box.Cost)) return false;

            // Generate box contents
            box.GenerateContents(currentRun.CurrentLevel);
            currentlyOpenBox = box;

            OnBoxOpened?.Invoke(box);
            return true;
        }

        /// <summary>
        /// Takes a piece from the currently open box into inventory.
        /// </summary>
        public bool TryTakePiece(InventoryPiece piece)
        {
            if (currentRun == null || currentlyOpenBox == null) return false;
            if (!currentlyOpenBox.Contents.Contains(piece)) return false;

            // Check if inventory has room or if we need to swap
            if (currentRun.Inventory.Count >= 16)
            {
                // Inventory full - caller should handle swap flow
                return false;
            }

            if (currentRun.Inventory.TryAddPiece(piece))
            {
                currentlyOpenBox.Contents.Remove(piece);
                OnPieceAcquired?.Invoke(piece);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Swaps a piece from inventory with one from the box.
        /// </summary>
        public bool TrySwapPiece(InventoryPiece fromInventory, InventoryPiece fromBox)
        {
            if (currentRun == null || currentlyOpenBox == null) return false;
            if (!currentlyOpenBox.Contents.Contains(fromBox)) return false;

            // Can't swap out the King
            if (fromInventory.Type == PieceType.King)
            {
                return false;
            }

            if (currentRun.Inventory.TrySwapPiece(fromInventory, fromBox))
            {
                currentlyOpenBox.Contents.Remove(fromBox);
                currentlyOpenBox.Contents.Add(fromInventory);
                OnPieceAcquired?.Invoke(fromBox);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sells a piece from the currently open box for coins.
        /// </summary>
        public bool TrySellBoxPiece(InventoryPiece piece)
        {
            if (currentRun == null || currentlyOpenBox == null) return false;
            if (!currentlyOpenBox.Contents.Contains(piece)) return false;

            int sellValue = GetSellValue(piece);
            currentRun.AddCoins(sellValue);
            currentlyOpenBox.Contents.Remove(piece);

            OnPieceSold?.Invoke(piece, sellValue);
            return true;
        }

        /// <summary>
        /// Sells a piece from inventory for coins.
        /// </summary>
        public bool TrySellInventoryPiece(InventoryPiece piece)
        {
            if (currentRun == null) return false;

            // Can't sell the King
            if (piece.Type == PieceType.King)
            {
                return false;
            }

            if (currentRun.Inventory.TryRemovePiece(piece))
            {
                int sellValue = GetSellValue(piece);
                currentRun.AddCoins(sellValue);
                OnPieceSold?.Invoke(piece, sellValue);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Closes the current box, selling any remaining pieces.
        /// </summary>
        public void CloseBox()
        {
            if (currentlyOpenBox != null)
            {
                // Sell remaining pieces at half value
                foreach (var piece in currentlyOpenBox.Contents.ToArray())
                {
                    int sellValue = GetSellValue(piece) / 2;
                    currentRun.AddCoins(sellValue);
                    OnPieceSold?.Invoke(piece, sellValue);
                }
                currentlyOpenBox.Contents.Clear();
                currentlyOpenBox = null;
            }

            OnShopUpdated?.Invoke();
        }

        /// <summary>
        /// Gets the sell value for a piece.
        /// </summary>
        public int GetSellValue(InventoryPiece piece)
        {
            int baseValue = piece.Type switch
            {
                PieceType.Pawn => 1,
                PieceType.Knight => 3,
                PieceType.Bishop => 3,
                PieceType.Rook => 5,
                PieceType.Queen => 9,
                PieceType.King => 0, // Can't sell King
                _ => 0
            };

            // Material tier multiplier
            int tierMultiplier = piece.Material switch
            {
                MaterialTier.Plastic => 1,
                MaterialTier.Wood => 2,
                MaterialTier.Stone => 3,
                MaterialTier.Metal => 4,
                MaterialTier.Gold => 5,
                _ => 1
            };

            return baseValue * tierMultiplier;
        }

        /// <summary>
        /// Exits the shop and proceeds to next match.
        /// </summary>
        public void ExitShop()
        {
            CloseBox();
            // GameFlowController handles the state transition
        }
    }

    public enum ShopBoxType
    {
        Basic,
        Premium
    }

    /// <summary>
    /// Represents a purchasable box in the shop.
    /// </summary>
    public class ShopBox
    {
        public ShopBoxType Type { get; private set; }
        public int Cost { get; private set; }
        public int PieceCount { get; private set; }
        public List<InventoryPiece> Contents { get; private set; } = new List<InventoryPiece>();
        public bool IsOpened => Contents.Count > 0;

        public ShopBox(ShopBoxType type, int cost, int pieceCount)
        {
            Type = type;
            Cost = cost;
            PieceCount = pieceCount;
        }

        /// <summary>
        /// Generates random piece contents based on level.
        /// </summary>
        public void GenerateContents(int level)
        {
            Contents.Clear();

            for (int i = 0; i < PieceCount; i++)
            {
                var piece = GenerateRandomPiece(level, Type == ShopBoxType.Premium);
                Contents.Add(piece);
            }
        }

        private InventoryPiece GenerateRandomPiece(int level, bool isPremium)
        {
            // Piece type weights (no Kings in boxes)
            float[] typeWeights = { 0.4f, 0.2f, 0.2f, 0.15f, 0.05f }; // Pawn, Knight, Bishop, Rook, Queen
            PieceType[] types = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };

            // Premium boxes have better chances for rare pieces
            if (isPremium)
            {
                typeWeights = new float[] { 0.2f, 0.25f, 0.25f, 0.2f, 0.1f };
            }

            // Select piece type
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;
            PieceType selectedType = PieceType.Pawn;

            for (int i = 0; i < typeWeights.Length; i++)
            {
                cumulative += typeWeights[i];
                if (roll <= cumulative)
                {
                    selectedType = types[i];
                    break;
                }
            }

            // Determine material tier based on level
            MaterialTier tier = DetermineTier(level, isPremium);

            return new InventoryPiece(selectedType, tier);
        }

        private MaterialTier DetermineTier(int level, bool isPremium)
        {
            // Base tier chances improve with level
            float plasticChance = Mathf.Max(0.1f, 0.5f - level * 0.05f);
            float woodChance = 0.3f;
            float stoneChance = Mathf.Min(0.3f, 0.1f + level * 0.02f);
            float metalChance = Mathf.Min(0.2f, level * 0.02f);
            float goldChance = Mathf.Min(0.1f, level * 0.01f);

            // Premium boxes boost higher tiers
            if (isPremium)
            {
                plasticChance *= 0.5f;
                woodChance *= 0.8f;
                stoneChance *= 1.2f;
                metalChance *= 1.5f;
                goldChance *= 2f;
            }

            // Normalize
            float total = plasticChance + woodChance + stoneChance + metalChance + goldChance;
            float roll = UnityEngine.Random.value * total;

            if (roll < plasticChance) return MaterialTier.Plastic;
            roll -= plasticChance;
            if (roll < woodChance) return MaterialTier.Wood;
            roll -= woodChance;
            if (roll < stoneChance) return MaterialTier.Stone;
            roll -= stoneChance;
            if (roll < metalChance) return MaterialTier.Metal;
            return MaterialTier.Gold;
        }
    }
}
