using NUnit.Framework;
using Shakki.Core;
using System.Linq;

namespace Shakki.Tests
{
    public class InventoryTests
    {
        [Test]
        public void StandardSet_Has16Pieces()
        {
            var inventory = PieceInventory.CreateStandardSet();
            Assert.AreEqual(16, inventory.Count);
        }

        [Test]
        public void StandardSet_HasOneKing()
        {
            var inventory = PieceInventory.CreateStandardSet();
            Assert.AreEqual(1, inventory.KingCount);
        }

        [Test]
        public void StandardSet_HasCorrectDistribution()
        {
            var inventory = PieceInventory.CreateStandardSet();

            Assert.AreEqual(1, inventory.GetCount(PieceType.King));
            Assert.AreEqual(1, inventory.GetCount(PieceType.Queen));
            Assert.AreEqual(2, inventory.GetCount(PieceType.Rook));
            Assert.AreEqual(2, inventory.GetCount(PieceType.Bishop));
            Assert.AreEqual(2, inventory.GetCount(PieceType.Knight));
            Assert.AreEqual(8, inventory.GetCount(PieceType.Pawn));
        }

        [Test]
        public void CannotAddMoreThan16Pieces()
        {
            var inventory = PieceInventory.CreateStandardSet();
            Assert.IsTrue(inventory.IsFull);

            bool added = inventory.TryAddPiece(new InventoryPiece(PieceType.Pawn));
            Assert.IsFalse(added);
            Assert.AreEqual(16, inventory.Count);
        }

        [Test]
        public void CannotAddSecondKing()
        {
            var inventory = new PieceInventory();
            inventory.TryAddPiece(new InventoryPiece(PieceType.King));

            bool added = inventory.TryAddPiece(new InventoryPiece(PieceType.King));
            Assert.IsFalse(added);
            Assert.AreEqual(1, inventory.KingCount);
        }

        [Test]
        public void CannotRemoveLastKing()
        {
            var inventory = PieceInventory.CreateStandardSet();
            var king = inventory.Pieces.First(p => p.IsKing);

            bool removed = inventory.TryRemovePiece(king);
            Assert.IsFalse(removed);
            Assert.AreEqual(1, inventory.KingCount);
        }

        [Test]
        public void SwapPiece_ReplacesCorrectly()
        {
            var inventory = PieceInventory.CreateStandardSet();
            var pawn = inventory.Pieces.First(p => p.Type == PieceType.Pawn);

            var newQueen = new InventoryPiece(PieceType.Queen, MaterialTier.Gold);
            bool swapped = inventory.TrySwapPiece(pawn, newQueen);

            Assert.IsTrue(swapped);
            Assert.AreEqual(16, inventory.Count);
            Assert.AreEqual(7, inventory.GetCount(PieceType.Pawn));
            Assert.AreEqual(2, inventory.GetCount(PieceType.Queen));
        }

        [Test]
        public void Validation_EmptyInventory_Fails()
        {
            var inventory = new PieceInventory();
            var validation = inventory.Validate();

            Assert.IsFalse(validation.IsValid);
            Assert.IsTrue(validation.Errors.Any(e => e.Contains("empty")));
        }

        [Test]
        public void Validation_NoKing_Fails()
        {
            var inventory = new PieceInventory();
            inventory.TryAddPiece(new InventoryPiece(PieceType.Queen));

            var validation = inventory.Validate();
            Assert.IsFalse(validation.IsValid);
            Assert.IsTrue(validation.Errors.Any(e => e.Contains("King")));
        }

        [Test]
        public void Validation_StandardSet_Passes()
        {
            var inventory = PieceInventory.CreateStandardSet();
            var validation = inventory.Validate();

            Assert.IsTrue(validation.IsValid);
            Assert.AreEqual(0, validation.Errors.Count);
        }
    }

    public class AutoDeployerTests
    {
        [Test]
        public void StandardSet_DeploysAllPieces()
        {
            var inventory = PieceInventory.CreateStandardSet();
            var deployment = AutoDeployer.CalculateDeployment(inventory, PieceColor.White);

            Assert.AreEqual(16, deployment.Count);
        }

