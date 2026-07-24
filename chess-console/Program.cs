using board;
using chess_console;

internal class Program
{
    static void Main(string[] args)
    {
        Board board = new Board(8, 8);
        Screen.printScreen(board);
    }
}

