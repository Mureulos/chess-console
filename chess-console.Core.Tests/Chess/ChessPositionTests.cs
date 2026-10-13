using board;
using chess;

namespace chess_console.Core.Tests.Chess;

public class ChessPositionTests
{
    [Theory]
    [InlineData('a', 8, 0, 0)]
    [InlineData('h', 8, 0, 7)]
    [InlineData('a', 1, 7, 0)]
    [InlineData('h', 1, 7, 7)]
    [InlineData('e', 4, 4, 4)]
    public void FromPosition_UndoesTheInvertedRowRule(char column, int row, int expectedRow, int expectedColumn)
    {
        Position position = new ChessPosition(column, row).ToPosition();

        Assert.Equal(expectedRow, position.row);
        Assert.Equal(expectedColumn, position.column);
        Assert.Equal($"{column}{row}", ChessPosition.FromPosition(position).ToString());
    }

    [Fact]
    public void FromPosition_RoundTripsEverySquareOfTheBoard()
    {
        for (char column = 'a'; column <= 'h'; column++)
        {
            for (int row = 1; row <= 8; row++)
            {
                ChessPosition original = new(column, row);
                ChessPosition roundTripped = ChessPosition.FromPosition(original.ToPosition());

                Assert.Equal(original.column, roundTripped.column);
                Assert.Equal(original.row, roundTripped.row);
            }
        }
    }
}
