namespace board;

public class Piece
{
    public Position position { get; set; }
    public Color color { get; set; }
    public Board board { get; set; } 
    private int _moveCount { get; set; }

    public Piece(Board board, Color color)
    {
        this.position = position;
        this.color = color;
        this.board = board;
        this._moveCount = 0;
    }
    
    public Color GetColor() {
        return this.color;
    }
}