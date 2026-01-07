using System;
using System.Collections.Generic;

namespace Shakki.Core
{
    /// <summary>
    /// Result of a Shakki match with score-race rules.
    /// </summary>
    public enum ShakkiMatchResult
    {
        InProgress,
        WhiteWinsByScore,
        BlackWinsByScore,
        WhiteWinsByCheckmate,
        BlackWinsByCheckmate,
        WhiteWinsByRoundLimit,
        BlackWinsByRoundLimit,
        Draw
    }

    /// <summary>
    /// Manages a Shakki match with score-race rules layered on top of chess.
    /// </summary>
    public class ShakkiMatchState
    {
        public GameState ChessState { get; private set; }
        public ShakkiMatchConfig Config { get; private set; }

        public int WhiteScore { get; private set; }
        public int BlackScore { get; private set; }
        public int CurrentRound { get; private set; } = 1;
        public ShakkiMatchResult Result { get; private set; } = ShakkiMatchResult.InProgress;

        private bool whiteMovedThisRound = false;
        private bool inSuddenDeath = false;
        private int suddenDeathStartRound = 0;

        public event Action<Piece, PieceColor, int> OnPieceCaptured; // piece, capturer, points
        public event Action<int> OnRoundComplete; // round number
        public event Action<ShakkiMatchResult> OnMatchEnd;

        public PieceInventory WhiteInventory { get; private set; }
        public PieceInventory BlackInventory { get; private set; }

        public ShakkiMatchState(ShakkiMatchConfig config = null)
            : this(config, PieceInventory.CreateStandardSet(), PieceInventory.CreateStandardSet())
        {
        }

        public ShakkiMatchState(ShakkiMatchConfig config, PieceInventory whiteInventory, PieceInventory blackInventory)
        {
            Config = config ?? ShakkiMatchConfig.Default;
            WhiteInventory = whiteInventory;
            BlackInventory = blackInventory;

            // Create board and deploy pieces using AutoDeployer
            var board = new Board();
            board.Clear();
            AutoDeployer.Deploy(board, WhiteInventory, PieceColor.White);
            AutoDeployer.Deploy(board, BlackInventory, PieceColor.Black);

            ChessState = new GameState(board);
            WhiteScore = 0;
            BlackScore = 0;
            CurrentRound = 1;
            whiteMovedThisRound = false;
        }

        public PieceColor CurrentPlayer => ChessState.CurrentPlayer;
        public Board Board => ChessState.Board;
        public bool IsMatchOver => Result != ShakkiMatchResult.InProgress;
        public bool IsInCheck => ChessState.IsInCheck;

        public List<Move> GetLegalMoves() => ChessState.GetLegalMoves();
        public List<Move> GetLegalMovesFrom(Square from) => ChessState.GetLegalMovesFrom(from);
        public Move? FindLegalMove(Square from, Square to, PieceType promotion = PieceType.None)
            => ChessState.FindLegalMove(from, to, promotion);

        /// <summary>
        /// Attempts to make a move. Returns true if successful.
        /// </summary>
        public bool TryMakeMove(Square from, Square to, PieceType promotion = PieceType.None)
        {
            if (IsMatchOver) return false;

            var move = ChessState.FindLegalMove(from, to, promotion);
            if (!move.HasValue) return false;

            return MakeMove(move.Value);
        }

        /// <summary>
        /// Makes a validated move and processes Shakki rules.
        /// </summary>
        public bool MakeMove(Move move)
        {
            if (IsMatchOver) return false;

            var movingColor = CurrentPlayer;
            var capturedPiece = GetCapturedPiece(move);

            // Make the chess move
            if (!ChessState.MakeMove(move))
                return false;

            // Award points for capture
            if (!capturedPiece.IsEmpty)
            {
                int points = CalculateCapturePoints(capturedPiece);
                if (movingColor == PieceColor.White)
                    WhiteScore += points;
                else
                    BlackScore += points;

                OnPieceCaptured?.Invoke(capturedPiece, movingColor, points);
            }

            // Check for checkmate (ends immediately, overrides round timing)
            if (ChessState.IsCheckmate)
            {
                Result = movingColor == PieceColor.White
                    ? ShakkiMatchResult.BlackWinsByCheckmate
                    : ShakkiMatchResult.WhiteWinsByCheckmate;
                OnMatchEnd?.Invoke(Result);
                return true;
            }

            // Check for stalemate
            if (ChessState.IsStalemate)
            {
                Result = ShakkiMatchResult.Draw;
                OnMatchEnd?.Invoke(Result);
                return true;
            }

            // Track round progress
            if (movingColor == PieceColor.White)
            {
                whiteMovedThisRound = true;
            }
            else if (whiteMovedThisRound) // Black just moved after White, round complete
            {
                CompleteRound();
            }

            return true;
        }

