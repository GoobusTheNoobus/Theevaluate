namespace Theevaluate.Core.Move;

using Theevaluate.Core.Board;

static class MoveGen
{
    public static void GeneratePseudoLegalMoves(ref Position pos, ref MoveList moves)
    {
        GeneratePawnMoves(ref pos, ref moves);
        GenerateKnightMoves(ref pos, ref moves);
        GenerateBishopMoves(ref pos, ref moves);
        GenerateRookMoves(ref pos, ref moves);
        GenerateQueenMoves(ref pos, ref moves);
        GenerateKingMoves(ref pos, ref moves);
    }

    private static void GeneratePawnMoves(ref Position pos, ref MoveList moves)
    {
        Color us = pos.GetSideMoving();
        Color them = TypeUtil.Opposite(us);
        bool isWhite = us == Color.White;

        ulong pawns = pos.GetBitboard(PieceType.Pawn, us);
        ulong occ   = pos.GetBitboard();
        ulong enemy = pos.GetBitboard(them);

        ulong rank3FromBottom = isWhite ? Bitboards.RankMasks[(int)Rank.R3]: Bitboards.RankMasks[(int)Rank.R6];
        ulong rank8FromBottom = isWhite ? Bitboards.RankMasks[(int)Rank.R8]: Bitboards.RankMasks[(int)Rank.R1];

        int singlePushDir   = isWhite ? 8: -8;
        int doublePushDir   = singlePushDir * 2;
        int leftCaptureDir  = isWhite ? 7: -9;
        int rightCaptureDir = isWhite ? 9: -7;

        // Bitboards
        ulong singlePushBB = (isWhite ? pawns << 8: pawns >> 8) & ~occ;
        ulong doublePushBB = (isWhite ? (singlePushBB & rank3FromBottom) << 8: (singlePushBB & rank3FromBottom) >> 8) & ~occ;
        ulong leftBB       = (isWhite ? (pawns & ~Bitboards.FileMasks[(int)File.A]) << 7: (pawns & ~Bitboards.FileMasks[(int)File.A]) >> 9) & enemy;
        ulong rightBB      = (isWhite ? (pawns & ~Bitboards.FileMasks[(int)File.H]) << 9: (pawns & ~Bitboards.FileMasks[(int)File.H]) >> 7) & enemy;

        // We seperate promotions from nonpromotions

        ulong singlePushPromoBB    = singlePushBB & rank8FromBottom;
        ulong singlePushNonPromoBB = singlePushBB & ~rank8FromBottom;
        ulong leftPromoBB          = leftBB & rank8FromBottom;
        ulong leftNonPromoBB       = leftBB & ~rank8FromBottom;
        ulong rightPromoBB         = rightBB & rank8FromBottom;
        ulong rightNonPromoBB      = rightBB & ~rank8FromBottom;

        ExtractPawn(ref moves, ref singlePushNonPromoBB, singlePushDir, Move.Normal);
        ExtractPawn(ref moves, ref doublePushBB, doublePushDir, Move.DoublePush);
        ExtractPawn(ref moves, ref leftNonPromoBB, leftCaptureDir, Move.Normal);
        ExtractPawn(ref moves, ref rightNonPromoBB, rightCaptureDir, Move.Normal);
        ExtractPawnPromo(ref moves, ref singlePushPromoBB, singlePushDir);
        ExtractPawnPromo(ref moves, ref leftPromoBB, leftCaptureDir);
        ExtractPawnPromo(ref moves, ref rightPromoBB, rightCaptureDir);

        // En Passant
        Square enPassantSquare = pos.GetEPSquare();
        if (enPassantSquare != Square.None)
        {
            ulong enPassantPawns = pawns & Bitboards.GetPawnAttacks(enPassantSquare, them);
            ExtractEP(ref moves, ref enPassantPawns, enPassantSquare);
        }
    }

    private static void GenerateKnightMoves(ref Position pos, ref MoveList moves)
    {
        Color us = pos.GetSideMoving();

        ulong own = pos.GetBitboard(us);
        ulong pieces = pos.GetBitboard(PieceType.Knight, us);

        if (pieces == 0) return; // Early exit

        while (pieces != 0)
        {
            Square from = (Square)Bitboards.PopLsb(ref pieces);
            ulong availableSquares = Bitboards.GetKnightAttacks(from) & ~own;

            ExtractPiece(ref moves, ref availableSquares, from);
        }
    }

    private static void GenerateBishopMoves(ref Position pos, ref MoveList moves)
    {
        Color us = pos.GetSideMoving();

        ulong occ = pos.GetBitboard();
        ulong own = pos.GetBitboard(us);
        ulong pieces = pos.GetBitboard(PieceType.Bishop, us);

        if (pieces == 0) return; // Early exit

        while (pieces != 0)
        {
            Square from = (Square)Bitboards.PopLsb(ref pieces);
            ulong availableSquares = Bitboards.GetBishopAttacks(from, occ) & ~own;

            ExtractPiece(ref moves, ref availableSquares, from);
        }
    }

