using chess_console.Exceptions;

namespace board;

public class Board
{   
    public int rows { get; set; }
    public int columns { get; set; }
    private Piece[,] _pieces;   
    
    public Board(int rows = 8, int columns = 8)
    {
        this.rows = rows;
        this.columns = columns;
        _pieces = new Piece[rows, columns];
    }
    
    public Piece piece(int row, int columns)
    {
        return _pieces[row, columns];
    }
    
    public Piece piece(Position position)
    {
        if (position == null)
            return null;
        
        ValidatePosition(position);
        return _pieces[position.row, position.column];
    }

    public bool IsPositionValid(Position position)
    {
        if (position.row < 0 || position.row >= rows || position.column < 0 || position.column >= columns)
            return false;
        
        return true;
    }

    private void ValidatePosition(Position position)
    {
        if (!IsPositionValid(position))
            throw new BoardException("Invalid position!");
    }
    
    private bool PieceExists(Position position)
    {
        ValidatePosition(position);
        return _pieces[position.row, position.column] != null;
    }
    
    public void PutPiece(Piece piece, Position position)
    {
        if (PieceExists(position))
            throw new BoardException("Piece already exists!");
        
        _pieces[position.row, position.column] = piece;
        piece.position = position;
    }

    public Piece RemovePiece(Position position)
    {
        if (piece(position) == null)
            return null;

        Piece aux = piece(position);
        _pieces[position.row, position.column] = null;
        aux.position = null;
        return aux;
    }
}