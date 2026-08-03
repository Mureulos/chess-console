using board;
using chess_console;
using chess_console.Exceptions;
using chess; 

internal class Program
{
    static void Main(string[] args)
    {
        ChessMatch chessMatch = new ChessMatch();
        
        while(chessMatch.completed == false)
        {
            try
            {
                Console.Clear();

                Screen.PrintScreen(chessMatch.board, null);
                
                Console.WriteLine("Turn: " + chessMatch.turn);
                Console.WriteLine("Waiting player: " + chessMatch.actualPlayerColor);
                Console.WriteLine();
                
                Console.Write("Origin: ");
                Position origin = Screen.ReadChessPosition().ToPosition();

                Console.Clear();
                Console.WriteLine("Turn: " + chessMatch.turn);
                Console.WriteLine("Waiting player: " + chessMatch.actualPlayerColor);
                Console.WriteLine();
                
                chessMatch.ValideOriginPosition(origin);
                bool[,] possibleMoves = chessMatch.board.piece(origin).PossibleMoves();
                Screen.PrintScreen(chessMatch.board, possibleMoves);
                
                Console.WriteLine("Turn: " + chessMatch.turn);
                Console.WriteLine("Waiting player: " + chessMatch.actualPlayerColor);
                Console.WriteLine();
                
                Console.Write("Target: ");
                Position target = Screen.ReadChessPosition().ToPosition();
                
                chessMatch.ValidadeTargetPosition(origin, target); 
                chessMatch.MakeMove(origin, target);
            }
            catch (BoardException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}

