using System.Collections.Generic;

namespace Shakki.Core
{
    public class GameState
    {
        public Board Board { get; private set; }
        public List<Move> MoveHistory { get; private set; } = new List<Move>();
        public List<Piece> CapturedByWhite { get; private set; } = new List<Piece>();
        public List<Piece> CapturedByBlack { get; private set; } = new List<Piece>();
        public GameResult Result { get; private set; } = GameResult.InProgress;

        private List<Move> cachedLegalMoves;
        private bool legalMovesCacheValid = false;

        public GameState()
        {
            Board = new Board();
            Board.SetupStandardPosition();
            InvalidateMoveCache();
        }

        public GameState(Board board)
        {
            Board = board;
            InvalidateMoveCache();
        }

        public PieceColor CurrentPlayer => Board.SideToMove;

        public List<Move> GetLegalMoves()
        {
            if (!legalMovesCacheValid)
            {
                cachedLegalMoves = MoveGenerator.GenerateLegalMoves(Board);
                legalMovesCacheValid = true;
            }
            return cachedLegalMoves;
        }

        public List<Move> GetLegalMovesFrom(Square from)
        {
            var allMoves = GetLegalMoves();
            var moves = new List<Move>();
            foreach (var move in allMoves)
            {
                if (move.From == from)
                    moves.Add(move);
            }
            return moves;
        }

        public bool IsLegalMove(Move move)
        {
            var legalMoves = GetLegalMoves();
            foreach (var legalMove in legalMoves)
            {
                if (legalMove.From == move.From && legalMove.To == move.To)
                {
                    // For promotions, check if the promotion type matches
                    if (legalMove.IsPromotion && move.IsPromotion)
                        return legalMove.Promotion == move.Promotion;
                    // If legal move is a promotion but input isn't, still allow (will default to queen)
                    return true;
                }
            }
            return false;
        }

        public Move? FindLegalMove(Square from, Square to, PieceType promotion = PieceType.None)
        {
            var legalMoves = GetLegalMoves();
            Move? found = null;

            foreach (var move in legalMoves)
            {
                if (move.From == from && move.To == to)
                {
                    if (move.IsPromotion)
                    {
                        // If a specific promotion is requested, match it
                        if (promotion != PieceType.None && move.Promotion == promotion)
                            return move;
                        // Default to queen promotion
                        if (move.Promotion == PieceType.Queen)
                            found = move;
                    }
                    else
                    {
                        return move;
                    }
                }
            }

            return found;
        }

        public bool TryMakeMove(Move move)
        {
            if (Result != GameResult.InProgress)
                return false;

            var legalMove = FindLegalMove(move.From, move.To, move.Promotion);
            if (!legalMove.HasValue)
                return false;

            return MakeMove(legalMove.Value);
        }

        public bool MakeMove(Move move)
        {
            if (Result != GameResult.InProgress)
                return false;

            // Track captured piece
            var capturedPiece = Board[move.To];
            if (move.IsEnPassant)
            {
                int capturedRank = Board.SideToMove == PieceColor.White
                    ? move.To.Rank - 1
                    : move.To.Rank + 1;
                capturedPiece = Board[move.To.File, capturedRank];
            }

            if (!capturedPiece.IsEmpty)
            {
                if (Board.SideToMove == PieceColor.White)
                    CapturedByWhite.Add(capturedPiece);
                else
                    CapturedByBlack.Add(capturedPiece);
            }

            Board.MakeMove(move);
            MoveHistory.Add(move);
            InvalidateMoveCache();

            // Check for game end
            UpdateGameResult();

            return true;
        }

        private void InvalidateMoveCache()
        {
            legalMovesCacheValid = false;
            cachedLegalMoves = null;
        }

        private void UpdateGameResult()
        {
            var legalMoves = GetLegalMoves();

            if (legalMoves.Count == 0)
            {
                if (IsInCheck)
                {
                    // Checkmate
                    Result = Board.SideToMove == PieceColor.White
                        ? GameResult.BlackWins
                        : GameResult.WhiteWins;
                }
                else
                {
                    // Stalemate
                    Result = GameResult.Draw;
                }
            }
            else if (IsDrawByInsufficientMaterial() || IsDrawByFiftyMoveRule())
            {
                Result = GameResult.Draw;
            }
        }

        public bool IsInCheck => MoveGenerator.IsKingInCheck(Board, Board.SideToMove);

        public bool IsCheckmate => Result == GameResult.WhiteWins || Result == GameResult.BlackWins;

        public bool IsStalemate => Result == GameResult.Draw && GetLegalMoves().Count == 0;

        public bool IsGameOver => Result != GameResult.InProgress;

        private bool IsDrawByFiftyMoveRule()
        {
            return Board.HalfmoveClock >= 100;
        }

        private bool IsDrawByInsufficientMaterial()
        {
            int whitePawns = 0, blackPawns = 0;
            int whiteKnights = 0, blackKnights = 0;
            int whiteBishops = 0, blackBishops = 0;
            int whiteRooks = 0, blackRooks = 0;
            int whiteQueens = 0, blackQueens = 0;

            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    var piece = Board[f, r];
                    if (piece.IsEmpty) continue;

                    switch (piece.Type)
                    {
                        case PieceType.Pawn:
                            if (piece.IsWhite) whitePawns++; else blackPawns++;
                            break;
                        case PieceType.Knight:
                            if (piece.IsWhite) whiteKnights++; else blackKnights++;
                            break;
                        case PieceType.Bishop:
                            if (piece.IsWhite) whiteBishops++; else blackBishops++;
                            break;
                        case PieceType.Rook:
                            if (piece.IsWhite) whiteRooks++; else blackRooks++;
                            break;
                        case PieceType.Queen:
                            if (piece.IsWhite) whiteQueens++; else blackQueens++;
                            break;
                    }
                }
            }

            // If any pawns, rooks, or queens exist, sufficient material
            if (whitePawns > 0 || blackPawns > 0) return false;
            if (whiteRooks > 0 || blackRooks > 0) return false;
            if (whiteQueens > 0 || blackQueens > 0) return false;

            int whitePieces = whiteKnights + whiteBishops;
            int blackPieces = blackKnights + blackBishops;

            // King vs King
            if (whitePieces == 0 && blackPieces == 0) return true;

            // King + minor vs King
            if (whitePieces == 0 && blackPieces == 1) return true;
            if (whitePieces == 1 && blackPieces == 0) return true;

            // King + 2 knights vs King (technically insufficient but rare)
            if (whitePieces == 0 && blackKnights == 2 && blackBishops == 0) return true;
            if (blackPieces == 0 && whiteKnights == 2 && whiteBishops == 0) return true;

            return false;
        }

        public int GetWhiteScore()
        {
            int score = 0;
            foreach (var piece in CapturedByWhite)
                score += piece.BaseValue;
            return score;
        }

        public int GetBlackScore()
        {
            int score = 0;
            foreach (var piece in CapturedByBlack)
                score += piece.BaseValue;
            return score;
        }
    }
}
