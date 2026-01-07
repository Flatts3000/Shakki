using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public static class ChessMoveValidator
    {
        public static List<Vector2Int> GetValidMoves(ChessPiece piece, ChessPiece[,] board)
        {
            var rawMoves = GetRawMoves(piece, board);
            var legalMoves = new List<Vector2Int>();

            foreach (var move in rawMoves)
            {
                if (!WouldBeInCheck(piece, move, board))
                {
                    legalMoves.Add(move);
                }
            }

            return legalMoves;
        }

        public static bool IsInCheck(PieceColor color, ChessPiece[,] board)
        {
            Vector2Int? kingPos = FindKing(color, board);
            if (!kingPos.HasValue) return false;

            return IsSquareAttacked(kingPos.Value, color, board);
        }

        public static bool IsCheckmate(PieceColor color, ChessPiece[,] board)
        {
            if (!IsInCheck(color, board)) return false;
            return !HasAnyLegalMoves(color, board);
        }

        public static bool IsStalemate(PieceColor color, ChessPiece[,] board)
        {
            if (IsInCheck(color, board)) return false;
            return !HasAnyLegalMoves(color, board);
        }

        private static bool HasAnyLegalMoves(PieceColor color, ChessPiece[,] board)
        {
            for (int x = 0; x < ChessConstants.BoardSize; x++)
            {
                for (int y = 0; y < ChessConstants.BoardSize; y++)
                {
                    ChessPiece piece = board[x, y];
                    if (piece != null && piece.Color == color)
                    {
                        var moves = GetValidMoves(piece, board);
                        if (moves.Count > 0) return true;
                    }
                }
            }
            return false;
        }

        private static bool WouldBeInCheck(ChessPiece piece, Vector2Int to, ChessPiece[,] board)
        {
            Vector2Int from = piece.Position;
            ChessPiece captured = board[to.x, to.y];

            // Simulate the move
            board[to.x, to.y] = piece;
            board[from.x, from.y] = null;
            Vector2Int originalPos = piece.Position;
            piece.Position = to;

            bool inCheck = IsInCheck(piece.Color, board);

            // Undo the move
            board[from.x, from.y] = piece;
            board[to.x, to.y] = captured;
            piece.Position = originalPos;

            return inCheck;
        }

        private static bool IsSquareAttacked(Vector2Int square, PieceColor defendingColor, ChessPiece[,] board)
        {
            PieceColor attackingColor = defendingColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            for (int x = 0; x < ChessConstants.BoardSize; x++)
            {
                for (int y = 0; y < ChessConstants.BoardSize; y++)
                {
                    ChessPiece piece = board[x, y];
                    if (piece != null && piece.Color == attackingColor)
                    {
                        var attacks = GetAttackSquares(piece, board);
                        if (attacks.Contains(square)) return true;
                    }
                }
            }
            return false;
        }

        private static List<Vector2Int> GetAttackSquares(ChessPiece piece, ChessPiece[,] board)
        {
            var attacks = new List<Vector2Int>();

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    int direction = piece.Color == PieceColor.White ? 1 : -1;
                    Vector2Int left = new Vector2Int(piece.Position.x - 1, piece.Position.y + direction);
                    Vector2Int right = new Vector2Int(piece.Position.x + 1, piece.Position.y + direction);
                    if (IsValidPosition(left)) attacks.Add(left);
                    if (IsValidPosition(right)) attacks.Add(right);
                    break;
                case PieceType.Knight:
                    AddKnightAttacks(piece, attacks);
                    break;
                case PieceType.Bishop:
                    AddLineAttacks(piece, board, attacks, new[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) });
                    break;
                case PieceType.Rook:
                    AddLineAttacks(piece, board, attacks, new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right });
                    break;
                case PieceType.Queen:
                    AddLineAttacks(piece, board, attacks, new[] {
                        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                    });
                    break;
                case PieceType.King:
                    AddKingAttacks(piece, attacks);
                    break;
            }

            return attacks;
        }

        private static void AddKnightAttacks(ChessPiece piece, List<Vector2Int> attacks)
        {
            Vector2Int[] offsets = {
                new Vector2Int(1, 2), new Vector2Int(2, 1),
                new Vector2Int(2, -1), new Vector2Int(1, -2),
                new Vector2Int(-1, -2), new Vector2Int(-2, -1),
                new Vector2Int(-2, 1), new Vector2Int(-1, 2)
            };

            foreach (var offset in offsets)
            {
                Vector2Int target = piece.Position + offset;
                if (IsValidPosition(target)) attacks.Add(target);
            }
        }

        private static void AddKingAttacks(ChessPiece piece, List<Vector2Int> attacks)
        {
            Vector2Int[] offsets = {
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(1, -1),
                new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1)
            };

            foreach (var offset in offsets)
            {
                Vector2Int target = piece.Position + offset;
                if (IsValidPosition(target)) attacks.Add(target);
            }
        }

        private static void AddLineAttacks(ChessPiece piece, ChessPiece[,] board, List<Vector2Int> attacks, Vector2Int[] directions)
        {
            foreach (var dir in directions)
            {
                Vector2Int current = piece.Position + dir;
                while (IsValidPosition(current))
                {
                    attacks.Add(current);
                    if (board[current.x, current.y] != null) break;
                    current += dir;
                }
            }
        }

        private static List<Vector2Int> GetRawMoves(ChessPiece piece, ChessPiece[,] board)
        {
            var moves = new List<Vector2Int>();
            if (piece == null) return moves;

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    AddPawnMoves(piece, board, moves);
                    break;
                case PieceType.Rook:
                    AddLineMoves(piece, board, moves, new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right });
                    break;
                case PieceType.Knight:
                    AddKnightMoves(piece, board, moves);
                    break;
                case PieceType.Bishop:
                    AddLineMoves(piece, board, moves, new[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) });
                    break;
                case PieceType.Queen:
                    AddLineMoves(piece, board, moves, new[] {
                        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                    });
                    break;
                case PieceType.King:
                    AddKingMoves(piece, board, moves);
                    break;
            }

            return moves;
        }

        private static void AddPawnMoves(ChessPiece piece, ChessPiece[,] board, List<Vector2Int> moves)
        {
            int direction = piece.Color == PieceColor.White ? 1 : -1;
            int startRow = piece.Color == PieceColor.White ? 1 : 6;
            Vector2Int pos = piece.Position;

            Vector2Int oneForward = new Vector2Int(pos.x, pos.y + direction);
            if (IsValidPosition(oneForward) && board[oneForward.x, oneForward.y] == null)
            {
                moves.Add(oneForward);

                if (pos.y == startRow)
                {
                    Vector2Int twoForward = new Vector2Int(pos.x, pos.y + 2 * direction);
                    if (IsValidPosition(twoForward) && board[twoForward.x, twoForward.y] == null)
                    {
                        moves.Add(twoForward);
                    }
                }
            }

            Vector2Int[] captures = { new Vector2Int(pos.x - 1, pos.y + direction), new Vector2Int(pos.x + 1, pos.y + direction) };
            foreach (var capture in captures)
            {
                if (IsValidPosition(capture))
                {
                    ChessPiece target = board[capture.x, capture.y];
                    if (target != null && target.Color != piece.Color)
                    {
                        moves.Add(capture);
                    }
                }
            }
        }

        private static void AddKnightMoves(ChessPiece piece, ChessPiece[,] board, List<Vector2Int> moves)
        {
            Vector2Int[] offsets = {
                new Vector2Int(1, 2), new Vector2Int(2, 1),
                new Vector2Int(2, -1), new Vector2Int(1, -2),
                new Vector2Int(-1, -2), new Vector2Int(-2, -1),
                new Vector2Int(-2, 1), new Vector2Int(-1, 2)
            };

            foreach (var offset in offsets)
            {
                Vector2Int target = piece.Position + offset;
                if (IsValidPosition(target))
                {
                    ChessPiece targetPiece = board[target.x, target.y];
                    if (targetPiece == null || targetPiece.Color != piece.Color)
                    {
                        moves.Add(target);
                    }
                }
            }
        }

        private static void AddKingMoves(ChessPiece piece, ChessPiece[,] board, List<Vector2Int> moves)
        {
            Vector2Int[] offsets = {
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(1, -1),
                new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1)
            };

            foreach (var offset in offsets)
            {
                Vector2Int target = piece.Position + offset;
                if (IsValidPosition(target))
                {
                    ChessPiece targetPiece = board[target.x, target.y];
                    if (targetPiece == null || targetPiece.Color != piece.Color)
                    {
                        moves.Add(target);
                    }
                }
            }
        }

        private static void AddLineMoves(ChessPiece piece, ChessPiece[,] board, List<Vector2Int> moves, Vector2Int[] directions)
        {
            foreach (var dir in directions)
            {
                Vector2Int current = piece.Position + dir;
                while (IsValidPosition(current))
                {
                    ChessPiece targetPiece = board[current.x, current.y];
                    if (targetPiece == null)
                    {
                        moves.Add(current);
                    }
                    else
                    {
                        if (targetPiece.Color != piece.Color)
                        {
                            moves.Add(current);
                        }
                        break;
                    }
                    current += dir;
                }
            }
        }

        private static Vector2Int? FindKing(PieceColor color, ChessPiece[,] board)
        {
            for (int x = 0; x < ChessConstants.BoardSize; x++)
            {
                for (int y = 0; y < ChessConstants.BoardSize; y++)
                {
                    ChessPiece piece = board[x, y];
                    if (piece != null && piece.Type == PieceType.King && piece.Color == color)
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
            return null;
        }

        private static bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < ChessConstants.BoardSize &&
                   pos.y >= 0 && pos.y < ChessConstants.BoardSize;
        }
    }
}
