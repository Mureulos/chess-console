using board;
using chess_console.Exceptions;

namespace chess;

public class ChessMatch
{
    public Board board { get; private set; }
    public int turn { get; private set; }
    public Color actualPlayerColor { get; private set; }
    public bool completed { get; private set; }
    
    public ChessMatch()
    {
        board = new Board();
        turn = 1;
        actualPlayerColor = Color.White;
        completed = false;
        SetBoard();
    }

    public void ExecuteMoviment(Position origin, Position target)
    {
        Piece piece = board.RemovePiece(origin);
        piece.AddMove();
        Piece caughtPiece = board.RemovePiece(target);
        board.PutPiece(piece, target);
    }

    public void MakeMove(Position origin, Position target)
    {
        ExecuteMoviment(origin, target);
        turn++;
        ChangePlayer();
    }

    public void ValideOriginPosition(Position position)
    {
        if (board.piece(position) == null)
            throw new BoardException("Piece not found in that position");
        if (actualPlayerColor != board.piece(position).color)
            throw new BoardException("This piece is not yours");
        if (board.piece(position).HasPossibleMoves() == false)
            throw new BoardException("There are no possible moves for this piece");
    }

    public void ValidadeTargetPosition(Position origin, Position target)
    {
        if (board.piece(origin).CanMoveTo(target) == false)
            throw new BoardException("Invalid target position");
    }

    private void ChangePlayer()
    {
        if (actualPlayerColor == Color.White)
            actualPlayerColor = Color.Black;
        else
            actualPlayerColor = Color.White;
    }
    
    public void SetBoard()
    {
        board.PutPiece(new King(board, Color.White), new ChessPosition('d', 1).ToPosition());
        board.PutPiece(new Tower(board, Color.White), new ChessPosition('a', 1).ToPosition());
        board.PutPiece(new Tower(board, Color.White), new ChessPosition('h', 1).ToPosition());
        
        board.PutPiece(new King(board, Color.Black), new ChessPosition('d', 8).ToPosition());
        board.PutPiece(new Tower(board, Color.Black), new ChessPosition('d', 7).ToPosition());
        board.PutPiece(new Tower(board, Color.Black), new ChessPosition('a', 8).ToPosition());
        board.PutPiece(new Tower(board, Color.Black), new ChessPosition('h', 8).ToPosition());
    }
}