using UnityEngine;

namespace Chess
{
    public class ChessPiece
    {
        public PieceType Type { get; set; }
        public PieceColor Color { get; set; }
        public Vector2Int Position { get; set; }
        public GameObject GameObject { get; set; }
        public SpriteRenderer SpriteRenderer { get; set; }
    }
}
