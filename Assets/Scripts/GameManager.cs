using UnityEngine;
using System;
using System.Collections;
using Shakki.Core;
using Shakki.UI;

namespace Shakki
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // Events for audio and other systems
        public event Action<Move, bool> OnMoveMade; // move, isCapture
        public event Action<ShakkiMatchResult> OnMatchEnded;

        [Header("References")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private GameHUD gameHUD;
        [SerializeField] private RunManager runManager;
        [SerializeField] private GameFlowController flowController;

        [Header("AI Settings")]
        [SerializeField] private int aiSearchDepth = 3;
        [SerializeField] private float aiMoveDelay = 0.3f;
        [SerializeField] private bool aiPlaysWhite = false;

        [Header("Debug")]
        [SerializeField] private bool logMoves = true;
        [SerializeField] private bool enableDebugUI = true;

        private ShakkiMatchState matchState;
        private Square? selectedSquare = null;
        private DebugInventoryUI debugUI;
        private bool isMatchActive = false;
        private bool isAIThinking = false;

        public ShakkiMatchState MatchState => matchState;
        public int CurrentLevel => runManager?.CurrentRun?.CurrentLevel ?? 1;
        public PieceInventory PlayerInventory => runManager?.GetPlayerInventory();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (boardView == null)
                boardView = FindFirstObjectByType<BoardView>();

            if (gameHUD == null)
                gameHUD = FindFirstObjectByType<GameHUD>();

            if (runManager == null)
                runManager = FindFirstObjectByType<RunManager>();

            if (flowController == null)
                flowController = FindFirstObjectByType<GameFlowController>();

            // Create HUD if not found
            if (gameHUD == null)
            {
                var hudObj = new GameObject("GameHUD");
                hudObj.transform.SetParent(transform);
                gameHUD = hudObj.AddComponent<GameHUD>();
            }

            // Create debug UI
            if (enableDebugUI)
            {
                var debugObj = new GameObject("DebugInventoryUI");
                debugObj.transform.SetParent(transform);
                debugUI = debugObj.AddComponent<DebugInventoryUI>();
            }
        }

        private void Start()
        {
            // Subscribe to game flow events
            if (flowController != null)
            {
                flowController.OnStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (flowController != null)
            {
                flowController.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameFlowController.GameState oldState, GameFlowController.GameState newState)
        {
            if (newState == GameFlowController.GameState.InMatch)
            {
                StartNewMatch();
            }
            else if (newState == GameFlowController.GameState.PostMatch || newState == GameFlowController.GameState.RunEnd)
            {
                // Keep board visible so player can analyze the final position
                isMatchActive = false;
            }
            else
            {
                // Hide board for menu/pre-match screens
                isMatchActive = false;
                if (boardView != null)
                {
                    boardView.gameObject.SetActive(false);
                }
            }
        }

        private void OnEnable()
        {
            if (boardView != null)
            {
                boardView.OnSquareClicked += HandleSquareClicked;
                boardView.OnPieceDragged += HandlePieceDragged;
            }
        }

        private void OnDisable()
        {
            if (boardView != null)
            {
                boardView.OnSquareClicked -= HandleSquareClicked;
                boardView.OnPieceDragged -= HandlePieceDragged;
            }
        }

        public void StartNewMatch()
        {
            // Get configuration from run manager
            var config = runManager?.GetCurrentMatchConfig() ?? ShakkiMatchConfig.Default;
            var playerInventory = runManager?.GetPlayerInventory() ?? PieceInventory.CreateStandardSet();

            // Validate inventory before starting
            var validation = playerInventory.Validate();
            if (!validation.IsValid)
            {
                Debug.LogError($"Invalid inventory: {string.Join(", ", validation.Errors)}");
                playerInventory = PieceInventory.CreateStandardSet();
            }

            // Create opponent inventory (standard set for now, could be varied later)
            var opponentInventory = PieceInventory.CreateStandardSet();

            // Create match with inventories
            matchState = new ShakkiMatchState(config, playerInventory, opponentInventory);
            matchState.OnPieceCaptured += HandlePieceCaptured;
            matchState.OnRoundComplete += HandleRoundComplete;
            matchState.OnMatchEnd += HandleMatchEnd;

            selectedSquare = null;
            isMatchActive = true;

            // Show board and update display
            boardView.gameObject.SetActive(true);
            boardView.ClearSelection();
            boardView.SetLastMove(null);
            boardView.UpdateBoard(matchState.Board);

            gameHUD.BindMatch(matchState);

            // Bind debug UI if available
            if (debugUI != null)
            {
                debugUI.Bind(playerInventory, null);
            }

            if (logMoves)
            {
                int level = runManager?.CurrentRun?.CurrentLevel ?? 1;
                Debug.Log($"=== Level {level} Match Started ===");
                Debug.Log($"Target Score: {config.TargetScore}, Round Limit: {config.RoundLimit}");
                Debug.Log($"White army: {playerInventory}");
                if (aiPlaysWhite)
                    Debug.Log("AI vs AI mode enabled.");
                else
                    Debug.Log("White to move. Press F1 for debug inventory editor.");
            }

            // If AI plays White, start the AI
            if (aiPlaysWhite)
            {
                StartCoroutine(MakeAIMove());
            }
        }

        private void HandleSquareClicked(Square square)
        {
            // Don't handle clicks if not in active match, match is over, or AI is thinking
            if (!isMatchActive || matchState == null || matchState.IsMatchOver || isAIThinking)
                return;

            // Don't allow clicks if AI is playing White
            if (aiPlaysWhite)
                return;

            // Player is always White
            if (matchState.CurrentPlayer != PieceColor.White)
                return;

            var clickedPiece = matchState.Board[square];

            // If we have a selection, try to make a move
            if (selectedSquare.HasValue)
            {
                // Check if clicking on a legal move destination
                var move = matchState.FindLegalMove(selectedSquare.Value, square);
                if (move.HasValue)
                {
                    MakeMove(move.Value);
                    // Trigger AI response
                    if (!matchState.IsMatchOver && matchState.CurrentPlayer == PieceColor.Black)
                    {
                        StartCoroutine(MakeAIMove());
                    }
                    return;
                }

                // Clicking on own piece - change selection
                if (!clickedPiece.IsEmpty && clickedPiece.Color == PieceColor.White)
                {
                    SelectSquare(square);
                    return;
                }

                // Clicking elsewhere - clear selection
                ClearSelection();
                return;
            }

            // No selection - try to select a White piece
            if (!clickedPiece.IsEmpty && clickedPiece.Color == PieceColor.White)
            {
                SelectSquare(square);
            }
        }

        private void HandlePieceDragged(Square from, Square to)
        {
            // Don't handle drags if not in active match, match is over, or AI is thinking
            if (!isMatchActive || matchState == null || matchState.IsMatchOver || isAIThinking)
                return;

            // Don't allow drags if AI is playing White
            if (aiPlaysWhite)
                return;

            // Player is always White
            if (matchState.CurrentPlayer != PieceColor.White)
                return;

            // Check if it's the player's piece
            var piece = matchState.Board[from];
            if (piece.IsEmpty || piece.Color != PieceColor.White)
                return;

            // Try to make the move
            var move = matchState.FindLegalMove(from, to);
            if (move.HasValue)
            {
                MakeMove(move.Value);
                // Trigger AI response
                if (!matchState.IsMatchOver && matchState.CurrentPlayer == PieceColor.Black)
                {
                    StartCoroutine(MakeAIMove());
                }
            }
            else
            {
                // Invalid move - select the piece instead
                SelectSquare(from);
            }
        }

        private IEnumerator MakeAIMove()
        {
            isAIThinking = true;

            // Small delay so player can see the board state
            yield return new WaitForSeconds(aiMoveDelay);

            // AI plays for Black always, and for White if aiPlaysWhite is enabled
            bool shouldAIMove = !matchState.IsMatchOver &&
                (matchState.CurrentPlayer == PieceColor.Black ||
                 (aiPlaysWhite && matchState.CurrentPlayer == PieceColor.White));

            if (shouldAIMove)
            {
                var aiMove = ChessAI.GetBestMove(matchState, aiSearchDepth);
                if (aiMove.HasValue)
                {
                    MakeMove(aiMove.Value);

                    // Continue AI loop if match isn't over
                    if (!matchState.IsMatchOver)
                    {
                        bool nextPlayerIsAI = matchState.CurrentPlayer == PieceColor.Black ||
                                              (aiPlaysWhite && matchState.CurrentPlayer == PieceColor.White);
                        if (nextPlayerIsAI)
                        {
                            isAIThinking = false;
                            StartCoroutine(MakeAIMove());
                            yield break;
                        }
                    }
                }
            }

            isAIThinking = false;
        }

        private void SelectSquare(Square square)
        {
            selectedSquare = square;
            var legalMoves = matchState.GetLegalMovesFrom(square);
            boardView.SetSelection(square, legalMoves);

            if (logMoves)
            {
                var piece = matchState.Board[square];
                Debug.Log($"Selected {piece.Type} at {square}. {legalMoves.Count} legal moves.");
            }
        }

        private void ClearSelection()
        {
            selectedSquare = null;
            boardView.ClearSelection();
        }

        private void MakeMove(Move move)
        {
            // Handle promotion - always promote to queen
            if (move.IsPromotion && move.Promotion == PieceType.None)
            {
                move = new Move(move.From, move.To, PieceType.Queen, move.Flags);
            }

            var movingPiece = matchState.Board[move.From];
            var capturedPiece = matchState.Board[move.To];

            if (matchState.MakeMove(move))
            {
                if (logMoves)
                {
                    string player = movingPiece.Color == PieceColor.White ? "White" : "Black";
                    string moveStr = $"R{matchState.CurrentRound} {player}: {movingPiece.Type} {move.From} -> {move.To}";
                    if (move.IsCapture)
                        moveStr += $" captures {capturedPiece.Type}";
                    if (move.IsPromotion)
                        moveStr += $" promotes to {move.Promotion}";
                    if (move.IsCastling)
                        moveStr += " (castling)";

                    Debug.Log(moveStr);
                }

                selectedSquare = null;
                boardView.ClearSelection();
                boardView.SetLastMove(move);
                boardView.UpdateBoard(matchState.Board);
                gameHUD.UpdateDisplay();

                // Fire event for audio and other systems
                OnMoveMade?.Invoke(move, move.IsCapture);
            }
        }

        private void HandlePieceCaptured(Piece piece, PieceColor capturer, int points)
        {
            if (logMoves)
            {
                string capturerName = capturer == PieceColor.White ? "White" : "Black";
                Debug.Log($"{capturerName} captured {piece.Type} for {points} points! " +
                    $"(White: {matchState.WhiteScore}, Black: {matchState.BlackScore})");
            }
        }

        private void HandleRoundComplete(int round)
        {
            if (logMoves)
            {
                Debug.Log($"--- Round {round} complete ---");
                Debug.Log($"Score: White {matchState.WhiteScore} - Black {matchState.BlackScore} " +
                    $"(Target: {matchState.Config.TargetScore})");

                if (matchState.IsInSuddenDeath)
                    Debug.Log("SUDDEN DEATH!");
            }
        }

        private void HandleMatchEnd(ShakkiMatchResult result)
        {
            if (logMoves)
            {
                Debug.Log("=== MATCH OVER ===");
                Debug.Log(matchState.GetResultString());
                Debug.Log($"Final: White {matchState.WhiteScore} - Black {matchState.BlackScore}");
            }

            isMatchActive = false;

            // Fire event for audio and other systems
            OnMatchEnded?.Invoke(result);

            // Report result to RunManager which will handle progression
            runManager?.ReportMatchResult(result, matchState.WhiteScore, matchState.BlackScore);
        }

        // Public methods for external control
        public bool TryMakeMove(Square from, Square to, PieceType promotion = PieceType.None)
        {
            var move = matchState.FindLegalMove(from, to, promotion);
            if (move.HasValue)
            {
                MakeMove(move.Value);
                return true;
            }
            return false;
        }
    }
}
