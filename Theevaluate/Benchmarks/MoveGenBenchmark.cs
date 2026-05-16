using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Theevaluate.Core.Board;
using Theevaluate.Core.Move;

namespace Theevaluate.Benchmarks;

public static class MoveGenBenchmark
{
    private static readonly string[] BenchmarkFens = [
        Position.StartingPositionFEN,
        "rnbqk1nr/ppp1bppp/3p4/8/3NP3/8/PPP2PPP/RNBQKB1R w KQkq - 0 1",
        "rnbqk2r/ppp2ppp/4pn2/3p4/2PP4/P1b5/1P1BPPPP/R2QKBNR w KQkq - 0 1",

        "2rqk2r/p3bpp1/2Q2n1p/3p4/3P4/2N1PN2/P2B1PPP/R4RK1 b k - 0 1",
        "r1q2r1k/1p2bppp/2npbn2/p1N1p3/4P3/P1N1BP2/1PP1Q1PP/2KR1B1R w - - 0 1",
        "2k1r2r/ppqn2p1/2p1Bp2/6p1/3P2P1/2P1Q2P/PP3P2/2KR3R w - - 1 18"

    ];

    public static void Run(int warmupIterationsPerPosition = 200_000, int measureIterationsPerPosition = 2_000_000)
    {
        Position[] positions = CreateBenchmarkPositions();

        Span<ushort> moves = stackalloc ushort[300];
        MoveList list = new(moves);
        ulong checksum = 0;

        // Before measuring, we warm up CPU
        RunMovegenLoops(positions, warmupIterationsPerPosition, ref list, ref checksum);

        // Then we clean up extra noises
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long start = Stopwatch.GetTimestamp();
        RunMovegenLoops(positions, measureIterationsPerPosition, ref list, ref checksum);
        long end = Stopwatch.GetTimestamp();

        long calls = (long)positions.Length * measureIterationsPerPosition;
        double elapsedSeconds = (end - start) / (double)Stopwatch.Frequency;
        double nsPerCall = (elapsedSeconds * 1_000_000_000d) / calls;
        double mnCallsPerSecond = calls / elapsedSeconds / 1_000_000d;

        Console.WriteLine($"Positions: {positions.Length}");
        Console.WriteLine($"Calls measured: {calls:N0}");
        Console.WriteLine($"Elapsed: {elapsedSeconds:F6}s");
        Console.WriteLine($"Movegen: {nsPerCall:F2} ns/call ({mnCallsPerSecond:F2} M calls/s)");
        Console.WriteLine($"Checksum: 0x{checksum:X16}");
    }

    private static Position[] CreateBenchmarkPositions()
    {
        Position[] positions = new Position[BenchmarkFens.Length];

        for (int i = 0; i < BenchmarkFens.Length; ++i)
        {
            Position pos = new();
            pos.ParseFEN(BenchmarkFens[i]);
            positions[i] = pos;
        }

        return positions;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunMovegenLoops(Position[] positions, int iterationsPerPosition, ref MoveList list, ref ulong checksum)
    {
        for (int i = 0; i < iterationsPerPosition; ++i)
        {
            for (int p = 0; p < positions.Length; ++p)
            {
                Position pos = positions[p];
                list.Clear();
                MoveGen.GeneratePseudoLegalMoves(ref pos, ref list);

                checksum += (ulong)list.Count;
                if (list.Count != 0) checksum ^= list.Get(0);
            }
        }
    }
}
