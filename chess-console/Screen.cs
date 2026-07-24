using board;

namespace chess_console;

public class Screen
{
    public static void printScreen(Board board)
    {
        for (int i = 0; i < board.rows; i++)
        {
            for (int j = 0; j < board.colums; j++)
            {
                if (board.piece(i,j) == null)
                {
                    Console.Write("- ");
                }
                else
                {
                    Console.Write(board.piece(i,j) + " ");
                }
            }
            
            Console.WriteLine();
        }
    }
}