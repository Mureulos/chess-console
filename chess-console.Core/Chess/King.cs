using board;

namespace chess;

public class King : Piece
{
    private ChessMatch _chessMatch;

    public King(Board board, Color color, ChessMatch chessMatch) : base(board, color)
    {
        _chessMatch = chessMatch;
    }

    public override string ToString()
    {
        return "♚";
    }

    private bool CanMove(Position position)
    {
        Piece piece = board.piece(position);
        return piece == null || piece.color != color;
    }

    private bool CanRock(Position position)
    {
        Piece piece = board.piece(position);
        return piece != null && piece is Tower && piece.color == color && piece.moveCount == 0;
    }

    // As oito casas vizinhas, sem o roque: é isto que o rei de fato ataca.
    public override bool[,] AttackedSquares()
    {
        bool[,] matrix = new bool[board.rows, board.columns];
        Position position = new Position(0, 0);

        // above
        position.defineValues(this.position.row - 1, this.position.column);
        if (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
        }

        // northeast
        position.defineValues(this.position.row - 1, this.position.column + 1);
        if (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
        }

        // right
        position.defineValues(this.position.row, this.position.column + 1);
        if (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
        }

        // southeast
        position.defineValues(this.position.row + 1, this.position.column + 1);
        if (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
        }

        // below
        position.defineValues(this.position.row + 1, this.position.column);
        if (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
        }

        // southwest
        position.defineValues(this.position.row + 1, this.position.column - 1);
        if (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
        }

        // left
        position.defineValues(this.position.row, this.position.column - 1);
        if (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
        }

        // northwest
        position.defineValues(this.position.row - 1, this.position.column - 1);
        if (board.IsPositionValid(position) && CanMove(position))
        {
            matrix[position.row, position.column] = true;
        }

        return matrix;
    }

    public override bool[,] PossibleMoves()
    {
        bool[,] matrix = AttackedSquares();

        if (moveCount == 0 && !_chessMatch.check)
        {
            Color opponent = _chessMatch.Opponent(color);

            Position rockPosition1 = new Position(this.position.row, this.position.column + 3);
            if (CanRock(rockPosition1))
            {
                // p1 é a casa de passagem do rei e p2 a de chegada. Rocar atravessando
                // casa atacada é proibido; a de chegada o MakeMove já recusa.
                Position p1 = new Position(this.position.row, this.position.column + 1);
                Position p2 = new Position(this.position.row, this.position.column + 2);
                if (board.piece(p1) == null && board.piece(p2) == null
                    && !_chessMatch.IsUnderAttack(p1, opponent))
                {
                    matrix[this.position.row, this.position.column + 2] = true;
                }
            }

            Position rockPosition2 = new Position(this.position.row, this.position.column - 4);
            if (CanRock(rockPosition2))
            {
                Position p1 = new Position(this.position.row, this.position.column - 1);
                Position p2 = new Position(this.position.row, this.position.column - 2);
                // p3 só precisa estar vazia: por ela passa a torre, não o rei.
                Position p3 = new Position(this.position.row, this.position.column - 3);
                if (board.piece(p1) == null && board.piece(p2) == null && board.piece(p3) == null
                    && !_chessMatch.IsUnderAttack(p1, opponent))
                {
                    matrix[this.position.row, this.position.column - 2] = true;
                }
            }
        }

        return matrix;
    }
}
