using chess_console.Exceptions;

namespace board;

public class Board
{   
    public int rows { get; set; }
    public int colums { get; set; }
    private Piece[,] _pieces;   
    
    public Board(int rows = 8, int colums = 8)
    {
        this.rows = rows;
        this.colums = colums;
        _pieces = new Piece[rows, colums];
    }
    
    public Piece piece(int row, int colums)
    {
        return _pieces[row, colums];
    }
    
    public Piece piece(Position position)
    {
        if (position == null)
            return null;
        
        ValidatePosition(position);
        return _pieces[position.row, position.column];
    }

    private bool IsPositionValid(Position position)
    {
        if (position.row < 0 || position.row >= rows || position.column < 0 || position.column >= colums)
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