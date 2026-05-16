namespace Theevaluate.Core.Board;

static class Zobrist 
{
    public static ulong[,] PieceSquareKeys  = new ulong[(int)Piece.NB, (int)Square.NB];
    public static ulong[] CastlingRightKeys = new ulong[16];
    public static ulong[] EnPassantKeys     = new ulong[(int)File.NB];
    public static ulong SideToMoveKey       = 0;

    static Zobrist() 
    {
        #warning Zobrist Hashing hasn't been updated to the board representation yet

        Random rng = new();

        for (Piece p = Piece.WPawn; p <= Piece.BKing; ++p) 
            for (Square s = Square.A1; s <= Square.H8; ++s) 
                PieceSquareKeys[(int)p, (int)s] = (ulong)rng.NextInt64();

        for (int i = 0; i < 16; ++i) CastlingRightKeys[i] = (ulong)rng.NextInt64();
        for (File f = File.A; f <= File.H; ++f) EnPassantKeys[(int)f] = (ulong)rng.NextInt64();
        SideToMoveKey = (ulong)rng.NextInt64();
    }
}