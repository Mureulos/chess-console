using board;

namespace chess;

public class Knight : Piece
{
    public Knight(Board board, Color color) : base(board, color)
    {
    }

    public override string ToString()
    { 
        return "♞"; 
    }

    private bool CanMove(Position position)
    {
        Piece piece = board.piece(position);
        return piece == null || piece.color != color;
    }
    
    public override bool[,] PossibleMoves()
    {
        bool[,] matrix = new bool[board.rows, board.columns];
        Position position = new Position(0, 0);
        
        position.defineValues(this.position.row - 1, this.position.column - 2);
        if (board.IsPositionValid(position) && CanMove(position))
            matrix[position.row, position.column] = true;
        
        position.defineValues(this.position.row - 2, this.position.column - 1);
        if (board.IsPositionValid(position) && CanMove(position))
            matrix[position.row, position.column] = true;
        
        position.defineValues(this.position.row - 2, this.position.column + 1);
        if (board.IsPositionValid(position) && CanMove(position))
            matrix[position.row, position.column] = true;
        
        position.defineValues(this.position.row - 1, this.position.column + 2);
        if (board.IsPositionValid(position) && CanMove(position))
            matrix[position.row, position.column] = true;
        
        position.defineValues(this.position.row + 1, this.position.column + 2);
        if (board.IsPositionValid(position) && CanMove(position))
            matrix[position.row, position.column] = true;
        
        position.defineValues(this.position.row + 2, this.position.column + 1);
        if (board.IsPositionValid(position) && CanMove(position))
            matrix[position.row, position.column] = true;
        
        position.defineValues(this.position.row + 2, this.position.column - 1);
        if (board.IsPositionValid(position) && CanMove(position))
            matrix[position.row, position.column] = true;
        
        position.defineValues(this.position.row + 1, this.position.column - 2);
        if (board.IsPositionValid(position) && CanMove(position))
            matrix[position.row, position.column] = true;
        
        return matrix;
    }
}