using board;

namespace chess;

public class Tower : Piece
{
    public Tower(Board board, Color color) : base(board, color)
    {
    }

    public override string ToString()
    {
        return "♜";
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
        while (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            position.defineValues(position.row - 1, position.column);
        }

        // right
        position.defineValues(this.position.row, this.position.column + 1);
        while (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            position.defineValues(position.row, position.column + 1);
        }

        // below
        position.defineValues(this.position.row + 1, this.position.column);
        while (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            position.defineValues(position.row + 1, position.column);
        }

        // left
        position.defineValues(this.position.row, this.position.column - 1);
        while (board.IsPositionValid(position) && canMove(position))
        {
            matrix[position.row, position.column] = true;
            if (board.piece(position) != null && board.piece(position).color != color)
                break;
            position.defineValues(position.row, position.column - 1);
        }

        return matrix;
    }
}