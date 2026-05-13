using System.Reflection.Metadata;

namespace Theevaluate.Core;

public enum Square: byte
{
    A1, B1, C1, D1, E1, F1, G1, H1,
    A2, B2, C2, D2, E2, F2, G2, H2,
    A3, B3, C3, D3, E3, F3, G3, H3,
    A4, B4, C4, D4, E4, F4, G4, H4,
    A5, B5, C5, D5, E5, F5, G5, H5,
    A6, B6, C6, D6, E6, F6, G6, H6,
    A7, B7, C7, D7, E7, F7, G7, H7,
    A8, B8, C8, D8, E8, F8, G8, H8,
    NB,

    None = Byte.MaxValue,
};

public enum File: sbyte { A, B, C, D, E, F, G, H, NB };
public enum Rank: sbyte { R1, R2, R3, R4, R5, R6, R7, R8, NB};

public enum Piece: byte
{
    WPawn, WKnight, WBishop, WRook, WQueen, WKing,
    BPawn, BKnight, BBishop, BRook, BQueen, BKing,
    NB,
    None = Byte.MaxValue
}

public enum Color: byte { White, Black, NB }
public enum PieceType: byte { Pawn, Knight, Bishop, Rook, Queen, King }

public class Castling
{
    public static byte WhiteKingside  = 1;
    public static byte WhiteQueenside = 2;
    public static byte BlackKingside  = 4;
    public static byte BlackQueenside = 8;
}

public class TypeUtil
{
    public static File   FileOf(Square square) { return (File)((int)square & 7); }
    public static Rank   RankOf(Square square) { return (Rank)((int)square >> 3); }
    public static Square MakeSquare(Rank rank, File file) { return (Square)((int)rank << 3 | (int)file); }

    public static Square MakeSquare(string str)
    {
        Rank rank = (Rank)(str[1] - '1');
        File file = (File)(str[0] - 'a');

        return MakeSquare(rank, file);
    }

    public static Color     ColorOf(Piece piece) { return (Color)((int)piece / 6); }
    public static PieceType TypeOf(Piece piece) { return (PieceType)((int)piece % 6); }
    public static Piece     MakePiece(PieceType pieceType, Color color) { return (Piece)((int)color * 6 + (int)pieceType); }

    
}
