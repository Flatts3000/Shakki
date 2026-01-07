using System;
using System.Collections.Generic;
using System.Linq;

namespace Shakki.Core
{
    /// <summary>
    /// Manages the player's collection of chess pieces.
    /// Enforces the 16-piece cap and exactly-one-King rule.
    /// </summary>
    [Serializable]
    public class PieceInventory
    {
        public const int MaxPieces = 16;

        private List<InventoryPiece> pieces = new List<InventoryPiece>();
        private int nextId = 1;

        public IReadOnlyList<InventoryPiece> Pieces => pieces;
        public int Count => pieces.Count;
        public int RemainingSlots => MaxPieces - pieces.Count;
        public bool IsFull => pieces.Count >= MaxPieces;

        public event Action OnInventoryChanged;

        /// <summary>
        /// Creates an empty inventory.
        /// </summary>
        public PieceInventory()
        {
        }

        /// <summary>
        /// Creates an inventory with the standard starting set.
        /// </summary>
        public static PieceInventory CreateStandardSet()
        {
            var inventory = new PieceInventory();
            inventory.AddStandardSet();
            return inventory;
        }

        /// <summary>
        /// Adds the standard 16-piece chess set.
        /// </summary>
        public void AddStandardSet()
        {
            pieces.Clear();
            nextId = 1;

            // Back rank: R N B Q K B N R
            AddPieceInternal(PieceType.Rook);
            AddPieceInternal(PieceType.Knight);
            AddPieceInternal(PieceType.Bishop);
            AddPieceInternal(PieceType.Queen);
            AddPieceInternal(PieceType.King);
            AddPieceInternal(PieceType.Bishop);
            AddPieceInternal(PieceType.Knight);
            AddPieceInternal(PieceType.Rook);

            // Pawns
            for (int i = 0; i < 8; i++)
                AddPieceInternal(PieceType.Pawn);

            OnInventoryChanged?.Invoke();
        }

        private void AddPieceInternal(PieceType type, MaterialTier material = MaterialTier.Plastic)
        {
            pieces.Add(new InventoryPiece(type, material, nextId++));
        }

        /// <summary>
        /// Attempts to add a piece to the inventory.
        /// Returns false if inventory is full or would violate King rule.
        /// </summary>
        public bool TryAddPiece(InventoryPiece piece)
        {
            if (IsFull)
                return false;

            // Can't have more than one King
            if (piece.IsKing && HasKing)
                return false;

            // Assign new ID if not set
            if (piece.Id == 0)
                piece = new InventoryPiece(piece.Type, piece.Material, nextId++);

            pieces.Add(piece);
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Attempts to add a piece, returns the piece that needs to be removed first if full.
        /// </summary>
        public AddPieceResult TryAddPieceWithSwap(InventoryPiece newPiece)
        {
            if (newPiece.IsKing && HasKing)
            {
                return new AddPieceResult
                {
                    Success = false,
                    RequiresSwap = false,
                    Message = "You already have a King"
                };
            }

            if (!IsFull)
            {
                TryAddPiece(newPiece);
                return new AddPieceResult { Success = true };
            }

            return new AddPieceResult
            {
                Success = false,
                RequiresSwap = true,
                Message = "Inventory full - select a piece to replace"
            };
        }

        /// <summary>
        /// Removes a piece from the inventory.
        /// Cannot remove the last King.
        /// </summary>
        public bool TryRemovePiece(InventoryPiece piece)
        {
            // Can't remove the last King
            if (piece.IsKing && KingCount <= 1)
                return false;

            bool removed = pieces.Remove(piece);
            if (removed)
                OnInventoryChanged?.Invoke();
            return removed;
        }

        /// <summary>
        /// Removes a piece by index.
        /// </summary>
        public bool TryRemoveAt(int index)
        {
            if (index < 0 || index >= pieces.Count)
                return false;

            var piece = pieces[index];
            return TryRemovePiece(piece);
        }

        /// <summary>
        /// Swaps a piece in inventory with a new piece.
        /// </summary>
        public bool TrySwapPiece(InventoryPiece oldPiece, InventoryPiece newPiece)
        {
            int index = pieces.IndexOf(oldPiece);
            if (index < 0)
                return false;

            // Can't swap away the last King
            if (oldPiece.IsKing && KingCount <= 1 && !newPiece.IsKing)
                return false;

            // Can't add another King if we already have one (and not swapping away the King)
            if (newPiece.IsKing && HasKing && !oldPiece.IsKing)
                return false;

            // Assign new ID if not set
            if (newPiece.Id == 0)
                newPiece = new InventoryPiece(newPiece.Type, newPiece.Material, nextId++);

            pieces[index] = newPiece;
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Validates the inventory meets game requirements.
        /// </summary>
        public InventoryValidation Validate()
        {
            var validation = new InventoryValidation();

            if (pieces.Count == 0)
            {
                validation.IsValid = false;
                validation.Errors.Add("Inventory is empty");
                return validation;
            }

            if (pieces.Count > MaxPieces)
            {
                validation.IsValid = false;
                validation.Errors.Add($"Too many pieces ({pieces.Count}/{MaxPieces})");
            }

            if (!HasKing)
            {
                validation.IsValid = false;
                validation.Errors.Add("Must have exactly one King");
            }

            if (KingCount > 1)
            {
                validation.IsValid = false;
                validation.Errors.Add($"Too many Kings ({KingCount})");
            }

            return validation;
        }

        public bool HasKing => pieces.Any(p => p.IsKing);
        public int KingCount => pieces.Count(p => p.IsKing);

        public int GetCount(PieceType type) => pieces.Count(p => p.Type == type);

        public IEnumerable<InventoryPiece> GetPiecesOfType(PieceType type)
            => pieces.Where(p => p.Type == type);

        /// <summary>
        /// Gets pieces sorted for deterministic deployment.
        /// Sort order: Material tier (desc), then ID (asc) for stability.
        /// </summary>
        public List<InventoryPiece> GetSortedForDeployment()
        {
            return pieces
                .OrderByDescending(p => (int)p.Material)
                .ThenBy(p => p.Id)
                .ToList();
        }

        /// <summary>
        /// Creates a deep copy of this inventory.
        /// </summary>
        public PieceInventory Clone()
        {
            var clone = new PieceInventory();
            clone.pieces = new List<InventoryPiece>(pieces);
            clone.nextId = nextId;
            return clone;
        }

        public override string ToString()
        {
            var counts = new Dictionary<PieceType, int>();
            foreach (var piece in pieces)
            {
                counts.TryGetValue(piece.Type, out int count);
                counts[piece.Type] = count + 1;
            }

            var parts = new List<string>();
            if (counts.TryGetValue(PieceType.King, out int k)) parts.Add($"{k}K");
            if (counts.TryGetValue(PieceType.Queen, out int q)) parts.Add($"{q}Q");
            if (counts.TryGetValue(PieceType.Rook, out int r)) parts.Add($"{r}R");
            if (counts.TryGetValue(PieceType.Bishop, out int b)) parts.Add($"{b}B");
            if (counts.TryGetValue(PieceType.Knight, out int n)) parts.Add($"{n}N");
            if (counts.TryGetValue(PieceType.Pawn, out int p)) parts.Add($"{p}P");

            return $"[{string.Join(" ", parts)}] ({pieces.Count}/{MaxPieces})";
        }
    }

    public struct AddPieceResult
    {
        public bool Success;
        public bool RequiresSwap;
        public string Message;
    }

    public class InventoryValidation
    {
        public bool IsValid = true;
        public List<string> Errors = new List<string>();
    }
}
