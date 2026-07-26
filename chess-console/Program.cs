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
            ChessMatch chessMatch = new ChessMatch();
            
            while(chessMatch.completed == false)
            {
                Console.Clear();
                Screen.PrintScreen(chessMatch.board);
                
                Console.Write("Origin: ");
                Position origin = Screen.ReadChessPosition().ToPosition();
                
                Console.Write("Target: ");
                Position target = Screen.ReadChessPosition().ToPosition();
                
                chessMatch.ExecuteMoviment(origin, target);
            }
        }
        catch (BoardException e)
        {
            Console.WriteLine(e.Message);
        }
    }
}

