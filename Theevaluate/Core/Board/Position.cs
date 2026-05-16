namespace Theevaluate.Core.Board;

using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using Theevaluate.Core.Move;

struct Board 
{
    public Piece[] pieces       = new Piece[(int)Square.NB];
    public ulong[] pieceBitboards = new ulong[(int)Piece.NB];
    public ulong[] colorBitboards = new ulong[(int)Color.NB];
    public ulong   occupancy      = 0;

    public Board() {}
}

struct GameState 
{
    public Square epSquare     = Square.None;
    public byte castlingRights = 0;
    public byte fiftyMoveClock = 0;

    public GameState() {}
    public void CopyData(ref GameState other) 
    {
        epSquare = other.epSquare;
        castlingRights = other.castlingRights;
        fiftyMoveClock = other.fiftyMoveClock;
    }
}

public class Position 
{
    // Static constant variable
    private static readonly char[] PieceCharacters = ['P', 'N', 'B', 'R', 'Q', 'K', 'p', 'n', 'b', 'r', 'q', 'k', '.'];
    public static readonly string StartingPositionFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    // Position states
    private Board board = new();
    private GameState gameState = new();
    private Color sideToMove = Color.White;

    private byte[] castlingRightsStack = new byte[3000];
    private byte[] fiftyMoveStack      = new byte[3000];
    private Piece[] capturedStack      = new Piece[3000];
    private Square[] enPassantStack    = new Square[3000];
    private int stackCounter = 0;
    
    public Position() 
    {
        // Set up empty board
        for (Square square = Square.A1; square <= Square.H8; ++square) 
            board.pieces[(int)square] = Piece.None;
    }

    // Use when making move
    private void PushUndoInfo(Piece captured)
    {
        castlingRightsStack[stackCounter] = gameState.castlingRights;
        fiftyMoveStack[stackCounter] = gameState.fiftyMoveClock;
        enPassantStack[stackCounter] = gameState.epSquare;
        capturedStack[stackCounter] = captured;
        stackCounter++;
    }

    private Piece PopUndoInfo()
    {
        stackCounter--;
        gameState.castlingRights = castlingRightsStack[stackCounter];
        gameState.fiftyMoveClock = fiftyMoveStack[stackCounter];
        gameState.epSquare = enPassantStack[stackCounter];
        
        return capturedStack[stackCounter];
    }

    // Lookup functions
    public Piece   PieceOn(Square squ) { return board.pieces[(int)squ]; }
    public ulong   GetBitboard(Piece piece) { return board.pieceBitboards[(int)piece]; }
    public ulong   GetBitboard(PieceType pt, Color c) { return board.pieceBitboards[(int)TypeUtil.MakePiece(pt, c)]; }
    public ulong   GetBitboard(Color color) { return board.colorBitboards[(int)color]; }
    public ulong   GetBitboard() { return board.occupancy; }

    public Color  GetSideMoving() { return sideToMove; }
    public Square GetEPSquare() { return gameState.epSquare; }
    public byte   GetCastling() { return gameState.castlingRights; }
    public byte   GetRule50() { return gameState.fiftyMoveClock; }

    public bool HasCastlingRights(byte mask) { return (mask & gameState.castlingRights) == mask; }

    private void Clear() 
    {
        Array.Fill(board.pieces, Piece.None);
        Array.Fill(board.pieceBitboards, 0UL);
        Array.Fill(board.colorBitboards, 0UL);
        board.occupancy = 0;

        sideToMove = Color.White;
        gameState.epSquare = Square.None;
        gameState.castlingRights = 0;
        gameState.fiftyMoveClock = 0;
    }

