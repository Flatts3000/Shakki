using UnityEngine;
using UnityEngine.InputSystem;
using Shakki.Core;
using System;

namespace Shakki.UI
{
    /// <summary>
    /// Debug UI for modifying inventory at runtime.
    /// Toggle with F1 key.
    /// </summary>
    public class DebugInventoryUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        private bool showUI = false;
        private PieceInventory inventory;
        private Action onInventoryChanged;
        private Vector2 scrollPosition;
        private PieceType selectedAddType = PieceType.Pawn;
        private MaterialTier selectedMaterial = MaterialTier.Plastic;

        public void Bind(PieceInventory inventory, Action onChanged)
        {
            this.inventory = inventory;
            this.onInventoryChanged = onChanged;
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current[Key.F1].wasPressedThisFrame)
            {
                showUI = !showUI;
            }
        }

        private void OnGUI()
        {
            if (!showUI || inventory == null) return;

            // Dark background
            GUI.Box(new Rect(10, 10, 320, 500), "");

            GUILayout.BeginArea(new Rect(20, 20, 300, 480));

            GUILayout.Label("<b>DEBUG: Inventory Editor</b>", CreateRichStyle());
            GUILayout.Label($"Pieces: {inventory.Count}/{PieceInventory.MaxPieces}");
            GUILayout.Label($"Press {toggleKey} to toggle");

            GUILayout.Space(10);

            // Add piece section
            GUILayout.Label("<b>Add Piece:</b>", CreateRichStyle());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("P")) selectedAddType = PieceType.Pawn;
            if (GUILayout.Button("N")) selectedAddType = PieceType.Knight;
            if (GUILayout.Button("B")) selectedAddType = PieceType.Bishop;
            if (GUILayout.Button("R")) selectedAddType = PieceType.Rook;
            if (GUILayout.Button("Q")) selectedAddType = PieceType.Queen;
            if (GUILayout.Button("K")) selectedAddType = PieceType.King;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            foreach (MaterialTier mat in Enum.GetValues(typeof(MaterialTier)))
            {
                if (GUILayout.Toggle(selectedMaterial == mat, mat.ToString().Substring(0, 2)))
                    selectedMaterial = mat;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Selected: {selectedMaterial} {selectedAddType}");

            if (GUILayout.Button($"Add {selectedAddType}"))
            {
                var piece = new InventoryPiece(selectedAddType, selectedMaterial);
                if (inventory.TryAddPiece(piece))
                {
                    onInventoryChanged?.Invoke();
                }
                else
                {
                    Debug.LogWarning("Cannot add piece - inventory full or invalid");
                }
            }

            GUILayout.Space(10);

            // Current inventory
            GUILayout.Label("<b>Current Inventory:</b>", CreateRichStyle());
            GUILayout.Label(inventory.ToString());

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            for (int i = 0; i < inventory.Pieces.Count; i++)
            {
                var piece = inventory.Pieces[i];
                GUILayout.BeginHorizontal();

                string label = $"{piece.Material.ToString().Substring(0, 2)} {piece.Type}";
                GUILayout.Label(label, GUILayout.Width(100));

                if (!piece.IsKing || inventory.KingCount > 1)
                {
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        if (inventory.TryRemovePiece(piece))
                        {
                            onInventoryChanged?.Invoke();
                        }
                    }
                }
                else
                {
                    GUILayout.Label("", GUILayout.Width(25)); // Spacer for king
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Preset buttons
            GUILayout.Label("<b>Presets:</b>", CreateRichStyle());

            if (GUILayout.Button("Standard Set (16 pieces)"))
            {
                inventory.AddStandardSet();
                onInventoryChanged?.Invoke();
            }

            if (GUILayout.Button("5 Queens Army"))
            {
                CreateFiveQueensArmy();
            }

            if (GUILayout.Button("No Pawns Army"))
            {
                CreateNoPawnsArmy();
            }

            if (GUILayout.Button("Apply & Restart Match"))
            {
                onInventoryChanged?.Invoke();
            }

            GUILayout.EndArea();
        }

        private GUIStyle CreateRichStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            return style;
        }

        private void CreateFiveQueensArmy()
        {
            // Clear and create a 5-queen army
            while (inventory.Count > 0)
            {
                var piece = inventory.Pieces[0];
                if (!piece.IsKing || inventory.KingCount > 1)
                    inventory.TryRemovePiece(piece);
                else
                    break;
            }

            // Ensure we have a king
            if (!inventory.HasKing)
                inventory.TryAddPiece(new InventoryPiece(PieceType.King));

            // Add 5 queens
            for (int i = 0; i < 5 && !inventory.IsFull; i++)
                inventory.TryAddPiece(new InventoryPiece(PieceType.Queen));

            // Fill with rooks
            while (!inventory.IsFull)
                inventory.TryAddPiece(new InventoryPiece(PieceType.Rook));

            onInventoryChanged?.Invoke();
        }

        private void CreateNoPawnsArmy()
        {
            while (inventory.Count > 0)
            {
                var piece = inventory.Pieces[0];
                if (!piece.IsKing || inventory.KingCount > 1)
                    inventory.TryRemovePiece(piece);
                else
                    break;
            }

            if (!inventory.HasKing)
                inventory.TryAddPiece(new InventoryPiece(PieceType.King));

            // Standard back rank without pawns
            inventory.TryAddPiece(new InventoryPiece(PieceType.Queen));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Rook));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Rook));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Bishop));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Bishop));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Knight));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Knight));

            // Fill remaining with extra pieces
            inventory.TryAddPiece(new InventoryPiece(PieceType.Queen));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Rook));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Rook));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Bishop));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Bishop));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Knight));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Knight));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Knight));

            onInventoryChanged?.Invoke();
        }
    }
}
