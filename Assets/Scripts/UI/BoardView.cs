using UnityEngine;
using UnityEngine.InputSystem;
using Shakki.Core;
using System.Collections.Generic;

namespace Shakki.UI
{
    public class BoardView : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private float squareSize = 1f;
        [SerializeField] private float pieceScale = 0.85f; // Scale pieces to fit in squares
        [SerializeField] private Color lightSquareColor = new Color(0.93f, 0.85f, 0.71f);
        [SerializeField] private Color darkSquareColor = new Color(0.71f, 0.53f, 0.39f);
        [SerializeField] private Color selectedColor = new Color(0.5f, 0.8f, 0.5f, 0.7f);
        [SerializeField] private Color legalMoveColor = new Color(0.5f, 0.5f, 0.8f, 0.5f);
        [SerializeField] private Color lastMoveColor = new Color(0.8f, 0.8f, 0.3f, 0.5f);
        [SerializeField] private Color dragHighlightColor = new Color(0.3f, 0.7f, 0.3f, 0.8f);

        [Header("Touch Settings")]
        [SerializeField] private bool enableDragToMove = true;
        [SerializeField] private float dragThreshold = 0.2f; // Min distance to start drag
        [SerializeField] private float dragPieceScale = 1.2f; // Scale piece while dragging
        [SerializeField] private Vector3 dragOffset = new Vector3(0, 0.5f, 0); // Offset so finger doesn't cover piece

        [Header("Piece Sprites")]
        [SerializeField] private Sprite whitePawnSprite;
        [SerializeField] private Sprite whiteKnightSprite;
        [SerializeField] private Sprite whiteBishopSprite;
        [SerializeField] private Sprite whiteRookSprite;
        [SerializeField] private Sprite whiteQueenSprite;
        [SerializeField] private Sprite whiteKingSprite;
        [SerializeField] private Sprite blackPawnSprite;
        [SerializeField] private Sprite blackKnightSprite;
        [SerializeField] private Sprite blackBishopSprite;
        [SerializeField] private Sprite blackRookSprite;
        [SerializeField] private Sprite blackQueenSprite;
        [SerializeField] private Sprite blackKingSprite;

        private GameObject[,] squareObjects = new GameObject[8, 8];
        private SpriteRenderer[,] squareRenderers = new SpriteRenderer[8, 8];
        private GameObject[,] pieceObjects = new GameObject[8, 8];
        private SpriteRenderer[,] pieceRenderers = new SpriteRenderer[8, 8];
        private GameObject[,] highlightObjects = new GameObject[8, 8];
        private SpriteRenderer[,] highlightRenderers = new SpriteRenderer[8, 8];

        private Square? selectedSquare = null;
        private List<Move> legalMovesFromSelected = new List<Move>();
        private Move? lastMove = null;

        // Drag state
        private bool isDragging = false;
        private Square? dragStartSquare = null;
        private Vector3 dragStartWorldPos;
        private GameObject dragPieceVisual;
        private Vector3 originalPieceScale;
        private Square? currentHoverSquare = null;

        public event System.Action<Square> OnSquareClicked;
        public event System.Action<Square, Square> OnPieceDragged; // from, to

        private void Awake()
        {
            CreateBoard();
        }

