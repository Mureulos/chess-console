using System;
using board;

namespace chess_console;

public class Screen
{
    public static void PrintScreen(Board board)
    {
        ConsoleColor originalBackground = Console.BackgroundColor;
        ConsoleColor originalForeground = Console.ForegroundColor;

        Console.WriteLine("    a  b  c  d  e  f  g  h");
        Console.WriteLine("  -------------------------");

        for (int i = 0; i < board.rows; i++)
        {
            Console.BackgroundColor = originalBackground;
            Console.ForegroundColor = originalForeground;
            Console.Write((8 - i) + " |");

            for (int j = 0; j < board.colums; j++)
            {
                if ((i + j) % 2 == 0)
                    Console.BackgroundColor = ConsoleColor.White;
                else
                    Console.BackgroundColor = ConsoleColor.Black;

                Piece piece = board.piece(i, j);

                if (piece == null)
                    Console.Write("   ");
                else
                {
                    if (piece.GetColor() == Color.White)
                        Console.ForegroundColor = ConsoleColor.Blue;
                    else
                        Console.ForegroundColor = ConsoleColor.Red;

                    Console.Write(" " + piece + " ");
                    Console.ForegroundColor = originalForeground;
                }
            }

            Console.BackgroundColor = originalBackground;
            Console.WriteLine("|");
        }

        Console.BackgroundColor = originalBackground;
        Console.WriteLine("  -------------------------");
        Console.WriteLine("    a  b  c  d  e  f  g  h");
        Console.WriteLine("");
    }
}