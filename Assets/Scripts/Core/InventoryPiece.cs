using System;

namespace Shakki.Core
{
    /// <summary>
    /// Material tier affects piece visuals and potentially scoring multipliers.
    /// </summary>
    public enum MaterialTier
    {
        Plastic = 0,    // Baseline starter pieces
        Wood = 1,
        Stone = 2,
        Metal = 3,
        Crystal = 4,
        Gold = 5
    }

    /// <summary>
    /// Represents a piece owned in the player's inventory.
    /// Immutable struct for safe passing around.
    /// </summary>
    [Serializable]
    public struct InventoryPiece : IEquatable<InventoryPiece>
    {
        public PieceType Type;
        public MaterialTier Material;
        public int Id; // Unique identifier for stable sorting

        public InventoryPiece(PieceType type, MaterialTier material = MaterialTier.Plastic, int id = 0)
        {
            Type = type;
            Material = material;
            Id = id;
        }

        /// <summary>
        /// Base point value for scoring captures.
        /// </summary>
        public int BaseValue => Type switch
        {
            PieceType.Pawn => 1,
            PieceType.Knight => 3,
            PieceType.Bishop => 3,
            PieceType.Rook => 5,
            PieceType.Queen => 9,
            PieceType.King => 0,
            _ => 0
        };

        /// <summary>
        /// Sell value in Coins.
        /// </summary>
        public int SellValue => (int)(BaseValue * (1 + (int)Material * 0.5f));

        /// <summary>
        /// Display name for UI.
        /// </summary>
        public string DisplayName => Material == MaterialTier.Plastic
            ? Type.ToString()
            : $"{Material} {Type}";

        public bool IsKing => Type == PieceType.King;

        public override string ToString() => $"{Material} {Type} (#{Id})";

        public bool Equals(InventoryPiece other)
        {
            return Type == other.Type && Material == other.Material && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is InventoryPiece other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Material, Id);
        }

        public static bool operator ==(InventoryPiece left, InventoryPiece right) => left.Equals(right);
        public static bool operator !=(InventoryPiece left, InventoryPiece right) => !left.Equals(right);
    }
}