    private void ClearSquare(Square square) 
    {
        if (PieceOn(square) == Piece.None) return;

        Piece pieceAlreadyThere = PieceOn(square);
        Color color = TypeUtil.ColorOf(pieceAlreadyThere);

        // We first clear the array
        board.pieces[(int)square] = Piece.None;

        // Then we clear Bitboards
        ulong mask = ~Bitboards.SquareMasks[(int)square];

        board.pieceBitboards[(int)pieceAlreadyThere] &= mask;
        board.colorBitboards[(int)color] &= mask;
        board.occupancy &= mask;
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
        board.pieces[(int)square] = piece;

        // Set in Bitboards
        ulong mask = Bitboards.SquareMasks[(int)square];

        board.pieceBitboards[(int)piece] |= mask;
        board.colorBitboards[(int)color] |= mask;
        board.occupancy |= mask;
    }

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
                    if (c == PieceCharacters[i]) 
                        piece = (Piece)i;

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
            if (c == 'K') gameState.castlingRights |= Castling.WhiteKingside;
            if (c == 'Q') gameState.castlingRights |= Castling.WhiteQueenside;
            if (c == 'k') gameState.castlingRights |= Castling.BlackKingside;
            if (c == 'q') gameState.castlingRights |= Castling.BlackQueenside;
        }

        if (parts.Length < 4) return;
        string epPart = parts[3];
        if (epPart != "-")
            gameState.epSquare = TypeUtil.MakeSquare(epPart);
        
        if (parts.Length < 5) return;
        string r50Part = parts[4];
        gameState.fiftyMoveClock = (byte)Int16.Parse(r50Part);
    }

    public bool IsAttacked(Square square, Color by) 
    {
        // Pawns
        if ((GetBitboard(PieceType.Pawn, by) & Bitboards.GetPawnAttacks(square, TypeUtil.Opposite(by))) != 0) return true;

        // Knights
        if ((GetBitboard(PieceType.Knight, by) & Bitboards.GetKnightAttacks(square)) != 0) return true;

        // Kings
        if ((GetBitboard(PieceType.King, by) & Bitboards.GetKingAttacks(square)) != 0) return true;

        // Bishops & Queens
        if (((GetBitboard(PieceType.Bishop, by) | GetBitboard(PieceType.Queen, by)) & Bitboards.GetBishopAttacks(square, board.occupancy)) != 0) return true;

        // Rooks & Queens
        if (((GetBitboard(PieceType.Rook, by) | GetBitboard(PieceType.Queen, by)) & Bitboards.GetRookAttacks(square, board.occupancy)) != 0) return true;

        return false;
    }

    public void MakeMove(ushort move)
    {
        Color us = sideToMove;
        bool isWhite = us == Color.White;

        sideToMove = TypeUtil.Opposite(sideToMove);

        Square from = Move.From(move);
        Square to   = Move.To(move);
        byte flag   = Move.Flag(move);

        Piece movingPiece = PieceOn(from);
        Piece capturedPiece = flag == Move.EnPassant ? 
                                TypeUtil.MakePiece(PieceType.Pawn, TypeUtil.Opposite(us)) : 
                                PieceOn(to);

        PushUndoInfo(capturedPiece);

        gameState.epSquare = Square.None;

        // No matter what type of move it is, we always have to move
        // the moving piece
        ClearSquare(from);

        switch (flag)
        {
            case Move.Castling:
            {
                bool kingside = TypeUtil.FileOf(to) == File.G;
                Square rookFrom = kingside ? (isWhite ? Square.H1 : Square.H8) : (isWhite ? Square.A1 : Square.A8);
                Square rookTo   = kingside ? (Square)((int)rookFrom - 2) : (Square)((int)rookFrom + 3);

                // Place King
                PlacePiece(to, movingPiece);

                ClearSquare(rookFrom);
                PlacePiece(rookTo, TypeUtil.MakePiece(PieceType.Rook, us));

                break;
            }

            case Move.EnPassant:
            {
                PlacePiece(to, movingPiece);
                ClearSquare((Square)(isWhite ? to - 8 : to + 8));

                break;
            }

            case Move.DoublePush:
            {
                PlacePiece(to, movingPiece);

                gameState.epSquare = (Square)(isWhite ? to - 8 : to + 8);

                break;
            }

            case Move.Normal: 
            {
                ClearSquare(to);
                PlacePiece(to, movingPiece);

                break;
            }

            // Promotion
            default:
            {
                ClearSquare(to);
                PlacePiece(to, TypeUtil.MakePiece((PieceType)(flag - Move.NPromo + 1), us));

                break;
            }
        }

        if (TypeUtil.TypeOf(movingPiece) == PieceType.King)
        {
            gameState.castlingRights &= (byte)(isWhite ? ~Castling.White : ~Castling.Black);
        }

        if (to == Square.A1 || from == Square.A1)
        {
            gameState.castlingRights &= (byte)Castling.WhiteQueenside;
        }
        else if (to == Square.H1 || from == Square.H1)
        {
            gameState.castlingRights &= (byte)Castling.WhiteKingside;
        }
        else if (to == Square.A8 || from == Square.A8)
        {
            gameState.castlingRights &= (byte)Castling.BlackQueenside;
        }
        else if (to == Square.H8 || from == Square.H8)
        {
            gameState.castlingRights &= (byte)Castling.BlackKingside;
        }
    }

    public void UndoMove(ushort move)
    {
        sideToMove = TypeUtil.Opposite(sideToMove);

        Color us = sideToMove;
        bool isWhite = us == Color.White;

        Square from = Move.From(move);
        Square to   = Move.To(move);
        byte flag   = Move.Flag(move);

        Piece capturedPiece = PopUndoInfo();
        Piece movingPiece = PieceOn(to);

        ClearSquare(to);

        switch (flag)
        {
            case Move.Castling:
            {
                bool kingside = TypeUtil.FileOf(to) == File.G;
                Square rookFrom = kingside ? (isWhite ? Square.H1 : Square.H8) : (isWhite ? Square.A1 : Square.A8);
                Square rookTo   = kingside ? (Square)((int)rookFrom - 2) : (Square)((int)rookFrom + 3);

                PlacePiece(from, movingPiece);
                ClearSquare(rookTo);
                PlacePiece(rookFrom, TypeUtil.MakePiece(PieceType.Rook, us));

                break;
            }

            case Move.EnPassant:
            {
                PlacePiece(from, movingPiece);
                PlacePiece((Square)(isWhite ? to - 8 : to + 8), capturedPiece);

                break;
            }

            case Move.DoublePush:
            case Move.Normal:
            {
                PlacePiece(from, movingPiece);
                if (capturedPiece != Piece.None)
                    PlacePiece(to, capturedPiece);

                break;
            }

            // Promotion
            default:
            {
                PlacePiece(from, TypeUtil.MakePiece(PieceType.Pawn, us));
                if (capturedPiece != Piece.None)
                    PlacePiece(to, capturedPiece);

                break;
            }
        }
    }

    public override string ToString() 
    {
        StringBuilder sb = new();

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
