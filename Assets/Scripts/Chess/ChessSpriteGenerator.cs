using System.Collections.Generic;
using UnityEngine;

namespace Chess
{
    public static class ChessSpriteGenerator
    {
        private const int Size = 64;

        public static Dictionary<(PieceType, PieceColor), Sprite> GenerateAllSprites()
        {
            var sprites = new Dictionary<(PieceType, PieceColor), Sprite>();

            foreach (PieceType type in new[] { PieceType.King, PieceType.Queen, PieceType.Rook,
                                                PieceType.Bishop, PieceType.Knight, PieceType.Pawn })
            {
                sprites[(type, PieceColor.White)] = GeneratePieceSprite(type, true);
                sprites[(type, PieceColor.Black)] = GeneratePieceSprite(type, false);
            }

            return sprites;
        }

        private static Sprite GeneratePieceSprite(PieceType type, bool isWhite)
        {
            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color fill = isWhite ? new Color(1f, 1f, 1f, 1f) : new Color(0.15f, 0.15f, 0.15f, 1f);
            Color outline = isWhite ? new Color(0.2f, 0.2f, 0.2f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
            Color clear = new Color(0, 0, 0, 0);

            // Clear texture
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    tex.SetPixel(x, y, clear);

            switch (type)
            {
                case PieceType.King:
                    DrawKing(tex, fill, outline);
                    break;
                case PieceType.Queen:
                    DrawQueen(tex, fill, outline);
                    break;
                case PieceType.Rook:
                    DrawRook(tex, fill, outline);
                    break;
                case PieceType.Bishop:
                    DrawBishop(tex, fill, outline);
                    break;
                case PieceType.Knight:
                    DrawKnight(tex, fill, outline);
                    break;
                case PieceType.Pawn:
                    DrawPawn(tex, fill, outline);
                    break;
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        private static void DrawKing(Texture2D tex, Color fill, Color outline)
        {
            // Base
            FillRect(tex, 16, 4, 32, 8, fill);
            FillRect(tex, 20, 12, 24, 20, fill);
            // Body
            FillRect(tex, 24, 20, 16, 28, fill);
            // Cross
            FillRect(tex, 28, 48, 8, 12, fill);
            FillRect(tex, 22, 52, 20, 6, fill);
            // Outline
            DrawOutline(tex, outline);
        }

        private static void DrawQueen(Texture2D tex, Color fill, Color outline)
        {
            // Base
            FillRect(tex, 16, 4, 32, 8, fill);
            FillRect(tex, 20, 12, 24, 16, fill);
            // Body tapered
            FillRect(tex, 22, 28, 20, 16, fill);
            FillRect(tex, 26, 44, 12, 8, fill);
            // Crown points
            FillRect(tex, 18, 52, 6, 8, fill);
            FillRect(tex, 29, 54, 6, 10, fill);
            FillRect(tex, 40, 52, 6, 8, fill);
            // Outline
            DrawOutline(tex, outline);
        }

        private static void DrawRook(Texture2D tex, Color fill, Color outline)
        {
            // Base
            FillRect(tex, 14, 4, 36, 8, fill);
            FillRect(tex, 18, 12, 28, 8, fill);
            // Tower body
            FillRect(tex, 20, 20, 24, 28, fill);
            // Battlements
            FillRect(tex, 18, 48, 8, 12, fill);
            FillRect(tex, 28, 48, 8, 12, fill);
            FillRect(tex, 38, 48, 8, 12, fill);
            // Outline
            DrawOutline(tex, outline);
        }

        private static void DrawBishop(Texture2D tex, Color fill, Color outline)
        {
            // Base
            FillRect(tex, 18, 4, 28, 8, fill);
            FillRect(tex, 22, 12, 20, 12, fill);
            // Body tapered
            FillRect(tex, 24, 24, 16, 16, fill);
            FillRect(tex, 26, 40, 12, 12, fill);
            // Pointed top
            FillRect(tex, 28, 52, 8, 8, fill);
            FillRect(tex, 30, 58, 4, 4, fill);
            // Slash mark (diagonal line represented as small gap)
            tex.SetPixel(34, 36, new Color(0, 0, 0, 0));
            tex.SetPixel(35, 37, new Color(0, 0, 0, 0));
            tex.SetPixel(36, 38, new Color(0, 0, 0, 0));
            // Outline
            DrawOutline(tex, outline);
        }

        private static void DrawKnight(Texture2D tex, Color fill, Color outline)
        {
            // Base
            FillRect(tex, 14, 4, 36, 8, fill);
            FillRect(tex, 18, 12, 28, 8, fill);
            // Body
            FillRect(tex, 20, 20, 20, 20, fill);
            // Neck
            FillRect(tex, 24, 40, 12, 12, fill);
            // Head
            FillRect(tex, 20, 48, 20, 10, fill);
            FillRect(tex, 16, 44, 12, 8, fill);
            // Ear
            FillRect(tex, 34, 54, 6, 8, fill);
            // Outline
            DrawOutline(tex, outline);
        }

        private static void DrawPawn(Texture2D tex, Color fill, Color outline)
        {
            // Base
            FillRect(tex, 18, 4, 28, 8, fill);
            FillRect(tex, 22, 12, 20, 8, fill);
            // Body
            FillRect(tex, 24, 20, 16, 16, fill);
            // Head (circle approximation)
            FillRect(tex, 26, 36, 12, 12, fill);
            FillRect(tex, 24, 40, 16, 8, fill);
            FillRect(tex, 28, 48, 8, 4, fill);
            // Outline
            DrawOutline(tex, outline);
        }

        private static void FillRect(Texture2D tex, int x, int y, int width, int height, Color color)
        {
            for (int px = x; px < x + width && px < Size; px++)
            {
                for (int py = y; py < y + height && py < Size; py++)
                {
                    if (px >= 0 && py >= 0)
                        tex.SetPixel(px, py, color);
                }
            }
        }

        private static void DrawOutline(Texture2D tex, Color outline)
        {
            Color clear = new Color(0, 0, 0, 0);

            for (int x = 1; x < Size - 1; x++)
            {
                for (int y = 1; y < Size - 1; y++)
                {
                    Color current = tex.GetPixel(x, y);
                    if (current.a > 0)
                    {
                        // Check neighbors for edge detection
                        if (tex.GetPixel(x - 1, y).a == 0 ||
                            tex.GetPixel(x + 1, y).a == 0 ||
                            tex.GetPixel(x, y - 1).a == 0 ||
                            tex.GetPixel(x, y + 1).a == 0)
                        {
                            tex.SetPixel(x, y, outline);
                        }
                    }
                }
            }
        }
    }
}
