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
                display.DisplayFullGameWithHistory(chessMatch, chessMatch.MoveHistory);
                
                Console.Write("Origin: ");
                Position origin = Screen.ReadChessPosition().ToPosition();

                chessMatch.ValideOriginPosition(origin);
                bool[,] possibleMoves = chessMatch.board.piece(origin).PossibleMoves();
                
                display.DisplayFullGameWithHistory(chessMatch, chessMatch.MoveHistory, possibleMoves, origin);
                
                Console.Write("Target: ");
                Position target = Screen.ReadChessPosition().ToPosition();
                
                chessMatch.ValidadeTargetPosition(origin, target); 

                PromotionPiece? promotion = null;
                Piece movingPiece = chessMatch.board.piece(origin);
                if (movingPiece is Pawn && (target.row == 0 || target.row == 7))
                {
                    Console.Write("Promotion (Q/R/B/N): ");
                    promotion = ReadPromotionPiece();
                }

                chessMatch.MakeMove(origin, target, promotion);
            }
            catch (BoardException e)
            {
                Console.WriteLine("❌ " + e.Message);
                Console.ReadLine();
            }
        }
        
        Console.Clear();
        display.DisplayFullGame(chessMatch);
        Console.WriteLine("╔════════════════════════╗");
        Console.WriteLine($"║ 🏁 GAME OVER!          ║");
        Console.WriteLine(chessMatch.draw
            ? "║ Result: Draw           ║"
            : $"║ Winner: {chessMatch.Opponent(chessMatch.actualPlayerColor),-8} ║");
        Console.WriteLine("╚════════════════════════╝");
    }

    private static PromotionPiece ReadPromotionPiece()
    {
        return Console.ReadLine()?.Trim().ToUpperInvariant() switch
        {
            "Q" => PromotionPiece.Queen,
            "R" => PromotionPiece.Rook,
            "B" => PromotionPiece.Bishop,
            "N" => PromotionPiece.Knight,
            _ => throw new BoardException("Invalid promotion piece")
        };
    }
}
