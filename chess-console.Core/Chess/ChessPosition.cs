using board;
using chess_console.Exceptions;

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

    // Caminho de entrada: no console o texto vem do próprio jogador, mas num Hub vem
    // de um client qualquer — "", "zz" ou 10 KB de lixo não podem derrubar a chamada.
    public static bool TryParse(string? text, out ChessPosition? position)
    {
        position = null;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        string square = text.Trim();

        if (square.Length != 2)
            return false;

        char column = char.ToLowerInvariant(square[0]);
        int row = square[1] - '0';

        if (column < 'a' || column > 'h' || row < 1 || row > 8)
            return false;

        position = new ChessPosition(column, row);
        return true;
    }

    // Não ecoa o texto recebido na mensagem: ela chega ao outro jogador e ao log.
    public static ChessPosition Parse(string? text) =>
        TryParse(text, out ChessPosition? position)
            ? position!
            : throw new BoardException("Invalid board square: use a column from a to h and a row from 1 to 8");

    public override string ToString()
    {
        return "" + column + row;
    }
}
