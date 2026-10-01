using board;

namespace chess;

public class Pawn : Piece
{
    private ChessMatch _chessMatch;
    
    public Pawn(Board board, Color color, ChessMatch chessMatch) : base(board, color)
    {
        _chessMatch = chessMatch;
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

    // As duas diagonais, ocupadas ou não. PossibleMoves() só marca a diagonal quando
    // há inimigo nela, mas a casa vazia ao lado continua atacada — é o que decide se
    // o rei pode atravessá-la no roque.
    public override bool[,] AttackedSquares()
    {
        bool[,] matrix = new bool[board.rows, board.columns];
        int forward = color == Color.White ? -1 : 1;

        Position left = new Position(this.position.row + forward, this.position.column - 1);
        if (board.IsPositionValid(left))
        {
            matrix[left.row, left.column] = true;
        }

        Position right = new Position(this.position.row + forward, this.position.column + 1);
        if (board.IsPositionValid(right))
        {
            matrix[right.row, right.column] = true;
        }

        return matrix;
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
            if (board.IsPositionValid(position) && Free(position) && Free(betweenPosition) && moveCount == 0)
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

            if (this.position.row == 3)
            {
                Position left = new Position(this.position.row, this.position.column - 1);
                
                if (board.IsPositionValid(left) && ExistEnemie(left) && board.piece(left) == _chessMatch.vulnerableEnPassant)
                    matrix[left.row - 1, left.column] = true;
                
                Position right  = new Position(this.position.row, this.position.column + 1);
                
                if (board.IsPositionValid(right) && ExistEnemie(right) && board.piece(right) == _chessMatch.vulnerableEnPassant)
                    matrix[right.row - 1, right.column] = true;
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
            if (board.IsPositionValid(position) && Free(position) && Free(betweenPosition) && moveCount == 0)
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
            
            if (this.position.row == 4)
            {
                Position left = new Position(this.position.row, this.position.column - 1);

                // O preto anda para baixo (row cresce): o destino é row + 1, igual ao
                // lado direito logo abaixo.
                if (board.IsPositionValid(left) && ExistEnemie(left) && board.piece(left) == _chessMatch.vulnerableEnPassant)
                    matrix[left.row + 1, left.column] = true;
                
                Position right  = new Position(this.position.row, this.position.column + 1);
                
                if (board.IsPositionValid(right) && ExistEnemie(right) && board.piece(right) == _chessMatch.vulnerableEnPassant)
                    matrix[right.row + 1, right.column] = true;
            }
        }
        
        return matrix;
    }
}