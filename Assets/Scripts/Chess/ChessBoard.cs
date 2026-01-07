using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public class ChessBoard : MonoBehaviour
    {
        private ChessPiece[,] board = new ChessPiece[ChessConstants.BoardSize, ChessConstants.BoardSize];
        private GameObject[,] squares = new GameObject[ChessConstants.BoardSize, ChessConstants.BoardSize];
        private GameObject selectionHighlight;
        private GameObject checkHighlight;
        private List<GameObject> validMoveHighlights = new List<GameObject>();
        private Sprite squareSprite;
        private Dictionary<(PieceType, PieceColor), Sprite> pieceSprites;

        private void Start()
        {
            CreateSquareSprite();
            LoadPieceSprites();
            CreateBoard();
            CreateSelectionHighlight();
            CreateCheckHighlight();
            SetupStartingPosition();
        }

        private void CreateSquareSprite()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }

        private void LoadPieceSprites()
        {
            // Generate sprites procedurally
            pieceSprites = ChessSpriteGenerator.GenerateAllSprites();
        }

        private void CreateBoard()
        {
            GameObject boardParent = new GameObject("Board");
            boardParent.transform.SetParent(transform);

            for (int x = 0; x < ChessConstants.BoardSize; x++)
            {
                for (int y = 0; y < ChessConstants.BoardSize; y++)
                {
                    GameObject square = new GameObject($"Square_{x}_{y}");
                    square.transform.SetParent(boardParent.transform);
                    square.transform.position = BoardToWorld(x, y);

                    SpriteRenderer sr = square.AddComponent<SpriteRenderer>();
                    sr.sprite = squareSprite;
                    sr.color = (x + y) % 2 == 0 ? ChessConstants.DarkSquare : ChessConstants.LightSquare;
                    sr.sortingOrder = 0;

                    squares[x, y] = square;
                }
            }
        }

        private void CreateSelectionHighlight()
        {
            selectionHighlight = new GameObject("SelectionHighlight");
            selectionHighlight.transform.SetParent(transform);

            SpriteRenderer sr = selectionHighlight.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.color = ChessConstants.SelectionHighlight;
            sr.sortingOrder = 1;

            selectionHighlight.SetActive(false);
        }

        private void CreateCheckHighlight()
        {
            checkHighlight = new GameObject("CheckHighlight");
            checkHighlight.transform.SetParent(transform);

            SpriteRenderer sr = checkHighlight.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.color = ChessConstants.CheckHighlight;
            sr.sortingOrder = 1;

            checkHighlight.SetActive(false);
        }

        private void SetupStartingPosition()
        {
            for (int x = 0; x < ChessConstants.BoardSize; x++)
            {
                CreatePiece(x, 0, ChessConstants.BackRank[x], PieceColor.White);
                CreatePiece(x, 1, PieceType.Pawn, PieceColor.White);
                CreatePiece(x, 6, PieceType.Pawn, PieceColor.Black);
                CreatePiece(x, 7, ChessConstants.BackRank[x], PieceColor.Black);
            }
        }

        private void CreatePiece(int x, int y, PieceType type, PieceColor color)
        {
            GameObject pieceObj = new GameObject($"Piece_{type}_{color}");
            pieceObj.transform.SetParent(transform);
            pieceObj.transform.position = BoardToWorld(x, y);

            SpriteRenderer sr = pieceObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 2;
            sr.sprite = pieceSprites[(type, color)];
            pieceObj.transform.localScale = new Vector3(0.9f, 0.9f, 1);

            ChessPiece piece = new ChessPiece
            {
                Type = type,
                Color = color,
                Position = new Vector2Int(x, y),
                GameObject = pieceObj,
                SpriteRenderer = sr
            };

            board[x, y] = piece;
        }

        public ChessPiece GetPieceAt(Vector2Int pos)
        {
            if (!IsValidPosition(pos)) return null;
            return board[pos.x, pos.y];
        }

        public void MovePiece(Vector2Int from, Vector2Int to)
        {
            ChessPiece piece = board[from.x, from.y];
            if (piece == null) return;

            ChessPiece captured = board[to.x, to.y];
            if (captured != null)
            {
                Destroy(captured.GameObject);
            }

            board[to.x, to.y] = piece;
            board[from.x, from.y] = null;

            piece.Position = to;
            piece.GameObject.transform.position = BoardToWorld(to.x, to.y);
        }

        public void ShowSelectionAt(Vector2Int pos)
        {
            selectionHighlight.transform.position = BoardToWorld(pos.x, pos.y);
            selectionHighlight.SetActive(true);
        }

        public void HideSelection()
        {
            selectionHighlight.SetActive(false);
            HideValidMoves();
        }

        public void ShowValidMoves(List<Vector2Int> moves)
        {
            HideValidMoves();
            foreach (var move in moves)
            {
                GameObject highlight = new GameObject($"ValidMove_{move.x}_{move.y}");
                highlight.transform.SetParent(transform);
                highlight.transform.position = BoardToWorld(move.x, move.y);

                SpriteRenderer sr = highlight.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;
                sr.color = ChessConstants.ValidMoveHighlight;
                sr.sortingOrder = 1;

                validMoveHighlights.Add(highlight);
            }
        }

        public void HideValidMoves()
        {
            foreach (var highlight in validMoveHighlights)
            {
                Destroy(highlight);
            }
            validMoveHighlights.Clear();
        }

        public ChessPiece[,] GetBoard()
        {
            return board;
        }

        public void ShowCheck(Vector2Int kingPos)
        {
            checkHighlight.transform.position = BoardToWorld(kingPos.x, kingPos.y);
            checkHighlight.SetActive(true);
        }

        public void HideCheck()
        {
            checkHighlight.SetActive(false);
        }

        public Vector2Int? FindKing(PieceColor color)
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

        public Vector2Int WorldToBoard(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - ChessConstants.BoardOffset + 0.5f) / ChessConstants.SquareSize);
            int y = Mathf.FloorToInt((worldPos.y - ChessConstants.BoardOffset + 0.5f) / ChessConstants.SquareSize);
            return new Vector2Int(x, y);
        }

        public Vector3 BoardToWorld(int x, int y)
        {
            return new Vector3(
                x * ChessConstants.SquareSize + ChessConstants.BoardOffset,
                y * ChessConstants.SquareSize + ChessConstants.BoardOffset,
                0
            );
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < ChessConstants.BoardSize &&
                   pos.y >= 0 && pos.y < ChessConstants.BoardSize;
        }
    }
}
