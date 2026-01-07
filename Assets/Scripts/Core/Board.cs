using System.Text;

namespace Shakki.Core
{
    public class Board
    {
        private Piece[,] squares = new Piece[8, 8];

        public PieceColor SideToMove { get; private set; } = PieceColor.White;
        public bool WhiteCanCastleKingside { get; private set; } = true;
        public bool WhiteCanCastleQueenside { get; private set; } = true;
        public bool BlackCanCastleKingside { get; private set; } = true;
        public bool BlackCanCastleQueenside { get; private set; } = true;
        public Square? EnPassantSquare { get; private set; } = null;
        public int HalfmoveClock { get; private set; } = 0;
        public int FullmoveNumber { get; private set; } = 1;

        public Piece this[int file, int rank]
        {
            get => IsValidSquare(file, rank) ? squares[file, rank] : Piece.None;
            set { if (IsValidSquare(file, rank)) squares[file, rank] = value; }
        }

        public Piece this[Square sq]
        {
            get => this[sq.File, sq.Rank];
            set => this[sq.File, sq.Rank] = value;
        }

        public static bool IsValidSquare(int file, int rank)
        {
            return file >= 0 && file < 8 && rank >= 0 && rank < 8;
        }

        public Board()
        {
            Clear();
        }

        public void Clear()
        {
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                    squares[f, r] = Piece.None;

            SideToMove = PieceColor.White;
            WhiteCanCastleKingside = false;
            WhiteCanCastleQueenside = false;
            BlackCanCastleKingside = false;
            BlackCanCastleQueenside = false;
            EnPassantSquare = null;
            HalfmoveClock = 0;
            FullmoveNumber = 1;
        }

        public void SetupStandardPosition()
        {
            Clear();

            // White pieces
            squares[0, 0] = new Piece(PieceType.Rook, PieceColor.White);
            squares[1, 0] = new Piece(PieceType.Knight, PieceColor.White);
            squares[2, 0] = new Piece(PieceType.Bishop, PieceColor.White);
            squares[3, 0] = new Piece(PieceType.Queen, PieceColor.White);
            squares[4, 0] = new Piece(PieceType.King, PieceColor.White);
            squares[5, 0] = new Piece(PieceType.Bishop, PieceColor.White);
            squares[6, 0] = new Piece(PieceType.Knight, PieceColor.White);
            squares[7, 0] = new Piece(PieceType.Rook, PieceColor.White);

            for (int f = 0; f < 8; f++)
                squares[f, 1] = new Piece(PieceType.Pawn, PieceColor.White);

            // Black pieces
            squares[0, 7] = new Piece(PieceType.Rook, PieceColor.Black);
            squares[1, 7] = new Piece(PieceType.Knight, PieceColor.Black);
            squares[2, 7] = new Piece(PieceType.Bishop, PieceColor.Black);
            squares[3, 7] = new Piece(PieceType.Queen, PieceColor.Black);
            squares[4, 7] = new Piece(PieceType.King, PieceColor.Black);
            squares[5, 7] = new Piece(PieceType.Bishop, PieceColor.Black);
            squares[6, 7] = new Piece(PieceType.Knight, PieceColor.Black);
            squares[7, 7] = new Piece(PieceType.Rook, PieceColor.Black);

            for (int f = 0; f < 8; f++)
                squares[f, 6] = new Piece(PieceType.Pawn, PieceColor.Black);

            SideToMove = PieceColor.White;
            WhiteCanCastleKingside = true;
            WhiteCanCastleQueenside = true;
            BlackCanCastleKingside = true;
            BlackCanCastleQueenside = true;
            EnPassantSquare = null;
            HalfmoveClock = 0;
            FullmoveNumber = 1;
        }

