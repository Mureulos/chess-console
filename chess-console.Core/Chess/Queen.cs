using board;

namespace chess;

public class Queen : Piece
{
    public Queen(Board board, Color color) : base(board, color)
    {
    }

    public override string ToString()
    {
        return "♛";
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

        // above
        position.defineValues(this.position.row - 1, this.position.column);
        while (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            position.defineValues(position.row - 1, position.column);
        }
        
        // northeast
        position.defineValues(this.position.row - 1, this.position.column + 1);
        while (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;

            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            
            position.defineValues(position.row - 1, position.column + 1);
        }

        // right
        position.defineValues(this.position.row, this.position.column + 1);
        while (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            position.defineValues(position.row, position.column + 1);
        }
        
        // southeast
        position.defineValues(this.position.row + 1, this.position.column + 1);
        while (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;

            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            
            position.defineValues(position.row + 1, position.column + 1);
        }

        // below
        position.defineValues(this.position.row + 1, this.position.column);
        while (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            position.defineValues(position.row + 1, position.column);
        }
        
        // southwest
        position.defineValues(this.position.row + 1, this.position.column - 1);
        while (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;

            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            
            position.defineValues(position.row + 1, position.column - 1);
        }

        // left
        position.defineValues(this.position.row, this.position.column - 1);
        while (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            position.defineValues(position.row, position.column - 1);
        }
        
        // northwest
        position.defineValues(this.position.row - 1, this.position.column - 1);
        while (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;

            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            
            position.defineValues(position.row - 1, position.column - 1);
        }

        return matrix;
    }
}