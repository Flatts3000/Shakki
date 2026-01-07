namespace Shakki.Core
{
    public enum PieceType
    {
        None = 0,
        Pawn = 1,
        Knight = 2,
        Bishop = 3,
        Rook = 4,
        Queen = 5,
        King = 6
    }

    public enum PieceColor
    {
        None = 0,
        White = 1,
        Black = 2
    }

    public struct Piece
    {
        public PieceType Type;
        public PieceColor Color;

        public static readonly Piece None = new Piece { Type = PieceType.None, Color = PieceColor.None };

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
        }

        public bool IsEmpty => Type == PieceType.None;
        public bool IsWhite => Color == PieceColor.White;
        public bool IsBlack => Color == PieceColor.Black;

        public int BaseValue => Type switch
        {
            PieceType.Pawn => 1,
            PieceType.Knight => 3,
            PieceType.Bishop => 3,
            PieceType.Rook => 5,
            PieceType.Queen => 9,
            PieceType.King => 0,
            _ => 0
        };

        public override string ToString()
        {
            if (IsEmpty) return ".";
            char c = Type switch
            {
                PieceType.Pawn => 'P',
                PieceType.Knight => 'N',
                PieceType.Bishop => 'B',
                PieceType.Rook => 'R',
                PieceType.Queen => 'Q',
                PieceType.King => 'K',
                _ => '?'
            };
            return IsWhite ? c.ToString() : c.ToString().ToLower();
        }
    }

    public struct Square
    {
        public int File; // 0-7 (a-h)
        public int Rank; // 0-7 (1-8)

        public Square(int file, int rank)
        {
            File = file;
            Rank = rank;
        }

        public bool IsValid => File >= 0 && File < 8 && Rank >= 0 && Rank < 8;

        public string ToAlgebraic()
        {
            return $"{(char)('a' + File)}{Rank + 1}";
        }

        public static Square FromAlgebraic(string notation)
        {
            if (notation.Length != 2) return new Square(-1, -1);
            int file = notation[0] - 'a';
            int rank = notation[1] - '1';
            return new Square(file, rank);
        }

        public override string ToString() => ToAlgebraic();

        public override bool Equals(object obj)
        {
            if (obj is Square other)
                return File == other.File && Rank == other.Rank;
            return false;
        }

        public override int GetHashCode() => File * 8 + Rank;

        public static bool operator ==(Square a, Square b) => a.File == b.File && a.Rank == b.Rank;
        public static bool operator !=(Square a, Square b) => !(a == b);
    }

    public struct Move
    {
        public Square From;
        public Square To;
        public PieceType Promotion;
        public MoveFlags Flags;

        public Move(Square from, Square to, PieceType promotion = PieceType.None, MoveFlags flags = MoveFlags.None)
        {
            From = from;
            To = to;
            Promotion = promotion;
            Flags = flags;
        }

        public bool IsCapture => (Flags & MoveFlags.Capture) != 0;
        public bool IsCastling => (Flags & (MoveFlags.KingsideCastle | MoveFlags.QueensideCastle)) != 0;
        public bool IsEnPassant => (Flags & MoveFlags.EnPassant) != 0;
        public bool IsPromotion => Promotion != PieceType.None;

        public override string ToString()
        {
            string promo = Promotion != PieceType.None ? $"={Promotion}" : "";
            return $"{From}{To}{promo}";
        }
    }

    [System.Flags]
    public enum MoveFlags
    {
        None = 0,
        Capture = 1,
        EnPassant = 2,
        KingsideCastle = 4,
        QueensideCastle = 8,
        DoublePawnPush = 16
    }

    public enum GameResult
    {
        InProgress,
        WhiteWins,
        BlackWins,
        Draw
    }
}
