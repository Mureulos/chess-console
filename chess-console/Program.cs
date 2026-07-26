using board;
using chess_console;
using chess_console.Exceptions;
using chess; 

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            Board board = new Board(8, 8);
            board.PutPiece(new King(board, Color.White), new Position(1, 3));
            board.PutPiece(new King(board, Color.White), new Position(1, 3));
            Screen.PrintScreen(board);
        }
        catch (BoardException e)
        {
            Console.WriteLine(e.Message);
        }
    }
}

