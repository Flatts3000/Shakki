using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public enum PieceType
    {
        None,
        King,
        Queen,
        Rook,
        Bishop,
        Knight,
        Pawn
    }

    public enum PieceColor
    {
        None,
        White,
        Black
    }

    public static class ChessConstants
    {
        public const int BoardSize = 8;
        public const float SquareSize = 1f;
        public const float BoardOffset = -3.5f;

        public static readonly Color LightSquare = new Color(0.93f, 0.87f, 0.73f);
        public static readonly Color DarkSquare = new Color(0.46f, 0.30f, 0.18f);
        public static readonly Color SelectionHighlight = new Color(0.5f, 0.8f, 0.3f, 0.6f);
        public static readonly Color ValidMoveHighlight = new Color(0.3f, 0.5f, 0.8f, 0.5f);

        public static readonly Dictionary<(PieceType, PieceColor), string> PieceSymbols = new()
        {
            { (PieceType.King, PieceColor.White), "K" },
            { (PieceType.Queen, PieceColor.White), "Q" },
            { (PieceType.Rook, PieceColor.White), "R" },
            { (PieceType.Bishop, PieceColor.White), "B" },
            { (PieceType.Knight, PieceColor.White), "N" },
            { (PieceType.Pawn, PieceColor.White), "P" },
            { (PieceType.King, PieceColor.Black), "K" },
            { (PieceType.Queen, PieceColor.Black), "Q" },
            { (PieceType.Rook, PieceColor.Black), "R" },
            { (PieceType.Bishop, PieceColor.Black), "B" },
            { (PieceType.Knight, PieceColor.Black), "N" },
            { (PieceType.Pawn, PieceColor.Black), "P" }
        };

        public static readonly Color WhitePieceColor = new Color(1f, 1f, 1f);
        public static readonly Color BlackPieceColor = new Color(0.1f, 0.1f, 0.1f);
        public static readonly Color CheckHighlight = new Color(0.9f, 0.2f, 0.2f, 0.6f);

        public static readonly PieceType[] BackRank =
        {
            PieceType.Rook,
            PieceType.Knight,
            PieceType.Bishop,
            PieceType.Queen,
            PieceType.King,
            PieceType.Bishop,
            PieceType.Knight,
            PieceType.Rook
        };
    }
}
