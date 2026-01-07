using System.Collections.Generic;

namespace Shakki.Core
{
    /// <summary>
    /// Simple chess AI using Minimax with Alpha-Beta pruning.
    /// Evaluates positions based on material advantage.
    /// </summary>
    public static class ChessAI
    {
        private const int PositiveInfinity = 999999;
        private const int NegativeInfinity = -999999;

        // Opening book: common first moves for variety
        private static readonly Move[] WhiteOpenings = new Move[]
        {
            new Move(new Square(4, 1), new Square(4, 3)), // e4 - King's Pawn
            new Move(new Square(3, 1), new Square(3, 3)), // d4 - Queen's Pawn
            new Move(new Square(2, 1), new Square(2, 3)), // c4 - English Opening
            new Move(new Square(6, 0), new Square(5, 2)), // Nf3 - Reti Opening
        };

        private static readonly Move[] BlackResponses_e4 = new Move[]
        {
            new Move(new Square(4, 6), new Square(4, 4)), // e5 - Open Game
            new Move(new Square(2, 6), new Square(2, 4)), // c5 - Sicilian
            new Move(new Square(4, 6), new Square(4, 5)), // e6 - French
            new Move(new Square(2, 6), new Square(2, 5)), // c6 - Caro-Kann
        };

        private static readonly Move[] BlackResponses_d4 = new Move[]
        {
            new Move(new Square(3, 6), new Square(3, 4)), // d5 - Closed Game
            new Move(new Square(6, 7), new Square(5, 5)), // Nf6 - Indian Defense
            new Move(new Square(4, 6), new Square(4, 5)), // e6 - Queen's Gambit style
        };

        /// <summary>
        /// Chooses the best move for the current player.
        /// Randomly selects among equally-scored moves for variety.
        /// </summary>
        /// <param name="state">Current match state</param>
        /// <param name="depth">Search depth (3-4 recommended)</param>
        public static Move? GetBestMove(ShakkiMatchState state, int depth = 3)
        {
            var legalMoves = state.GetLegalMoves();
            if (legalMoves.Count == 0)
                return null;

            // Try opening book first for early game variety
            var openingMove = TryGetOpeningMove(state, legalMoves);
            if (openingMove.HasValue)
                return openingMove;

            // Collect all moves with their scores
            var scoredMoves = new List<(Move move, int score)>();
            bool isMaximizing = state.CurrentPlayer == PieceColor.White;

            foreach (var move in legalMoves)
            {
                // Create a copy of the board to test the move
                var testBoard = state.Board.Clone();
                testBoard.MakeMove(move);

                int score = Minimax(testBoard, depth - 1, NegativeInfinity, PositiveInfinity, !isMaximizing);

                // Flip score for black (black wants lower scores)
                if (!isMaximizing)
                    score = -score;

                scoredMoves.Add((move, score));
            }

            // Find the best score
            int bestScore = NegativeInfinity;
            foreach (var (_, score) in scoredMoves)
            {
                if (score > bestScore)
                    bestScore = score;
            }

            // Collect all moves with the best score
            var bestMoves = new List<Move>();
            foreach (var (move, score) in scoredMoves)
            {
                if (score == bestScore)
                    bestMoves.Add(move);
            }

            // Randomly select among the best moves
            Move? bestMove = bestMoves[UnityEngine.Random.Range(0, bestMoves.Count)];

            // Handle promotion
            if (bestMove.HasValue && bestMove.Value.IsPromotion && bestMove.Value.Promotion == PieceType.None)
            {
                var m = bestMove.Value;
                bestMove = new Move(m.From, m.To, PieceType.Queen, m.Flags);
            }

            return bestMove;
        }

