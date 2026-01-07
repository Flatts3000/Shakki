using System.Collections.Generic;
using System.Linq;

namespace Shakki.Core
{
    /// <summary>
    /// Automatically deploys pieces from inventory onto the board.
    /// Implements the deterministic placement algorithm from the design doc.
    /// </summary>
    public static class AutoDeployer
    {
        // Center-out file order for symmetric placement
        private static readonly int[] CenterOutFiles = { 3, 4, 2, 5, 1, 6, 0, 7 }; // d, e, c, f, b, g, a, h

        /// <summary>
        /// Deploys pieces from inventory onto the board for the given color.
        /// </summary>
        public static void Deploy(Board board, PieceInventory inventory, PieceColor color)
        {
            var deployment = CalculateDeployment(inventory, color);
            ApplyDeployment(board, deployment, color);
        }

        /// <summary>
        /// Calculates piece placements without modifying the board.
        /// Returns a dictionary mapping squares to inventory pieces.
        /// </summary>
        public static Dictionary<Square, InventoryPiece> CalculateDeployment(PieceInventory inventory, PieceColor color)
        {
            var result = new Dictionary<Square, InventoryPiece>();
            var pieces = inventory.GetSortedForDeployment();

            int backRank = color == PieceColor.White ? 0 : 7;
            int pawnRank = color == PieceColor.White ? 1 : 6;

            // Track which squares are filled
            var filledSquares = new HashSet<Square>();

            // Track remaining pieces by type
            var remaining = new Dictionary<PieceType, Queue<InventoryPiece>>();
            foreach (var pieceType in new[] { PieceType.King, PieceType.Queen, PieceType.Rook,
                                               PieceType.Bishop, PieceType.Knight, PieceType.Pawn })
            {
                remaining[pieceType] = new Queue<InventoryPiece>(
                    pieces.Where(p => p.Type == pieceType));
            }

            // Step A: Place Pawns on pawn rank (center-out)
            PlacePawns(result, filledSquares, remaining[PieceType.Pawn], pawnRank);

            // Step B: Place King and Queen
            PlaceKing(result, filledSquares, remaining[PieceType.King], backRank, pawnRank);
            PlaceQueen(result, filledSquares, remaining[PieceType.Queen], backRank, pawnRank);

            // Step C: Place Rooks (prefer a/h files)
            PlaceRooks(result, filledSquares, remaining[PieceType.Rook], backRank);

            // Step D: Place Bishops (prefer c/f files), then Knights (prefer b/g files)
            PlaceBishops(result, filledSquares, remaining[PieceType.Bishop], backRank);
            PlaceKnights(result, filledSquares, remaining[PieceType.Knight], backRank);

            // Step E: Place any remaining pieces
            PlaceRemaining(result, filledSquares, remaining, backRank, pawnRank);

            return result;
        }

        private static void PlacePawns(Dictionary<Square, InventoryPiece> result,
            HashSet<Square> filled, Queue<InventoryPiece> pawns, int pawnRank)
        {
            foreach (int file in CenterOutFiles)
            {
                if (pawns.Count == 0) break;

                var square = new Square(file, pawnRank);
                var pawn = pawns.Dequeue();
                result[square] = pawn;
                filled.Add(square);
            }
        }

        private static void PlaceKing(Dictionary<Square, InventoryPiece> result,
            HashSet<Square> filled, Queue<InventoryPiece> kings, int backRank, int pawnRank)
        {
            if (kings.Count == 0) return;

            var king = kings.Dequeue();

            // Prefer e-file (index 4) on back rank
            var preferredSquares = new[]
            {
                new Square(4, backRank),  // e1/e8
                new Square(3, backRank),  // d1/d8
                new Square(5, backRank),  // f1/f8
                new Square(2, backRank),  // c1/c8
                new Square(6, backRank),  // g1/g8
                new Square(1, backRank),  // b1/b8
                new Square(7, backRank),  // h1/h8
                new Square(0, backRank),  // a1/a8
            };

            var square = FindFirstEmpty(preferredSquares, filled)
                ?? FindFirstEmptyOnRanks(filled, backRank, pawnRank);

            if (square.HasValue)
            {
                result[square.Value] = king;
                filled.Add(square.Value);
            }
        }

        private static void PlaceQueen(Dictionary<Square, InventoryPiece> result,
            HashSet<Square> filled, Queue<InventoryPiece> queens, int backRank, int pawnRank)
        {
            foreach (var queen in queens.ToList())
            {
                queens.Dequeue();

                // First queen prefers d-file
                var preferredSquares = new[]
                {
                    new Square(3, backRank),  // d1/d8
                    new Square(4, backRank),  // e1/e8
                    new Square(2, backRank),  // c1/c8
                    new Square(5, backRank),  // f1/f8
                    new Square(1, backRank),  // b1/b8
                    new Square(6, backRank),  // g1/g8
                    new Square(0, backRank),  // a1/a8
                    new Square(7, backRank),  // h1/h8
                };

                var square = FindFirstEmpty(preferredSquares, filled)
                    ?? FindFirstEmptyOnRanks(filled, backRank, pawnRank);

                if (square.HasValue)
                {
                    result[square.Value] = queen;
                    filled.Add(square.Value);
                }
            }
        }

