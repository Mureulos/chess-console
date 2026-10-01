using board;

namespace chess;

public class ChessPosition
{
    public char column { get; set; }
    public int row { get; set; }

    public ChessPosition(char column, int row)
    {
        this.column = column;
        this.row = row;
    }

    public Position ToPosition()
    {
        return new Position(8 - row, column - 'a');
    }

    // Inversa de ToPosition(). A regra de que row 0 é a linha 8 mora só aqui — qualquer
    // DTO que precise de notação humana ("e4") deve passar por este par de métodos.
    public static ChessPosition FromPosition(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);

        return new ChessPosition((char)('a' + position.column), 8 - position.row);
    }

    public override string ToString()
    {
        return "" + column + row;
    }
}
