using board;

namespace chess;

public class Pawn : Piece
{
    public Pawn(Board board, Color color) : base(board, color)
    {
    }

    public override string ToString()
    { 
        return "♟";
    }

    private bool ExistEnemie(Position position)
    {
        Piece piece = board.piece(position);
        return piece != null && piece.color != color;
    }

    private bool Free(Position position)
    {
        return board.piece(position) == null;       
    }
    
    public override bool[,] PossibleMoves()
    {
        bool[,] matrix = new bool[board.rows, board.columns];
        Position position = new Position(0, 0);

        if (color == Color.White)
        {
            position.defineValues(this.position.row - 1, this.position.column);
            if (board.IsPositionValid(position) && Free(position))
            {
                matrix[position.row, position.column] = true;
            }
            
            position.defineValues(this.position.row - 2, this.position.column);
            Position betweenPosition = new Position(this.position.row - 1, this.position.column);
            if (board.IsPositionValid(position) && Free(position) && Free(betweenPosition) && _moveCount == 0)
            {
                matrix[position.row, position.column] = true;
            }
            
            position.defineValues(this.position.row - 1, this.position.column - 1);
            if (board.IsPositionValid(position) && ExistEnemie(position))
            {
                matrix[position.row, position.column] = true;
            }
            
            position.defineValues(this.position.row - 1, this.position.column + 1);
            if (board.IsPositionValid(position) && ExistEnemie(position))
            {
                matrix[position.row, position.column] = true;
            }
        }
        else
        {
            position.defineValues(this.position.row + 1, this.position.column);
            if (board.IsPositionValid(position) && Free(position))
            {
                matrix[position.row, position.column] = true;
            }
            
            position.defineValues(this.position.row + 2, this.position.column);
            Position betweenPosition = new Position(this.position.row + 1, this.position.column);
            if (board.IsPositionValid(position) && Free(position) && Free(betweenPosition) && _moveCount == 0)
            {
                matrix[position.row, position.column] = true;
            }
            
            position.defineValues(this.position.row + 1, this.position.column - 1);
            if (board.IsPositionValid(position) && ExistEnemie(position))
            {
                matrix[position.row, position.column] = true;
            }
            
            position.defineValues(this.position.row + 1, this.position.column + 1);
            if (board.IsPositionValid(position) && ExistEnemie(position))
            {
                matrix[position.row, position.column] = true;
            }
        }
        
        return matrix;
    }
}