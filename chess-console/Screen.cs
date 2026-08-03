using System;
using board;
using chess;

namespace chess_console;

public class Screen
{
    public static void PrintScreen(Board board, bool [,] possibleMoves = null)
    {
        ConsoleColor originalBackground = Console.BackgroundColor;
        ConsoleColor originalForeground = Console.ForegroundColor;

        Console.WriteLine("    a  b  c  d  e  f  g  h");
        Console.WriteLine("   ────────────────────────");

        for (int i = 0; i < board.rows; i++)
        {
            Console.BackgroundColor = originalBackground;
            Console.ForegroundColor = originalForeground;
            Console.Write((8 - i) + " │");

            for (int j = 0; j < board.columns; j++)
            {
                if (possibleMoves != null && possibleMoves[i, j])
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                else if ((i + j) % 2 == 0)
                    Console.BackgroundColor = ConsoleColor.Blue; 
                else
                    Console.BackgroundColor = ConsoleColor.DarkBlue;

                Piece piece = board.piece(i, j);

                if (piece == null)
                    Console.Write("   ");
                else
                {
                    if (piece.GetColor() == Color.White)
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                    else
                        Console.ForegroundColor = ConsoleColor.Black;

                    Console.Write(" " + piece + " ");
                    Console.ForegroundColor = originalForeground;
                }
            }

            Console.BackgroundColor = originalBackground;
            Console.WriteLine("│");
        }

        Console.BackgroundColor = originalBackground;
        Console.WriteLine("   ────────────────────────");
        Console.WriteLine("    a  b  c  d  e  f  g  h");
        Console.WriteLine("");
    }
    
    public static ChessPosition ReadChessPosition()
    {
        string s = Console.ReadLine();
        char column = s[0];
        int row = int.Parse(s[1].ToString());
        return new ChessPosition(column, row);
    }
}