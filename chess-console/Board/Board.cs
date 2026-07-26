namespace board;

public class Board
{   
    public int rows { get; set; }
    public int colums { get; set; }
    private Piece[,] _pieces;   
    
    public Board(int rows, int colums)
    {
        this.rows = rows;
        this.colums = colums;
        _pieces = new Piece[rows, colums];
    }
    
    public Piece piece(int row, int colums)
    {
        return _pieces[row, colums];
    }

    public void PutPiece(Piece piece, Position position)
    {
        _pieces[position.row, position.column] = piece;
        piece.position = position;
    }
}