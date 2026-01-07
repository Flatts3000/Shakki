using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public enum GameState
    {
        Playing,
        Checkmate,
        Stalemate
    }

    public class ChessGame : MonoBehaviour
    {
        public static ChessGame Instance { get; private set; }

        private ChessBoard board;
        private PieceColor currentTurn = PieceColor.White;
        private Vector2Int? selectedPosition = null;
        private List<Vector2Int> currentValidMoves = new List<Vector2Int>();
        private GameState gameState = GameState.Playing;

        public PieceColor CurrentTurn => currentTurn;
        public GameState State => gameState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            board = GetComponent<ChessBoard>();
        }

        public void HandleClick(Vector3 worldPos)
        {
            if (gameState != GameState.Playing) return;

            Vector2Int boardPos = board.WorldToBoard(worldPos);

            if (!board.IsValidPosition(boardPos)) return;

            ChessPiece clickedPiece = board.GetPieceAt(boardPos);

            if (selectedPosition == null)
            {
                if (clickedPiece != null && clickedPiece.Color == currentTurn)
                {
                    SelectPiece(boardPos, clickedPiece);
                }
            }
            else
            {
                if (boardPos == selectedPosition.Value)
                {
                    ClearSelection();
                }
                else if (clickedPiece != null && clickedPiece.Color == currentTurn)
                {
                    SelectPiece(boardPos, clickedPiece);
                }
                else if (currentValidMoves.Contains(boardPos))
                {
                    ExecuteMove(boardPos);
                }
            }
        }

        private void ExecuteMove(Vector2Int to)
        {
            board.MovePiece(selectedPosition.Value, to);
            ClearSelection();

            currentTurn = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;

            UpdateGameState();
        }

        private void UpdateGameState()
        {
            ChessPiece[,] boardState = board.GetBoard();
            bool inCheck = ChessMoveValidator.IsInCheck(currentTurn, boardState);

            board.HideCheck();

            if (inCheck)
            {
                Vector2Int? kingPos = board.FindKing(currentTurn);
                if (kingPos.HasValue)
                {
                    board.ShowCheck(kingPos.Value);
                }

                if (ChessMoveValidator.IsCheckmate(currentTurn, boardState))
                {
                    gameState = GameState.Checkmate;
                    PieceColor winner = currentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    Debug.Log($"Checkmate! {winner} wins!");
                }
                else
                {
                    Debug.Log($"{currentTurn} is in check!");
                }
            }
            else if (ChessMoveValidator.IsStalemate(currentTurn, boardState))
            {
                gameState = GameState.Stalemate;
                Debug.Log("Stalemate! The game is a draw.");
            }
        }

        private void SelectPiece(Vector2Int pos, ChessPiece piece)
        {
            selectedPosition = pos;
            currentValidMoves = ChessMoveValidator.GetValidMoves(piece, board.GetBoard());
            board.ShowSelectionAt(pos);
            board.ShowValidMoves(currentValidMoves);
        }

        private void ClearSelection()
        {
            selectedPosition = null;
            currentValidMoves.Clear();
            board.HideSelection();
        }
    }
}
