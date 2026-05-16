using System.Text;
using Theevaluate.Core.Board;

namespace Theevaluate.Core.Move;

public ref struct MoveList
{
    private int count;
    private Span<ushort> data;
    public readonly int Count => count;

    public MoveList(Span<ushort> buff)
    {
        count = 0;
        data = buff;
    }

    public MoveList(Span<ushort> buff, ref Position pos)
    {
        count = 0;
        data = buff;

        MoveGen.GeneratePseudoLegalMoves(ref pos, ref this);
    }

    public void Add(ushort move) { data[count++] = move; }
    public readonly ushort Get(int i) { return data[i]; }
    public void Clear() { count = 0; }
    public void Swap(int i1, int i2) 
    {
        (data[i1], data[i2]) = (data[i2], data[i1]);
    }

    public readonly override string ToString()
    {
        StringBuilder sb = new();

        for (int i = 0; i < count; ++i)
        {
            sb.Append(Move.ToString(data[i]));
            sb.Append(",\n");
        }
        sb.Append("Size: " + count);

        return sb.ToString();
    }
}
