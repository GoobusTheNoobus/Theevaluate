namespace Theevaluate.Core;

using System.Text;
using U64 = ulong;

public class Position
{
    private static readonly char[] PieceCharacters = ['P', 'N', 'B', 'R', 'Q', 'K', 'p', 'n', 'b', 'r', 'q', 'k', '.'];
    public static readonly string StartingPositionFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    // Board representations
    private Piece[] pieces       = new Piece[(int)Square.NB];
    private U64[] pieceBitboards = new U64[(int)Piece.NB];
    private U64[] colorBitboards = new U64[(int)Color.NB];
    private U64   occupancy      = 0;

    private Color sideToMove    = Color.White;
    private Square epSquare     = Square.None;
    private byte castlingRights = 0;
    private byte fiftyMoveClock = 0;


    public Position()
    {
        // Set up empty board
        for (Square square = Square.A1; square <= Square.H8; ++square )
        {
            pieces[(int)square] = Piece.None;
        }
    }

    // Lookup functions
    public Piece PieceOn(Square squ) { return pieces[(int)squ]; }
    public U64   GetBitboard(Piece piece) { return pieceBitboards[(int)piece]; }
    public U64   GetBitboard(Color color) { return colorBitboards[(int)color]; }
    public U64   GetBitboard() { return occupancy; }

    public Color GetSideMoving() { return sideToMove; }
    public Square GetEPSquare() { return epSquare; }
    public byte GetCastling() { return castlingRights; }
    public byte GetRule50() { return fiftyMoveClock; }

    public void ParseFEN(string fen)
    {
        Clear();

        string[] parts = fen.Split(' ');
        if (parts.Length < 1) return;

        // Board
        string boardPart = parts[0];

        Rank rank = Rank.R8;
        File file = File.A;
        foreach (char c in boardPart.ToCharArray())
        {
            if (c == '/')
            {
                --rank;
                file = 0;
            } 
            
            else if (Char.IsAsciiDigit(c))
                file = (File)((int)file + c - '0');
            else
            {
                Piece piece = Piece.None;

                for (int i = 0; i < PieceCharacters.Length; ++i)
                {
                    if (c == PieceCharacters[i]) 
                        piece = (Piece)i;
                }

                if (piece == Piece.None) return;
                PlacePiece(TypeUtil.MakeSquare(rank, file), piece);
                ++file;
            }
        }

        if (parts.Length < 2) return;
        string sidePart = parts[1];
        if (sidePart == "b") sideToMove = Color.Black;

        if (parts.Length < 3) return;
        string castlingPart = parts[2];
        foreach (char c in castlingPart.ToCharArray())
        {
            if (c == 'K') castlingRights |= Castling.WhiteKingside;
            if (c == 'Q') castlingRights |= Castling.WhiteQueenside;
            if (c == 'k') castlingRights |= Castling.BlackKingside;
            if (c == 'q') castlingRights |= Castling.BlackQueenside;
        }


        if (parts.Length < 4) return;
        string epPart = parts[3];
        if (epPart != "-")
            epSquare = TypeUtil.MakeSquare(epPart);
        
        if (parts.Length < 5) return;
        string r50Part = parts[4];
        fiftyMoveClock = (byte)Int16.Parse(r50Part);
        
    }

    private void Clear()
    {
        Array.Fill(pieces, Piece.None);
        Array.Fill(pieceBitboards, 0UL);
        Array.Fill(colorBitboards, 0UL);
        occupancy = 0;

        sideToMove = Color.White;
        epSquare = Square.None;
        castlingRights = 0;
        fiftyMoveClock = 0;
    }

    private void ClearSquare(Square square)
    {
        if (PieceOn(square) == Piece.None) return;

        Piece pieceAlreadyThere = PieceOn(square);
        Color color = TypeUtil.ColorOf(pieceAlreadyThere);

        // We first clear the array
        pieces[(int)square] = Piece.None;

        // Then we clear Bitboards
        U64 mask = ~(1UL << (int)square);

        pieceBitboards[(int)pieceAlreadyThere] &= mask;
        colorBitboards[(int)color] &= mask;
        occupancy &= mask;

    }

    // Assumes the square is already empty
    private void PlacePiece(Square square, Piece piece)
    {
        if (piece == Piece.None)
        {
            ClearSquare(square); 
            return;
        }

        Color color = TypeUtil.ColorOf(piece);

        // Set in array
        pieces[(int)square] = piece;

        // Set in Bitboards
        U64 mask = 1UL << (int)square;

        pieceBitboards[(int)piece] |= mask;
        colorBitboards[(int)color] |= mask;
        occupancy |= mask;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        for (Rank r = Rank.R8; r >= Rank.R1; --r)
        {
            sb.Append("  +---+---+---+---+---+---+---+---+\n");
            sb.Append((int)(r + 1));
            sb.Append(' ');

            for (File f = File.A; f <= File.H; ++f)
            {
                sb.Append("| ");

                Piece piece = PieceOn(TypeUtil.MakeSquare((Rank)r, (File)f));

                if (piece != Piece.None)
                    sb.Append(PieceCharacters[(int)piece]);
                else
                    sb.Append(' ');
                sb.Append(' ');
            }
            sb.Append("|\n");
                
        }

        sb.Append("  +---+---+---+---+---+---+---+---+\n");
        sb.Append("    A   B   C   D   E   F   G   H  \n\n");

        return sb.ToString();
    }

}
