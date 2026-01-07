using UnityEngine;
using Shakki.Core;

namespace Shakki.UI
{
    /// <summary>
    /// Generates simple placeholder piece sprites at runtime.
    /// Replace with actual sprites later.
    /// </summary>
    public class PieceSpriteGenerator : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color whiteColor = Color.white;
        [SerializeField] private Color blackColor = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color outlineColor = new Color(0.1f, 0.1f, 0.1f);

        [Header("Settings")]
        [SerializeField] private int textureSize = 64;

        private BoardView boardView;

        private void Start()
        {
            boardView = GetComponent<BoardView>();
            if (boardView != null)
            {
                GenerateAndAssignSprites();
            }
        }

        private void GenerateAndAssignSprites()
        {
            // Use reflection to set the sprite fields on BoardView
            var type = boardView.GetType();

            SetSprite(type, "whitePawnSprite", CreatePieceSprite(PieceType.Pawn, true));
            SetSprite(type, "whiteKnightSprite", CreatePieceSprite(PieceType.Knight, true));
            SetSprite(type, "whiteBishopSprite", CreatePieceSprite(PieceType.Bishop, true));
            SetSprite(type, "whiteRookSprite", CreatePieceSprite(PieceType.Rook, true));
            SetSprite(type, "whiteQueenSprite", CreatePieceSprite(PieceType.Queen, true));
            SetSprite(type, "whiteKingSprite", CreatePieceSprite(PieceType.King, true));

            SetSprite(type, "blackPawnSprite", CreatePieceSprite(PieceType.Pawn, false));
            SetSprite(type, "blackKnightSprite", CreatePieceSprite(PieceType.Knight, false));
            SetSprite(type, "blackBishopSprite", CreatePieceSprite(PieceType.Bishop, false));
            SetSprite(type, "blackRookSprite", CreatePieceSprite(PieceType.Rook, false));
            SetSprite(type, "blackQueenSprite", CreatePieceSprite(PieceType.Queen, false));
            SetSprite(type, "blackKingSprite", CreatePieceSprite(PieceType.King, false));
        }

        private void SetSprite(System.Type type, string fieldName, Sprite sprite)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(boardView, sprite);
            }
        }

        private Sprite CreatePieceSprite(PieceType pieceType, bool isWhite)
        {
            var texture = new Texture2D(textureSize, textureSize);
            texture.filterMode = FilterMode.Point;

            // Fill with transparent
            var transparent = new Color(0, 0, 0, 0);
            for (int x = 0; x < textureSize; x++)
                for (int y = 0; y < textureSize; y++)
                    texture.SetPixel(x, y, transparent);

            Color fillColor = isWhite ? whiteColor : blackColor;

            // Draw shape based on piece type
            switch (pieceType)
            {
                case PieceType.Pawn:
                    DrawPawn(texture, fillColor);
                    break;
                case PieceType.Knight:
                    DrawKnight(texture, fillColor);
                    break;
                case PieceType.Bishop:
                    DrawBishop(texture, fillColor);
                    break;
                case PieceType.Rook:
                    DrawRook(texture, fillColor);
                    break;
                case PieceType.Queen:
                    DrawQueen(texture, fillColor);
                    break;
                case PieceType.King:
                    DrawKing(texture, fillColor);
                    break;
            }

            texture.Apply();

            float pixelsPerUnit = textureSize;
            return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        private void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                            tex.SetPixel(px, py, color);
                    }
                }
            }
        }

        private void DrawRect(Texture2D tex, int x1, int y1, int x2, int y2, Color color)
        {
            for (int x = x1; x <= x2; x++)
            {
                for (int y = y1; y <= y2; y++)
                {
                    if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
                        tex.SetPixel(x, y, color);
                }
            }
        }

        private void DrawPawn(Texture2D tex, Color color)
        {
            int c = textureSize / 2;
            // Base
            DrawRect(tex, c - 12, 4, c + 12, 12, color);
            // Body
            DrawRect(tex, c - 8, 12, c + 8, 32, color);
            // Head
            DrawCircle(tex, c, 44, 10, color);
        }

        private void DrawKnight(Texture2D tex, Color color)
        {
            int c = textureSize / 2;
            // Base
            DrawRect(tex, c - 14, 4, c + 14, 12, color);
            // Body
            DrawRect(tex, c - 10, 12, c + 6, 40, color);
            // Head
            DrawRect(tex, c - 6, 40, c + 12, 52, color);
            // Ear
            DrawRect(tex, c - 6, 52, c, 58, color);
        }

        private void DrawBishop(Texture2D tex, Color color)
        {
            int c = textureSize / 2;
            // Base
            DrawRect(tex, c - 12, 4, c + 12, 12, color);
            // Body
            DrawRect(tex, c - 8, 12, c + 8, 44, color);
            // Top
            DrawCircle(tex, c, 50, 6, color);
            // Point
            DrawRect(tex, c - 2, 54, c + 2, 60, color);
        }

        private void DrawRook(Texture2D tex, Color color)
        {
            int c = textureSize / 2;
            // Base
            DrawRect(tex, c - 14, 4, c + 14, 12, color);
            // Body
            DrawRect(tex, c - 10, 12, c + 10, 44, color);
            // Battlements
            DrawRect(tex, c - 12, 44, c - 8, 56, color);
            DrawRect(tex, c - 2, 44, c + 2, 56, color);
            DrawRect(tex, c + 8, 44, c + 12, 56, color);
        }

        private void DrawQueen(Texture2D tex, Color color)
        {
            int c = textureSize / 2;
            // Base
            DrawRect(tex, c - 14, 4, c + 14, 12, color);
            // Body
            DrawRect(tex, c - 10, 12, c + 10, 40, color);
            // Crown points
            DrawCircle(tex, c - 10, 50, 4, color);
            DrawCircle(tex, c, 54, 4, color);
            DrawCircle(tex, c + 10, 50, 4, color);
        }

        private void DrawKing(Texture2D tex, Color color)
        {
            int c = textureSize / 2;
            // Base
            DrawRect(tex, c - 14, 4, c + 14, 12, color);
            // Body
            DrawRect(tex, c - 10, 12, c + 10, 44, color);
            // Cross horizontal
            DrawRect(tex, c - 8, 48, c + 8, 52, color);
            // Cross vertical
            DrawRect(tex, c - 2, 44, c + 2, 60, color);
        }
    }
}
