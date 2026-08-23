namespace board;

public abstract class Piece
{
    public Position position { get; set; }
    public Color color { get; set; }
    public Board board { get; set; } 
    public int _moveCount { get; set; }

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

    public void AddMove()
    {
        _moveCount++;   
    }
    
    public void SubMove()
    {
        _moveCount--;   
    }

    public bool HasPossibleMoves()
    {
        bool [,] matrix = PossibleMoves();
        for (int i = 0; i < board.rows; i++)
        {
            for (int j = 0; j < board.columns; j++)
            {
                if (matrix[i, j] == true)
                    return true;
            }
        }
        
        return false;
    }
    
    public bool CanMoveTo(Position position)
    {
        return PossibleMoves()[position.row, position.column];
    }

    public abstract bool[,] PossibleMoves();
}