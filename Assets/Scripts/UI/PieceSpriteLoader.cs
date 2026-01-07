using UnityEngine;
using Shakki.Core;
using System.Collections.Generic;

namespace Shakki.UI
{
    /// <summary>
    /// Loads chess piece sprites from Resources folder.
    /// Naming convention: Chess_[piece][color]t45.svg
    /// piece: b=bishop, k=king, n=knight, p=pawn, q=queen, r=rook
    /// color: d=dark(black), l=light(white)
    /// </summary>
    public class PieceSpriteLoader : MonoBehaviour
    {
        private const string SpritePath = "Sprites/";

        private Dictionary<(PieceType, PieceColor), Sprite> spriteCache = new Dictionary<(PieceType, PieceColor), Sprite>();

        private static PieceSpriteLoader instance;
        public static PieceSpriteLoader Instance => instance;

        private void Awake()
        {
            instance = this;
            LoadAllSprites();
        }

        private void LoadAllSprites()
        {
            foreach (PieceType type in System.Enum.GetValues(typeof(PieceType)))
            {
                if (type == PieceType.None) continue;

                LoadSprite(type, PieceColor.White);
                LoadSprite(type, PieceColor.Black);
            }

            Debug.Log($"Loaded {spriteCache.Count} piece sprites");
        }

        private void LoadSprite(PieceType type, PieceColor color)
        {
            string pieceLetter = type switch
            {
                PieceType.Pawn => "p",
                PieceType.Knight => "n",
                PieceType.Bishop => "b",
                PieceType.Rook => "r",
                PieceType.Queen => "q",
                PieceType.King => "k",
                _ => ""
            };

            string colorLetter = color == PieceColor.White ? "l" : "d";

            // Resource name matches filename without extension: Chess_plt45
            string spriteName = $"Chess_{pieceLetter}{colorLetter}t45";

            var sprite = Resources.Load<Sprite>(SpritePath + spriteName);
            if (sprite != null)
            {
                spriteCache[(type, color)] = sprite;
            }
            else
            {
                Debug.LogWarning($"Failed to load sprite: {SpritePath}{spriteName}");
            }
        }

        public Sprite GetSprite(PieceType type, PieceColor color)
        {
            if (type == PieceType.None || color == PieceColor.None)
                return null;

            if (spriteCache.TryGetValue((type, color), out var sprite))
                return sprite;

            return null;
        }

        public Sprite GetSprite(Piece piece)
        {
            return GetSprite(piece.Type, piece.Color);
        }
    }
}
