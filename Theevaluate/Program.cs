using System;
using Theevaluate.Core;

namespace Theevaluate
{
class Program
{
    static void Main(string[] args)
    {
        Position pos = new Position();

        pos.ParseFEN(Position.StartingPositionFEN);

        Console.WriteLine(pos);
    }
}  

} // namespace Theevaluate