        private Piece GetCapturedPiece(Move move)
        {
            if (move.IsEnPassant)
            {
                int capturedRank = CurrentPlayer == PieceColor.White
                    ? move.To.Rank - 1
                    : move.To.Rank + 1;
                return Board[move.To.File, capturedRank];
            }
            return Board[move.To];
        }

        private int CalculateCapturePoints(Piece piece)
        {
            // Base piece values - can be modified by bounties/modifiers later
            return piece.BaseValue;
        }

        private void CompleteRound()
        {
            OnRoundComplete?.Invoke(CurrentRound);

            // Check score-based win conditions at end of round
            bool whiteReachedTarget = WhiteScore >= Config.TargetScore;
            bool blackReachedTarget = BlackScore >= Config.TargetScore;

            if (whiteReachedTarget || blackReachedTarget)
            {
                if (whiteReachedTarget && !blackReachedTarget)
                {
                    Result = ShakkiMatchResult.WhiteWinsByScore;
                    OnMatchEnd?.Invoke(Result);
                    return;
                }
                else if (blackReachedTarget && !whiteReachedTarget)
                {
                    Result = ShakkiMatchResult.BlackWinsByScore;
                    OnMatchEnd?.Invoke(Result);
                    return;
                }
                else // Both reached target
                {
                    // Higher score wins
                    if (WhiteScore > BlackScore)
                    {
                        Result = ShakkiMatchResult.WhiteWinsByScore;
                        OnMatchEnd?.Invoke(Result);
                        return;
                    }
                    else if (BlackScore > WhiteScore)
                    {
                        Result = ShakkiMatchResult.BlackWinsByScore;
                        OnMatchEnd?.Invoke(Result);
                        return;
                    }
                    else // Tied at or above target - sudden death
                    {
                        if (!inSuddenDeath)
                        {
                            inSuddenDeath = true;
                            suddenDeathStartRound = CurrentRound;
                        }
                        else
                        {
                            // In sudden death, first to lead wins
                            // If still tied, continue
                        }
                    }
                }
            }

            // Check round limit
            if (Config.RoundLimit > 0 && CurrentRound >= Config.RoundLimit)
            {
                // Round limit reached - higher score wins
                if (WhiteScore > BlackScore)
                {
                    Result = ShakkiMatchResult.WhiteWinsByRoundLimit;
                    OnMatchEnd?.Invoke(Result);
                    return;
                }
                else if (BlackScore > WhiteScore)
                {
                    Result = ShakkiMatchResult.BlackWinsByRoundLimit;
                    OnMatchEnd?.Invoke(Result);
                    return;
                }
                else
                {
                    // Tied at round limit - one sudden death round
                    if (!inSuddenDeath)
                    {
                        inSuddenDeath = true;
                        suddenDeathStartRound = CurrentRound;
                    }
                    else
                    {
                        // Already in sudden death and still tied - draw
                        Result = ShakkiMatchResult.Draw;
                        OnMatchEnd?.Invoke(Result);
                        return;
                    }
                }
            }

            // Advance to next round
            CurrentRound++;
            whiteMovedThisRound = false;
        }

        /// <summary>
        /// Gets a display-friendly result string.
        /// </summary>
        public string GetResultString()
        {
            return Result switch
            {
                ShakkiMatchResult.WhiteWinsByScore => $"White wins by score! ({WhiteScore} - {BlackScore})",
                ShakkiMatchResult.BlackWinsByScore => $"Black wins by score! ({BlackScore} - {WhiteScore})",
                ShakkiMatchResult.WhiteWinsByCheckmate => "White wins by checkmate!",
                ShakkiMatchResult.BlackWinsByCheckmate => "Black wins by checkmate!",
                ShakkiMatchResult.WhiteWinsByRoundLimit => $"White wins on round limit! ({WhiteScore} - {BlackScore})",
                ShakkiMatchResult.BlackWinsByRoundLimit => $"Black wins on round limit! ({BlackScore} - {WhiteScore})",
                ShakkiMatchResult.Draw => "Draw!",
                _ => "Match in progress"
            };
        }

        public bool IsInSuddenDeath => inSuddenDeath;

        public int RoundsRemaining => Config.RoundLimit > 0
            ? Math.Max(0, Config.RoundLimit - CurrentRound + 1)
            : -1; // -1 indicates unlimited
    }
}