    private static void GenerateRookMoves(ref Position pos, ref MoveList moves)
    {
        Color us = pos.GetSideMoving();

        ulong occ = pos.GetBitboard();
        ulong own = pos.GetBitboard(us);
        ulong pieces = pos.GetBitboard(PieceType.Rook, us);

        if (pieces == 0) return; // Early exit

        while (pieces != 0)
        {
            Square from = (Square)Bitboards.PopLsb(ref pieces);
            ulong availableSquares = Bitboards.GetRookAttacks(from, occ) & ~own;

            ExtractPiece(ref moves, ref availableSquares, from);
        }
    }

    private static void GenerateQueenMoves(ref Position pos, ref MoveList moves)
    {
        Color us = pos.GetSideMoving();

        ulong occ = pos.GetBitboard();
        ulong own = pos.GetBitboard(us);
        ulong pieces = pos.GetBitboard(PieceType.Queen, us);

        if (pieces == 0) return; // Early exit

        while (pieces != 0)
        {
            Square from = (Square)Bitboards.PopLsb(ref pieces);
            ulong availableSquares = Bitboards.GetQueenAttacks(from, occ) & ~own;

            ExtractPiece(ref moves, ref availableSquares, from);
        }
    }

    private static void GenerateKingMoves(ref Position pos, ref MoveList moves)
    {
        Color us = pos.GetSideMoving();

        ulong occ = pos.GetBitboard();
        ulong own = pos.GetBitboard(us);
        ulong pieces = pos.GetBitboard(PieceType.King, us);

        if (pieces == 0) return; // Early exit

        while (pieces != 0)
        {
            Square from = (Square)Bitboards.PopLsb(ref pieces);
            ulong availableSquares = Bitboards.GetKingAttacks(from) & ~own;

            ExtractPiece(ref moves, ref availableSquares, from);
        }

        HandleCastling(ref pos, ref moves);

    }

    private static void HandleCastling(ref Position pos, ref MoveList moves)
    {
        Color us = pos.GetSideMoving();
        bool isWhite = us == Color.White;

        Square from = isWhite ? Square.E1: Square.E8;
        Square ksTo = isWhite ? Square.G1: Square.G8;
        Square qsTo = isWhite ? Square.C1: Square.C8;

        ulong occ = pos.GetBitboard();

        if (isWhite)
        {
            if (pos.HasCastlingRights(Castling.WhiteKingside) && (occ & Castling.WhiteKingsideBetweenSquares) == 0)
                moves.Add(Move.New(from, ksTo, Move.Castling));
            if (pos.HasCastlingRights(Castling.WhiteQueenside) && (occ & Castling.WhiteQueensideBetweenSquares) == 0)
                moves.Add(Move.New(from, qsTo, Move.Castling));
        } else
        {
            if (pos.HasCastlingRights(Castling.BlackKingside) && (occ & Castling.BlackKingsideBetweenSquares) == 0)
                moves.Add(Move.New(from, ksTo, Move.Castling));
            if (pos.HasCastlingRights(Castling.BlackQueenside) && (occ & Castling.BlackQueenside) == 0)
                moves.Add(Move.New(from, qsTo, Move.Castling));
        }
    }

    // Helpers
    private static void ExtractPawn(ref MoveList moves, ref ulong bb, int offset, byte flag)
    {
        while (bb != 0)
        {
            int lsb = Bitboards.PopLsb(ref bb);
            moves.Add(Move.New((Square)(lsb - offset), (Square)lsb, flag));
        }
    }

    private static void ExtractPawnPromo(ref MoveList moves, ref ulong bb, int offset)
    {
        while (bb != 0)
        {
            int lsb = Bitboards.PopLsb(ref bb);

            ushort baseMove = Move.New((Square)(lsb - offset), (Square)lsb, Move.Normal);
            moves.Add(Move.WithFlag(baseMove, Move.NPromo));
            moves.Add(Move.WithFlag(baseMove, Move.BPromo));
            moves.Add(Move.WithFlag(baseMove, Move.RPromo));
            moves.Add(Move.WithFlag(baseMove, Move.QPromo));
        }
    }

    private static void ExtractEP(ref MoveList moves, ref ulong bb, Square to)
    {
        if (bb == 0) return;
        while (bb != 0)
        {
            int lsb = Bitboards.PopLsb(ref bb);
            moves.Add(Move.New((Square)lsb, to, Move.EnPassant));
        }
    }

    private static void ExtractPiece(ref MoveList moves, ref ulong bb, Square from)
    {
        while (bb != 0)
        {
            int lsb = Bitboards.PopLsb(ref bb);
            moves.Add(Move.New(from, (Square)lsb, Move.Normal));
        }
    }
}