        [Test]
        public void StandardSet_KingOnCorrectSquare()
        {
            var inventory = PieceInventory.CreateStandardSet();
            var deployment = AutoDeployer.CalculateDeployment(inventory, PieceColor.White);

            var kingSquare = deployment.FirstOrDefault(kvp => kvp.Value.IsKing).Key;
            Assert.AreEqual(new Square(4, 0), kingSquare); // e1
        }

        [Test]
        public void StandardSet_QueenOnCorrectSquare()
        {
            var inventory = PieceInventory.CreateStandardSet();
            var deployment = AutoDeployer.CalculateDeployment(inventory, PieceColor.White);

            var queenSquare = deployment.FirstOrDefault(kvp => kvp.Value.Type == PieceType.Queen).Key;
            Assert.AreEqual(new Square(3, 0), queenSquare); // d1
        }

        [Test]
        public void NoPawns_FillsBackRank()
        {
            var inventory = new PieceInventory();
            inventory.TryAddPiece(new InventoryPiece(PieceType.King));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Queen));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Queen));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Rook));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Rook));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Bishop));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Bishop));
            inventory.TryAddPiece(new InventoryPiece(PieceType.Knight));

            var deployment = AutoDeployer.CalculateDeployment(inventory, PieceColor.White);

            // All pieces should be on back rank (rank 0)
            Assert.IsTrue(deployment.Keys.All(sq => sq.Rank == 0));
        }

        [Test]
        public void FiveQueens_AllDeployed()
        {
            var inventory = new PieceInventory();
            inventory.TryAddPiece(new InventoryPiece(PieceType.King));
            for (int i = 0; i < 5; i++)
                inventory.TryAddPiece(new InventoryPiece(PieceType.Queen));

            var deployment = AutoDeployer.CalculateDeployment(inventory, PieceColor.White);

            Assert.AreEqual(6, deployment.Count);
            Assert.AreEqual(5, deployment.Values.Count(p => p.Type == PieceType.Queen));
        }

        [Test]
        public void BlackDeployment_UsesCorrectRanks()
        {
            var inventory = PieceInventory.CreateStandardSet();
            var deployment = AutoDeployer.CalculateDeployment(inventory, PieceColor.Black);

            // Back rank pieces should be on rank 7
            var kingSquare = deployment.FirstOrDefault(kvp => kvp.Value.IsKing).Key;
            Assert.AreEqual(7, kingSquare.Rank);

            // Pawns should be on rank 6
            var pawnSquares = deployment.Where(kvp => kvp.Value.Type == PieceType.Pawn).Select(kvp => kvp.Key);
            Assert.IsTrue(pawnSquares.All(sq => sq.Rank == 6));
        }

        [Test]
        public void OverflowPieces_GoToPawnRank()
        {
            var inventory = new PieceInventory();
            inventory.TryAddPiece(new InventoryPiece(PieceType.King));
            // Add 9 queens (more than back rank can hold)
            for (int i = 0; i < 9; i++)
                inventory.TryAddPiece(new InventoryPiece(PieceType.Queen));

            var deployment = AutoDeployer.CalculateDeployment(inventory, PieceColor.White);

            // Should have pieces on both ranks
            Assert.IsTrue(deployment.Keys.Any(sq => sq.Rank == 0)); // back rank
            Assert.IsTrue(deployment.Keys.Any(sq => sq.Rank == 1)); // pawn rank
        }

        [Test]
        public void ApplyDeployment_SetsBoard()
        {
            var inventory = PieceInventory.CreateStandardSet();
            var board = new Board();
            board.Clear();

            AutoDeployer.Deploy(board, inventory, PieceColor.White);

            // Check king is placed
            var kingPiece = board[4, 0];
            Assert.AreEqual(PieceType.King, kingPiece.Type);
            Assert.AreEqual(PieceColor.White, kingPiece.Color);
        }
    }
}
