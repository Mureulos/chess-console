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
        board.PutPiece(new King(board, Color.Black), new ChessPosition('d', 1).ToPosition());
        board.PutPiece(new Tower(board, Color.Black), new ChessPosition('a', 1).ToPosition());
        board.PutPiece(new Tower(board, Color.Black), new ChessPosition('h', 1).ToPosition());
        
        board.PutPiece(new King(board, Color.White), new ChessPosition('d', 8).ToPosition());
        board.PutPiece(new Tower(board, Color.White), new ChessPosition('a', 8).ToPosition());
        board.PutPiece(new Tower(board, Color.White), new ChessPosition('h', 8).ToPosition());
    }
}