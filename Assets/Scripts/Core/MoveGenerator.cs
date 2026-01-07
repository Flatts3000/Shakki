using System.Collections.Generic;

namespace Shakki.Core
{
    public static class MoveGenerator
    {
        public static List<Move> GenerateLegalMoves(Board board)
        {
            var pseudoLegal = GeneratePseudoLegalMoves(board);
            var legal = new List<Move>();

            foreach (var move in pseudoLegal)
            {
                var testBoard = board.Clone();
                testBoard.MakeMove(move);

                // After making the move, check if our king is in check
                // (side that just moved, which is now the opponent's turn)
                var ourColor = board.SideToMove;
                if (!IsKingInCheck(testBoard, ourColor))
                {
                    legal.Add(move);
                }
            }

            return legal;
        }

        public static List<Move> GeneratePseudoLegalMoves(Board board)
        {
            var moves = new List<Move>();
            var side = board.SideToMove;

            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    var piece = board[f, r];
                    if (piece.IsEmpty || piece.Color != side)
                        continue;

                    var from = new Square(f, r);
                    GeneratePieceMoves(board, from, piece, moves);
                }
            }

            return moves;
        }

        private static void GeneratePieceMoves(Board board, Square from, Piece piece, List<Move> moves)
        {
            switch (piece.Type)
            {
                case PieceType.Pawn:
                    GeneratePawnMoves(board, from, piece, moves);
                    break;
                case PieceType.Knight:
                    GenerateKnightMoves(board, from, piece, moves);
                    break;
                case PieceType.Bishop:
                    GenerateSlidingMoves(board, from, piece, moves, bishopDirections);
                    break;
                case PieceType.Rook:
                    GenerateSlidingMoves(board, from, piece, moves, rookDirections);
                    break;
                case PieceType.Queen:
                    GenerateSlidingMoves(board, from, piece, moves, queenDirections);
                    break;
                case PieceType.King:
                    GenerateKingMoves(board, from, piece, moves);
                    break;
            }
        }

        private static readonly (int, int)[] knightOffsets = {
            (-2, -1), (-2, 1), (-1, -2), (-1, 2),
            (1, -2), (1, 2), (2, -1), (2, 1)
        };

        private static readonly (int, int)[] kingOffsets = {
            (-1, -1), (-1, 0), (-1, 1), (0, -1),
            (0, 1), (1, -1), (1, 0), (1, 1)
        };

        private static readonly (int, int)[] bishopDirections = {
            (-1, -1), (-1, 1), (1, -1), (1, 1)
        };

        private static readonly (int, int)[] rookDirections = {
            (-1, 0), (1, 0), (0, -1), (0, 1)
        };

        private static readonly (int, int)[] queenDirections = {
            (-1, -1), (-1, 0), (-1, 1), (0, -1),
            (0, 1), (1, -1), (1, 0), (1, 1)
        };

        private static void GeneratePawnMoves(Board board, Square from, Piece piece, List<Move> moves)
        {
            int direction = piece.IsWhite ? 1 : -1;
            int startRank = piece.IsWhite ? 1 : 6;
            int promoRank = piece.IsWhite ? 7 : 0;

            // Single push
            var oneStep = new Square(from.File, from.Rank + direction);
            if (oneStep.IsValid && board[oneStep].IsEmpty)
            {
                if (oneStep.Rank == promoRank)
                    AddPromotionMoves(moves, from, oneStep, MoveFlags.None);
                else
                    moves.Add(new Move(from, oneStep));

                // Double push
                if (from.Rank == startRank)
                {
                    var twoStep = new Square(from.File, from.Rank + 2 * direction);
                    if (board[twoStep].IsEmpty)
                    {
                        moves.Add(new Move(from, twoStep, PieceType.None, MoveFlags.DoublePawnPush));
                    }
                }
            }

            // Captures
            int[] captureFiles = { from.File - 1, from.File + 1 };
            foreach (int cf in captureFiles)
            {
                var captureSq = new Square(cf, from.Rank + direction);
                if (!captureSq.IsValid) continue;

                var target = board[captureSq];
                if (!target.IsEmpty && target.Color != piece.Color)
                {
                    if (captureSq.Rank == promoRank)
                        AddPromotionMoves(moves, from, captureSq, MoveFlags.Capture);
                    else
                        moves.Add(new Move(from, captureSq, PieceType.None, MoveFlags.Capture));
                }

                // En passant
                if (board.EnPassantSquare.HasValue && captureSq == board.EnPassantSquare.Value)
                {
                    moves.Add(new Move(from, captureSq, PieceType.None, MoveFlags.Capture | MoveFlags.EnPassant));
                }
            }
        }

        private static void AddPromotionMoves(List<Move> moves, Square from, Square to, MoveFlags flags)
        {
            moves.Add(new Move(from, to, PieceType.Queen, flags));
            moves.Add(new Move(from, to, PieceType.Rook, flags));
            moves.Add(new Move(from, to, PieceType.Bishop, flags));
            moves.Add(new Move(from, to, PieceType.Knight, flags));
        }

        private static void GenerateKnightMoves(Board board, Square from, Piece piece, List<Move> moves)
        {
            foreach (var (df, dr) in knightOffsets)
            {
                var to = new Square(from.File + df, from.Rank + dr);
                if (!to.IsValid) continue;

                var target = board[to];
                if (target.IsEmpty)
                    moves.Add(new Move(from, to));
                else if (target.Color != piece.Color)
                    moves.Add(new Move(from, to, PieceType.None, MoveFlags.Capture));
            }
        }

        private static void GenerateSlidingMoves(Board board, Square from, Piece piece, List<Move> moves, (int, int)[] directions)
        {
            foreach (var (df, dr) in directions)
            {
                for (int dist = 1; dist < 8; dist++)
                {
                    var to = new Square(from.File + df * dist, from.Rank + dr * dist);
                    if (!to.IsValid) break;

                    var target = board[to];
                    if (target.IsEmpty)
                    {
                        moves.Add(new Move(from, to));
                    }
                    else
                    {
                        if (target.Color != piece.Color)
                            moves.Add(new Move(from, to, PieceType.None, MoveFlags.Capture));
                        break;
                    }
                }
            }
        }

        private static void GenerateKingMoves(Board board, Square from, Piece piece, List<Move> moves)
        {
            // Normal king moves
            foreach (var (df, dr) in kingOffsets)
            {
                var to = new Square(from.File + df, from.Rank + dr);
                if (!to.IsValid) continue;

                var target = board[to];
                if (target.IsEmpty)
                    moves.Add(new Move(from, to));
                else if (target.Color != piece.Color)
                    moves.Add(new Move(from, to, PieceType.None, MoveFlags.Capture));
            }

            // Castling
            if (piece.IsWhite)
            {
                if (board.WhiteCanCastleKingside)
                    TryAddCastlingMove(board, from, piece, moves, true);
                if (board.WhiteCanCastleQueenside)
                    TryAddCastlingMove(board, from, piece, moves, false);
            }
            else
            {
                if (board.BlackCanCastleKingside)
                    TryAddCastlingMove(board, from, piece, moves, true);
                if (board.BlackCanCastleQueenside)
                    TryAddCastlingMove(board, from, piece, moves, false);
            }
        }

        private static void TryAddCastlingMove(Board board, Square from, Piece piece, List<Move> moves, bool kingside)
        {
            int rank = piece.IsWhite ? 0 : 7;

            // King must be on starting square
            if (from.File != 4 || from.Rank != rank)
                return;

            // Can't castle out of check
            if (IsKingInCheck(board, piece.Color))
                return;

            if (kingside)
            {
                // Squares between king and rook must be empty
                if (!board[5, rank].IsEmpty || !board[6, rank].IsEmpty)
                    return;

                // King can't pass through check
                if (IsSquareAttacked(board, new Square(5, rank), piece.Color) ||
                    IsSquareAttacked(board, new Square(6, rank), piece.Color))
                    return;

                moves.Add(new Move(from, new Square(6, rank), PieceType.None, MoveFlags.KingsideCastle));
            }
            else
            {
                // Squares between king and rook must be empty
                if (!board[1, rank].IsEmpty || !board[2, rank].IsEmpty || !board[3, rank].IsEmpty)
                    return;

                // King can't pass through check
                if (IsSquareAttacked(board, new Square(2, rank), piece.Color) ||
                    IsSquareAttacked(board, new Square(3, rank), piece.Color))
                    return;

                moves.Add(new Move(from, new Square(2, rank), PieceType.None, MoveFlags.QueensideCastle));
            }
        }

        public static bool IsKingInCheck(Board board, PieceColor kingColor)
        {
            var kingSq = board.FindKing(kingColor);
            if (!kingSq.IsValid) return false;
            return IsSquareAttacked(board, kingSq, kingColor);
        }

        public static bool IsSquareAttacked(Board board, Square square, PieceColor defendingColor)
        {
            var attackingColor = defendingColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

            // Check for pawn attacks
            int pawnDir = defendingColor == PieceColor.White ? 1 : -1;
            int[] pawnFiles = { square.File - 1, square.File + 1 };
            foreach (int f in pawnFiles)
            {
                var pawnSq = new Square(f, square.Rank + pawnDir);
                if (pawnSq.IsValid)
                {
                    var piece = board[pawnSq];
                    if (piece.Type == PieceType.Pawn && piece.Color == attackingColor)
                        return true;
                }
            }

            // Check for knight attacks
            foreach (var (df, dr) in knightOffsets)
            {
                var knightSq = new Square(square.File + df, square.Rank + dr);
                if (knightSq.IsValid)
                {
                    var piece = board[knightSq];
                    if (piece.Type == PieceType.Knight && piece.Color == attackingColor)
                        return true;
                }
            }

            // Check for king attacks
            foreach (var (df, dr) in kingOffsets)
            {
                var kingSq = new Square(square.File + df, square.Rank + dr);
                if (kingSq.IsValid)
                {
                    var piece = board[kingSq];
                    if (piece.Type == PieceType.King && piece.Color == attackingColor)
                        return true;
                }
            }

            // Check for sliding piece attacks (bishop/queen on diagonals)
            foreach (var (df, dr) in bishopDirections)
            {
                for (int dist = 1; dist < 8; dist++)
                {
                    var sq = new Square(square.File + df * dist, square.Rank + dr * dist);
                    if (!sq.IsValid) break;

                    var piece = board[sq];
                    if (!piece.IsEmpty)
                    {
                        if (piece.Color == attackingColor &&
                            (piece.Type == PieceType.Bishop || piece.Type == PieceType.Queen))
                            return true;
                        break;
                    }
                }
            }

            // Check for sliding piece attacks (rook/queen on ranks/files)
            foreach (var (df, dr) in rookDirections)
            {
                for (int dist = 1; dist < 8; dist++)
                {
                    var sq = new Square(square.File + df * dist, square.Rank + dr * dist);
                    if (!sq.IsValid) break;

                    var piece = board[sq];
                    if (!piece.IsEmpty)
                    {
                        if (piece.Color == attackingColor &&
                            (piece.Type == PieceType.Rook || piece.Type == PieceType.Queen))
                            return true;
                        break;
                    }
                }
            }

            return false;
        }
    }
}
