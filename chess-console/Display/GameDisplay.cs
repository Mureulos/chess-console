using board;
using chess;

namespace chess_console.Display;

public class GameDisplay
{
    private const ConsoleColor LightSquareColor = ConsoleColor.Blue;
    private const ConsoleColor DarkSquareColor = ConsoleColor.DarkBlue;
    private const ConsoleColor PossibleMoveColor = ConsoleColor.DarkCyan;
    private const ConsoleColor WhitePieceColor = ConsoleColor.DarkRed;
    private const ConsoleColor BlackPieceColor = ConsoleColor.Black;
    private const ConsoleColor SelectedPieceColor = ConsoleColor.Magenta;
    
    public void DisplayBoard(Board board, bool[,] possibleMoves = null, Position selectedPosition = null)
    {
        ConsoleColor originalBackground = Console.BackgroundColor;
        ConsoleColor originalForeground = Console.ForegroundColor;

        PrintBoardHeader();

        for (int i = 0; i < board.rows; i++)
        {
            PrintBoardRow(board, i, possibleMoves, selectedPosition, originalBackground, originalForeground);
        }

        PrintBoardFooter();

        Console.BackgroundColor = originalBackground;
        Console.ForegroundColor = originalForeground;
    }

    private void PrintBoardHeader()
    {
        Console.WriteLine("    a  b  c  d  e  f  g  h");
        Console.WriteLine("   ────────────────────────");
    }

    private void PrintBoardRow(
        Board board, 
        int row, 
        bool[,] possibleMoves,
        Position selectedPosition,
        ConsoleColor bgOrig, 
        ConsoleColor fgOrig)
    {
        Console.BackgroundColor = bgOrig;
        Console.ForegroundColor = fgOrig;
        Console.Write((8 - row) + " │");

        for (int col = 0; col < board.columns; col++)
        {
            bool isSelectedPiece = selectedPosition != null
                                   && selectedPosition.row == row
                                   && selectedPosition.column == col;

            if (isSelectedPiece)
                Console.BackgroundColor = SelectedPieceColor;
            else if (possibleMoves != null && possibleMoves[row, col])
                Console.BackgroundColor = PossibleMoveColor;
            else
                Console.BackgroundColor = (row + col) % 2 == 0
                    ? LightSquareColor
                    : DarkSquareColor;

            Piece piece = board.piece(row, col);

            if (piece == null)
                Console.Write("   ");
            else
            {
                Console.ForegroundColor = piece.GetColor() == Color.White
                    ? WhitePieceColor
                    : BlackPieceColor;

                Console.Write(" " + piece + " ");
                Console.ForegroundColor = fgOrig;
            }
        }

        Console.BackgroundColor = bgOrig;
        Console.WriteLine("│");
    }

    private void PrintBoardFooter()
    {
        Console.WriteLine("   ────────────────────────");
        Console.WriteLine("    a  b  c  d  e  f  g  h");
        Console.WriteLine("");
    }

    public void DisplayGameStatus(ChessMatch match)
    {
        Console.WriteLine("┌─────────────────────────┐");
        Console.WriteLine($"│ Turn: {match.turn,2}           │");
        Console.WriteLine($"│ Player: {match.actualPlayerColor,-11}│");
        
        if (match.check)
        {
            if (match.completed)
                Console.WriteLine("│ ⚠️  CHECKMATE!           │");
            else
                Console.WriteLine("│ ⚠️  CHECK!               │");
        }
        
        Console.WriteLine("└─────────────────────────┘");
        Console.WriteLine();
    }

    public void DisplayCapturedPieces(ChessMatch match)
    {
        var whiteCaptured = match.GetCapturedPieces(Color.White);
        var blackCaptured = match.GetCapturedPieces(Color.Black);

        if (whiteCaptured.Count == 0 && blackCaptured.Count == 0)
            return;

        Console.WriteLine("┌─ CAPTURED PIECES ─────────┐");

        if (whiteCaptured.Count > 0)
        {
            Console.Write("│ White: ");
            foreach (var piece in whiteCaptured)
                Console.Write(piece);
            Console.WriteLine();
        }

        if (blackCaptured.Count > 0)
        {
            Console.Write("│ Black: ");
            foreach (var piece in blackCaptured)
                Console.Write(piece);
            Console.WriteLine();
        }

        Console.WriteLine("└────────────────────────────┘");
        Console.WriteLine();
    }

    public void DisplayGameHistory(List<string> moves)
    {
        if (moves.Count == 0)
            return;

        Console.WriteLine("┌─ MOVE HISTORY ────────────┐");
        
        for (int i = 0; i < moves.Count; i++)
        {
            Console.WriteLine($"│ {i + 1:00}. {moves[i],-18}│");
        }

        Console.WriteLine("└────────────────────────────┘");
        Console.WriteLine();
    }

    public void DisplayFullGame(ChessMatch match, bool[,] possibleMoves = null, Position selectedPosition = null)
    {
        Console.Clear();
        DisplayGameStatus(match);
        DisplayBoard(match.board, possibleMoves, selectedPosition);
        DisplayCapturedPieces(match);
    }

    public void DisplayFullGameWithHistory(ChessMatch match, List<string> moves, bool[,] possibleMoves = null, Position selectedPosition = null)
    {
        Console.Clear();
        DisplayGameStatus(match);
        DisplayBoard(match.board, possibleMoves, selectedPosition);
        DisplayCapturedPieces(match);
        DisplayGameHistory(moves);
    }
}
