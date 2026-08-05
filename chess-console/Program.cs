using board;
using chess_console;
using chess_console.Display;
using chess_console.Exceptions;
using chess; 

internal class Program
{
    static void Main(string[] args)
    {
        ChessMatch chessMatch = new ChessMatch();
        var display = new GameDisplay();
        
        while (!chessMatch.completed)
        {
            try
            {
                display.DisplayFullGame(chessMatch);
                
                Console.Write("Origin: ");
                Position origin = Screen.ReadChessPosition().ToPosition();

                chessMatch.ValideOriginPosition(origin);
                bool[,] possibleMoves = chessMatch.board.piece(origin).PossibleMoves();
                
                display.DisplayFullGame(chessMatch, possibleMoves);
                
                Console.Write("Target: ");
                Position target = Screen.ReadChessPosition().ToPosition();
                
                chessMatch.ValidadeTargetPosition(origin, target); 
                chessMatch.MakeMove(origin, target);
            }
            catch (BoardException e)
            {
                Console.WriteLine("❌ " + e.Message);
                Console.ReadLine();
            }
        }
    }
}