        public Square FindKing(PieceColor color)
        {
            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    var piece = squares[f, r];
                    if (piece.Type == PieceType.King && piece.Color == color)
                        return new Square(f, r);
                }
            }
            return new Square(-1, -1);
        }

        public void MakeMove(Move move)
        {
            var piece = this[move.From];
            var captured = this[move.To];

            // Handle en passant capture
            if (move.IsEnPassant)
            {
                int capturedPawnRank = piece.IsWhite ? move.To.Rank - 1 : move.To.Rank + 1;
                this[move.To.File, capturedPawnRank] = Piece.None;
            }

            // Move the piece
            this[move.To] = move.IsPromotion
                ? new Piece(move.Promotion, piece.Color)
                : piece;
            this[move.From] = Piece.None;

            // Handle castling - move the rook
            if (move.IsCastling)
            {
                if ((move.Flags & MoveFlags.KingsideCastle) != 0)
                {
                    int rank = piece.IsWhite ? 0 : 7;
                    this[5, rank] = this[7, rank]; // Rook from h to f
                    this[7, rank] = Piece.None;
                }
                else if ((move.Flags & MoveFlags.QueensideCastle) != 0)
                {
                    int rank = piece.IsWhite ? 0 : 7;
                    this[3, rank] = this[0, rank]; // Rook from a to d
                    this[0, rank] = Piece.None;
                }
            }

            // Update castling rights
            UpdateCastlingRights(move, piece);

            // Update en passant square
            if ((move.Flags & MoveFlags.DoublePawnPush) != 0)
            {
                int epRank = piece.IsWhite ? move.From.Rank + 1 : move.From.Rank - 1;
                EnPassantSquare = new Square(move.From.File, epRank);
            }
            else
            {
                EnPassantSquare = null;
            }

            // Update clocks
            if (piece.Type == PieceType.Pawn || !captured.IsEmpty)
                HalfmoveClock = 0;
            else
                HalfmoveClock++;

            if (SideToMove == PieceColor.Black)
                FullmoveNumber++;

            // Switch side to move
            SideToMove = SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        private void UpdateCastlingRights(Move move, Piece piece)
        {
            // King moves remove all castling rights for that side
            if (piece.Type == PieceType.King)
            {
                if (piece.IsWhite)
                {
                    WhiteCanCastleKingside = false;
                    WhiteCanCastleQueenside = false;
                }
                else
                {
                    BlackCanCastleKingside = false;
                    BlackCanCastleQueenside = false;
                }
            }

            // Rook moves or captures on rook squares
            if (move.From == new Square(0, 0) || move.To == new Square(0, 0))
                WhiteCanCastleQueenside = false;
            if (move.From == new Square(7, 0) || move.To == new Square(7, 0))
                WhiteCanCastleKingside = false;
            if (move.From == new Square(0, 7) || move.To == new Square(0, 7))
                BlackCanCastleQueenside = false;
            if (move.From == new Square(7, 7) || move.To == new Square(7, 7))
                BlackCanCastleKingside = false;
        }

        public Board Clone()
        {
            var clone = new Board();
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                    clone.squares[f, r] = squares[f, r];

            clone.SideToMove = SideToMove;
            clone.WhiteCanCastleKingside = WhiteCanCastleKingside;
            clone.WhiteCanCastleQueenside = WhiteCanCastleQueenside;
            clone.BlackCanCastleKingside = BlackCanCastleKingside;
            clone.BlackCanCastleQueenside = BlackCanCastleQueenside;
            clone.EnPassantSquare = EnPassantSquare;
            clone.HalfmoveClock = HalfmoveClock;
            clone.FullmoveNumber = FullmoveNumber;

            return clone;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int r = 7; r >= 0; r--)
            {
                sb.Append($"{r + 1} ");
                for (int f = 0; f < 8; f++)
                {
                    sb.Append(squares[f, r].ToString());
                    sb.Append(' ');
                }
                sb.AppendLine();
            }
            sb.AppendLine("  a b c d e f g h");
            return sb.ToString();
        }
    }
}