        private static void PlaceRooks(Dictionary<Square, InventoryPiece> result,
            HashSet<Square> filled, Queue<InventoryPiece> rooks, int backRank)
        {
            // Rooks prefer corner files, placed symmetrically
            var rookFiles = new[] { 0, 7, 1, 6, 2, 5, 3, 4 }; // a, h, b, g, c, f, d, e

            foreach (int file in rookFiles)
            {
                if (rooks.Count == 0) break;

                var square = new Square(file, backRank);
                if (!filled.Contains(square))
                {
                    result[square] = rooks.Dequeue();
                    filled.Add(square);
                }
            }
        }

        private static void PlaceBishops(Dictionary<Square, InventoryPiece> result,
            HashSet<Square> filled, Queue<InventoryPiece> bishops, int backRank)
        {
            // Bishops prefer c/f files
            var bishopFiles = new[] { 2, 5, 1, 6, 3, 4, 0, 7 }; // c, f, b, g, d, e, a, h

            foreach (int file in bishopFiles)
            {
                if (bishops.Count == 0) break;

                var square = new Square(file, backRank);
                if (!filled.Contains(square))
                {
                    result[square] = bishops.Dequeue();
                    filled.Add(square);
                }
            }
        }

        private static void PlaceKnights(Dictionary<Square, InventoryPiece> result,
            HashSet<Square> filled, Queue<InventoryPiece> knights, int backRank)
        {
            // Knights prefer b/g files
            var knightFiles = new[] { 1, 6, 2, 5, 0, 7, 3, 4 }; // b, g, c, f, a, h, d, e

            foreach (int file in knightFiles)
            {
                if (knights.Count == 0) break;

                var square = new Square(file, backRank);
                if (!filled.Contains(square))
                {
                    result[square] = knights.Dequeue();
                    filled.Add(square);
                }
            }
        }

        private static void PlaceRemaining(Dictionary<Square, InventoryPiece> result,
            HashSet<Square> filled, Dictionary<PieceType, Queue<InventoryPiece>> remaining,
            int backRank, int pawnRank)
        {
            // Collect all remaining pieces
            var allRemaining = new List<InventoryPiece>();
            foreach (var queue in remaining.Values)
            {
                while (queue.Count > 0)
                    allRemaining.Add(queue.Dequeue());
            }

            if (allRemaining.Count == 0) return;

            // Fill back rank first (center-out), then pawn rank (center-out)
            var emptySquares = new List<Square>();

            // Back rank center-out
            foreach (int file in CenterOutFiles)
            {
                var square = new Square(file, backRank);
                if (!filled.Contains(square))
                    emptySquares.Add(square);
            }

            // Pawn rank center-out
            foreach (int file in CenterOutFiles)
            {
                var square = new Square(file, pawnRank);
                if (!filled.Contains(square))
                    emptySquares.Add(square);
            }

            // Place remaining pieces
            int i = 0;
            foreach (var piece in allRemaining)
            {
                if (i >= emptySquares.Count) break;
                result[emptySquares[i]] = piece;
                filled.Add(emptySquares[i]);
                i++;
            }
        }

        private static Square? FindFirstEmpty(Square[] squares, HashSet<Square> filled)
        {
            foreach (var sq in squares)
            {
                if (!filled.Contains(sq))
                    return sq;
            }
            return null;
        }

        private static Square? FindFirstEmptyOnRanks(HashSet<Square> filled, int backRank, int pawnRank)
        {
            // Back rank first, center-out
            foreach (int file in CenterOutFiles)
            {
                var square = new Square(file, backRank);
                if (!filled.Contains(square))
                    return square;
            }

            // Then pawn rank, center-out
            foreach (int file in CenterOutFiles)
            {
                var square = new Square(file, pawnRank);
                if (!filled.Contains(square))
                    return square;
            }

            return null;
        }

        /// <summary>
        /// Applies a calculated deployment to the board.
        /// </summary>
        public static void ApplyDeployment(Board board, Dictionary<Square, InventoryPiece> deployment, PieceColor color)
        {
            foreach (var kvp in deployment)
            {
                var square = kvp.Key;
                var invPiece = kvp.Value;
                board[square] = new Piece(invPiece.Type, color);
            }
        }

        /// <summary>
        /// Sets up both sides of the board from inventories.
        /// </summary>
        public static void SetupMatch(Board board, PieceInventory whiteInventory, PieceInventory blackInventory)
        {
            board.Clear();
            Deploy(board, whiteInventory, PieceColor.White);
            Deploy(board, blackInventory, PieceColor.Black);
        }
    }
}