        private void CreateBoard()
        {
            // Create parent containers
            var squaresParent = new GameObject("Squares").transform;
            squaresParent.SetParent(transform);

            var piecesParent = new GameObject("Pieces").transform;
            piecesParent.SetParent(transform);

            var highlightsParent = new GameObject("Highlights").transform;
            highlightsParent.SetParent(transform);

            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    Vector3 position = GetWorldPosition(file, rank);

                    // Create square
                    var squareObj = CreateSquare(file, rank, position, squaresParent);
                    squareObjects[file, rank] = squareObj;
                    squareRenderers[file, rank] = squareObj.GetComponent<SpriteRenderer>();

                    // Create highlight overlay
                    var highlightObj = CreateHighlight(position, highlightsParent);
                    highlightObjects[file, rank] = highlightObj;
                    highlightRenderers[file, rank] = highlightObj.GetComponent<SpriteRenderer>();

                    // Create piece placeholder
                    var pieceObj = CreatePiece(position, piecesParent);
                    pieceObjects[file, rank] = pieceObj;
                    pieceRenderers[file, rank] = pieceObj.GetComponent<SpriteRenderer>();
                }
            }

            // Center the board
            float offset = 3.5f * squareSize;
            transform.position = new Vector3(-offset, -offset, 0);
        }

        private GameObject CreateSquare(int file, int rank, Vector3 position, Transform parent)
        {
            var obj = new GameObject($"Square_{(char)('a' + file)}{rank + 1}");
            obj.transform.SetParent(parent);
            obj.transform.localPosition = position;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = (file + rank) % 2 == 0 ? darkSquareColor : lightSquareColor;
            sr.sortingOrder = 0;

            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(squareSize, squareSize);

            return obj;
        }

        private GameObject CreateHighlight(Vector3 position, Transform parent)
        {
            var obj = new GameObject("Highlight");
            obj.transform.SetParent(parent);
            obj.transform.localPosition = position;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = Color.clear;
            sr.sortingOrder = 1;

            return obj;
        }

        private GameObject CreatePiece(Vector3 position, Transform parent)
        {
            var obj = new GameObject("Piece");
            obj.transform.SetParent(parent);
            obj.transform.localPosition = position;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;

            return obj;
        }

        private Sprite CreateSquareSprite()
        {
            // Create a simple white square texture
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f / squareSize);
        }

        public Vector3 GetWorldPosition(int file, int rank)
        {
            return new Vector3(file * squareSize, rank * squareSize, 0);
        }

        public Square? GetSquareFromWorldPosition(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            int file = Mathf.FloorToInt(localPos.x / squareSize + 0.5f);
            int rank = Mathf.FloorToInt(localPos.y / squareSize + 0.5f);

            if (file >= 0 && file < 8 && rank >= 0 && rank < 8)
                return new Square(file, rank);
            return null;
        }

        public void UpdateBoard(Board board)
        {
            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    var piece = board[file, rank];
                    var sprite = GetPieceSprite(piece);
                    pieceRenderers[file, rank].sprite = sprite;

                    // Scale piece to fit within square
                    if (sprite != null)
                    {
                        float spriteWidth = sprite.bounds.size.x;
                        float spriteHeight = sprite.bounds.size.y;
                        float maxDimension = Mathf.Max(spriteWidth, spriteHeight);
                        float scale = (squareSize * pieceScale) / maxDimension;
                        pieceObjects[file, rank].transform.localScale = new Vector3(scale, scale, 1f);
                    }
                    else
                    {
                        pieceObjects[file, rank].transform.localScale = Vector3.one;
                    }
                }
            }
        }

        private Sprite GetPieceSprite(Piece piece)
        {
            if (piece.IsEmpty) return null;

            // Try to get from sprite loader first (actual sprites from Resources)
            if (PieceSpriteLoader.Instance != null)
            {
                var sprite = PieceSpriteLoader.Instance.GetSprite(piece);
                if (sprite != null) return sprite;
            }

            // Fallback to serialized fields (for backwards compatibility or generated sprites)
            return (piece.Type, piece.Color) switch
            {
                (PieceType.Pawn, PieceColor.White) => whitePawnSprite,
                (PieceType.Knight, PieceColor.White) => whiteKnightSprite,
                (PieceType.Bishop, PieceColor.White) => whiteBishopSprite,
                (PieceType.Rook, PieceColor.White) => whiteRookSprite,
                (PieceType.Queen, PieceColor.White) => whiteQueenSprite,
                (PieceType.King, PieceColor.White) => whiteKingSprite,
                (PieceType.Pawn, PieceColor.Black) => blackPawnSprite,
                (PieceType.Knight, PieceColor.Black) => blackKnightSprite,
                (PieceType.Bishop, PieceColor.Black) => blackBishopSprite,
                (PieceType.Rook, PieceColor.Black) => blackRookSprite,
                (PieceType.Queen, PieceColor.Black) => blackQueenSprite,
                (PieceType.King, PieceColor.Black) => blackKingSprite,
                _ => null
            };
        }

        public void SetSelection(Square? square, List<Move> legalMoves)
        {
            selectedSquare = square;
            legalMovesFromSelected = legalMoves ?? new List<Move>();
            UpdateHighlights();
        }

        public void SetLastMove(Move? move)
        {
            lastMove = move;
            UpdateHighlights();
        }

        public void ClearSelection()
        {
            selectedSquare = null;
            legalMovesFromSelected.Clear();
            UpdateHighlights();
        }

        private void UpdateHighlights()
        {
            // Clear all highlights
            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    highlightRenderers[file, rank].color = Color.clear;
                }
            }

            // Show last move
            if (lastMove.HasValue)
            {
                highlightRenderers[lastMove.Value.From.File, lastMove.Value.From.Rank].color = lastMoveColor;
                highlightRenderers[lastMove.Value.To.File, lastMove.Value.To.Rank].color = lastMoveColor;
            }

            // Show selected square
            if (selectedSquare.HasValue)
            {
                highlightRenderers[selectedSquare.Value.File, selectedSquare.Value.Rank].color = selectedColor;

                // Show legal moves
                foreach (var move in legalMovesFromSelected)
                {
                    highlightRenderers[move.To.File, move.To.Rank].color = legalMoveColor;
                }
            }
        }

        private void Update()
        {
            // Handle mouse/touch input using new Input System
            var pointer = Pointer.current;
            if (pointer == null) return;

            Vector2 screenPos = pointer.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
            worldPos.z = 0;

            if (pointer.press.wasPressedThisFrame)
            {
                HandlePointerDown(worldPos);
            }
            else if (pointer.press.isPressed && isDragging)
            {
                HandlePointerDrag(worldPos);
            }
            else if (pointer.press.wasReleasedThisFrame)
            {
                HandlePointerUp(worldPos);
            }
        }

        private void HandlePointerDown(Vector3 worldPos)
        {
            var square = GetSquareFromWorldPosition(worldPos);
            if (!square.HasValue) return;

            dragStartSquare = square;
            dragStartWorldPos = worldPos;
            isDragging = false; // Will become true if drag threshold exceeded
        }

        private void HandlePointerDrag(Vector3 worldPos)
        {
            if (!dragStartSquare.HasValue) return;

            float dragDistance = Vector3.Distance(worldPos, dragStartWorldPos);

            // Start visual drag if threshold exceeded and drag-to-move is enabled
            if (enableDragToMove && dragDistance > dragThreshold && !isDragging)
            {
                StartVisualDrag(dragStartSquare.Value);
            }

            if (isDragging)
            {
                UpdateDragVisual(worldPos);
                UpdateHoverHighlight(worldPos);
            }
        }

        private void HandlePointerUp(Vector3 worldPos)
        {
            var releaseSquare = GetSquareFromWorldPosition(worldPos);

            if (isDragging)
            {
                // Complete drag move
                EndVisualDrag();

                if (releaseSquare.HasValue && dragStartSquare.HasValue && releaseSquare.Value != dragStartSquare.Value)
                {
                    // Drag completed to different square
                    OnPieceDragged?.Invoke(dragStartSquare.Value, releaseSquare.Value);
                }
                else if (dragStartSquare.HasValue)
                {
                    // Dragged back to start or off board - treat as selection
                    OnSquareClicked?.Invoke(dragStartSquare.Value);
                }
            }
            else if (dragStartSquare.HasValue)
            {
                // No drag occurred - regular tap
                OnSquareClicked?.Invoke(dragStartSquare.Value);
            }

            // Reset drag state
            dragStartSquare = null;
            isDragging = false;
            currentHoverSquare = null;
            ClearHoverHighlight();
        }

        private void StartVisualDrag(Square square)
        {
            isDragging = true;

            // Get the piece at this square
            var pieceRenderer = pieceRenderers[square.File, square.Rank];
            if (pieceRenderer.sprite == null) return;

            // Create drag visual
            dragPieceVisual = new GameObject("DragPiece");
            dragPieceVisual.transform.SetParent(transform.parent);
            var sr = dragPieceVisual.AddComponent<SpriteRenderer>();
            sr.sprite = pieceRenderer.sprite;
            sr.sortingOrder = 10; // Above everything
            sr.color = new Color(1f, 1f, 1f, 0.9f);

            // Scale up the drag visual
            originalPieceScale = pieceObjects[square.File, square.Rank].transform.localScale;
            dragPieceVisual.transform.localScale = originalPieceScale * dragPieceScale;

            // Hide original piece
            pieceRenderer.enabled = false;

            // Show legal moves from this square
            SelectSquareForDrag(square);
        }

        private void UpdateDragVisual(Vector3 worldPos)
        {
            if (dragPieceVisual != null)
            {
                dragPieceVisual.transform.position = worldPos + dragOffset;
            }
        }

        private void EndVisualDrag()
        {
            if (dragPieceVisual != null)
            {
                Destroy(dragPieceVisual);
                dragPieceVisual = null;
            }

            // Show original piece again
            if (dragStartSquare.HasValue)
            {
                var pieceRenderer = pieceRenderers[dragStartSquare.Value.File, dragStartSquare.Value.Rank];
                pieceRenderer.enabled = true;
            }

            ClearHoverHighlight();
        }

        private void SelectSquareForDrag(Square square)
        {
            selectedSquare = square;
            // Legal moves will be set by GameManager through SetSelection
            // For now, just highlight the source
            UpdateHighlights();
        }

        private void UpdateHoverHighlight(Vector3 worldPos)
        {
            var hoverSquare = GetSquareFromWorldPosition(worldPos);

            if (hoverSquare != currentHoverSquare)
            {
                ClearHoverHighlight();
                currentHoverSquare = hoverSquare;

                if (currentHoverSquare.HasValue && currentHoverSquare != dragStartSquare)
                {
                    // Check if this is a legal move destination
                    bool isLegalDestination = false;
                    foreach (var move in legalMovesFromSelected)
                    {
                        if (move.To == currentHoverSquare.Value)
                        {
                            isLegalDestination = true;
                            break;
                        }
                    }

                    if (isLegalDestination)
                    {
                        highlightRenderers[currentHoverSquare.Value.File, currentHoverSquare.Value.Rank].color = dragHighlightColor;
                    }
                }
            }
        }

        private void ClearHoverHighlight()
        {
            if (currentHoverSquare.HasValue)
            {
                // Restore to legal move color if it's a legal destination, otherwise clear
                bool isLegalDestination = false;
                foreach (var move in legalMovesFromSelected)
                {
                    if (move.To == currentHoverSquare.Value)
                    {
                        isLegalDestination = true;
                        break;
                    }
                }

                if (isLegalDestination)
                {
                    highlightRenderers[currentHoverSquare.Value.File, currentHoverSquare.Value.Rank].color = legalMoveColor;
                }
                else
                {
                    highlightRenderers[currentHoverSquare.Value.File, currentHoverSquare.Value.Rank].color = Color.clear;
                }
            }
        }

        /// <summary>
        /// Cancels any in-progress drag operation.
        /// </summary>
        public void CancelDrag()
        {
            if (isDragging)
            {
                EndVisualDrag();
            }
            dragStartSquare = null;
            isDragging = false;
            currentHoverSquare = null;
        }
    }
}