        /// <summary>
        /// Tries to get a move from the opening book for early game variety.
        /// </summary>
        private static Move? TryGetOpeningMove(ShakkiMatchState state, IReadOnlyList<Move> legalMoves)
        {
            int moveCount = state.ChessState.MoveHistory.Count;

            // Only use opening book for first few moves
            if (moveCount > 3)
                return null;

            Move[] candidates = null;

            if (state.CurrentPlayer == PieceColor.White)
            {
                if (moveCount == 0)
                {
                    // White's first move
                    candidates = WhiteOpenings;
                }
            }
            else
            {
                if (moveCount == 1)
                {
                    // Black's first move - respond based on White's opening
                    var whiteMove = state.ChessState.MoveHistory[0];
                    if (whiteMove.From.File == 4 && whiteMove.To.File == 4) // e4
                    {
                        candidates = BlackResponses_e4;
                    }
                    else if (whiteMove.From.File == 3 && whiteMove.To.File == 3) // d4
                    {
                        candidates = BlackResponses_d4;
                    }
                }
            }

            if (candidates == null || candidates.Length == 0)
                return null;

            // Filter to only legal moves from candidates
            var validCandidates = new List<Move>();
            foreach (var candidate in candidates)
            {
                foreach (var legal in legalMoves)
                {
                    if (legal.From == candidate.From && legal.To == candidate.To)
                    {
                        validCandidates.Add(legal);
                        break;
                    }
                }
            }

            if (validCandidates.Count == 0)
                return null;

            // Randomly pick from valid opening moves
            return validCandidates[UnityEngine.Random.Range(0, validCandidates.Count)];
        }

        private static int Minimax(Board board, int depth, int alpha, int beta, bool isMaximizing)
        {
            if (depth == 0)
                return EvaluateBoard(board);

            var tempState = new GameState(board);
            var moves = tempState.GetLegalMoves();

            if (moves.Count == 0)
            {
                // No legal moves - checkmate or stalemate
                if (tempState.IsInCheck)
                    return isMaximizing ? NegativeInfinity : PositiveInfinity;
                return 0; // Stalemate
            }

            if (isMaximizing)
            {
                int maxEval = NegativeInfinity;
                foreach (var move in moves)
                {
                    var newBoard = board.Clone();
                    newBoard.MakeMove(move);
                    int eval = Minimax(newBoard, depth - 1, alpha, beta, false);
                    maxEval = System.Math.Max(maxEval, eval);
                    alpha = System.Math.Max(alpha, eval);
                    if (beta <= alpha)
                        break;
                }
                return maxEval;
            }
            else
            {
                int minEval = PositiveInfinity;
                foreach (var move in moves)
                {
                    var newBoard = board.Clone();
                    newBoard.MakeMove(move);
                    int eval = Minimax(newBoard, depth - 1, alpha, beta, true);
                    minEval = System.Math.Min(minEval, eval);
                    beta = System.Math.Min(beta, eval);
                    if (beta <= alpha)
                        break;
                }
                return minEval;
            }
        }

        /// <summary>
        /// Evaluates the board position. Positive = White advantage, Negative = Black advantage.
        /// </summary>
        private static int EvaluateBoard(Board board)
        {
            int score = 0;

            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    var piece = board[file, rank];
                    if (piece.IsEmpty) continue;

                    int pieceValue = GetPieceValue(piece.Type);

                    // Add positional bonus
                    pieceValue += GetPositionalBonus(piece, file, rank);

                    if (piece.Color == PieceColor.White)
                        score += pieceValue;
                    else
                        score -= pieceValue;
                }
            }

            return score;
        }

        private static int GetPieceValue(PieceType type)
        {
            return type switch
            {
                PieceType.Pawn => 100,
                PieceType.Knight => 320,
                PieceType.Bishop => 330,
                PieceType.Rook => 500,
                PieceType.Queen => 900,
                PieceType.King => 20000,
                _ => 0
            };
        }

        /// <summary>
        /// Simple positional bonuses to encourage good piece placement.
        /// </summary>
        private static int GetPositionalBonus(Piece piece, int file, int rank)
        {
            // Encourage pawns to advance
            if (piece.Type == PieceType.Pawn)
            {
                if (piece.Color == PieceColor.White)
                    return rank * 5;
                else
                    return (7 - rank) * 5;
            }

            // Encourage knights and bishops toward center
            if (piece.Type == PieceType.Knight || piece.Type == PieceType.Bishop)
            {
                int centerDistFile = System.Math.Abs(file - 3);
                int centerDistRank = System.Math.Abs(rank - 3);
                return 10 - (centerDistFile + centerDistRank) * 2;
            }

            return 0;
        }
    }
}
