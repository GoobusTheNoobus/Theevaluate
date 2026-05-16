namespace Theevaluate.Core.Move;

public static class Move
{
    private static readonly char[] PromoCharacters = ['n', 'b', 'r', 'q'];

    private const int ToShift = 6;
    private const int FlagShift = 12;
    private const ushort SquareMask = 0x3F;
    private const ushort MoveMaskWithoutFlag = 0x0FFF;
    private const byte FlagMask = 0x7;

    public const byte Normal = 0,
                      Castling = 1,
                      EnPassant = 2,
                      DoublePush = 3,
                      NPromo = 4,
                      BPromo = 5,
                      RPromo = 6,
                      QPromo = 7;

    public static ushort Make(Square from, Square to, byte flag = Normal)
    {
        return (ushort)(
            ((ushort)from & SquareMask) |
            ((((ushort)to) & SquareMask) << ToShift) |
            ((((ushort)flag) & FlagMask) << FlagShift)
        );
    }

    public static ushort WithFlag(ushort move, byte flag)
    {
        return (ushort)((move & MoveMaskWithoutFlag) | ((((ushort)flag) & FlagMask) << FlagShift));
    }

    public static Square From(ushort move) => (Square)(move & SquareMask);
    public static Square To(ushort move) => (Square)((move >> ToShift) & SquareMask);
    public static byte Flag(ushort move) => (byte)((move >> FlagShift) & FlagMask);

    public static string ToString(ushort move)
    {
        byte flag = Flag(move);
        string fromStr = From(move).ToString().ToLower();
        string toStr = To(move).ToString().ToLower();

        if (flag >= NPromo) return fromStr + toStr + PromoCharacters[flag - NPromo];

        return fromStr + toStr;
    }
}
