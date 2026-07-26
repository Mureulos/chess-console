using board;

namespace chess;

public class ChessMatch
{
    public Board board { get; private set; }
    private int _turn;
    private Color _actualPlayerColor;
    public bool completed { get; private set; }
    
    public ChessMatch()
    {
        board = new Board();
        _turn = 1;
        _actualPlayerColor = Color.White;
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
    
    public void SetBoard()
    {
        board.PutPiece(new King(board, Color.Black), new ChessPosition('c', 1).ToPosition());
    }
}