using board;

namespace chess;

public class King : Piece
{
    public King(Board board, Color color) : base(board, color)
    {
    }

    public override string ToString()
    { 
        return "♚";
    }

    private bool canMove(Position position)
    {
        Piece piece = board.piece(position);
        return piece == null || piece.color != color;
    }
    
    public override bool[,] PossibleMoves()
    {
        bool[,] matrix = new bool[board.rows, board.columns];
        Position position = new Position(0, 0);
        
        // above
        position.defineValues(this.position.row - 1, this.position.column);
        if (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
        }
        
        // northeast
        position.defineValues(this.position.row - 1, this.position.column + 1);
        if (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
        }
        
        // right
        position.defineValues(this.position.row, this.position.column + 1);
        if (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
        }
        
        // southeast
        position.defineValues(this.position.row + 1, this.position.column + 1);
        if (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
        }
        
        // below
        position.defineValues(this.position.row + 1, this.position.column);
        if (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
        }
        
        // southwest
        position.defineValues(this.position.row + 1, this.position.column - 1);
        if (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
        }
        
        // left
        position.defineValues(this.position.row, this.position.column - 1);
        if (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
        }
        
        // northwest
        position.defineValues(this.position.row - 1, this.position.column - 1);
        if (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
        }
        
        return matrix;
    }
}