namespace chess_console.Board;

public class Position
{
    public int row { get; set; }
    public int column { get; set; }

    public Position(int row, int column)
    {
        this.row = row;
        this.column = column;
    }

    override public string ToString()
    {
        return "(" + row + "," + column + ")";
    }
}