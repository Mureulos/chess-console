using System.Text.Json;
using board;
using chess;
using chess_console.Core.Dto;
using chess_console.Exceptions;

namespace chess_console.Core.Tests.Dto;

public class BoardMapperTests
{
    [Fact]
    public void ToDto_MapsTheOpeningPosition()
    {
        BoardStateDto state = BoardMapper.ToDto(new ChessMatch());

        Assert.Equal(32, state.Pieces.Count);
        Assert.Equal(1, state.Turn);
        Assert.Equal("White", state.CurrentPlayer);
        Assert.False(state.Check);
        Assert.False(state.Completed);
        Assert.Null(state.Winner);
        Assert.Empty(state.CapturedWhitePieces);
        Assert.Empty(state.CapturedBlackPieces);

        Assert.Equal(new PieceDto("King", "White", "e1"), PieceAt(state, "e1"));
        Assert.Equal(new PieceDto("Rook", "Black", "a8"), PieceAt(state, "a8"));
        Assert.Equal(new PieceDto("Knight", "Black", "g8"), PieceAt(state, "g8"));
        Assert.Equal(new PieceDto("Pawn", "White", "d2"), PieceAt(state, "d2"));
    }

    [Fact]
    public void ToDto_NeverLeaksTheBoardOrTheMatchIntoThePayload()
    {
        ChessMatch match = new();
        match.MakeMove(Square('e', 2), Square('e', 4));

        string json = JsonSerializer.Serialize(BoardMapper.ToDto(match));

        Assert.Contains("\"e4\"", json);
        Assert.DoesNotContain("board", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("moveCount", json, StringComparison.OrdinalIgnoreCase);

        BoardStateDto? roundTripped = JsonSerializer.Deserialize<BoardStateDto>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(32, roundTripped.Pieces.Count);
        Assert.Equal("Black", roundTripped.CurrentPlayer);
    }

    [Fact]
    public void ToDto_MovesThePieceToTheNewSquare()
    {
        ChessMatch match = new();
        match.MakeMove(Square('e', 2), Square('e', 4));

        BoardStateDto state = BoardMapper.ToDto(match);

        Assert.Null(PieceAt(state, "e2"));
        Assert.Equal(new PieceDto("Pawn", "White", "e4"), PieceAt(state, "e4"));
        Assert.Equal(2, state.Turn);
        Assert.Equal("Black", state.CurrentPlayer);
    }

    [Fact]
    public void ToDto_ListsCapturedPiecesWithoutASquare()
    {
        ChessMatch match = new();
        match.MakeMove(Square('e', 2), Square('e', 4));
        match.MakeMove(Square('d', 7), Square('d', 5));
        match.MakeMove(Square('e', 4), Square('d', 5));

        BoardStateDto state = BoardMapper.ToDto(match);

        Assert.Equal(31, state.Pieces.Count);
        Assert.Empty(state.CapturedWhitePieces);

        PieceDto captured = Assert.Single(state.CapturedBlackPieces);
        Assert.Equal("Pawn", captured.Type);
        Assert.Equal("Black", captured.Color);
        Assert.Null(captured.Position);
    }

    [Fact]
    public void ToDto_ReportsCheck()
    {
        ChessMatch match = new();
        match.MakeMove(Square('e', 2), Square('e', 4));
        match.MakeMove(Square('d', 7), Square('d', 5));
        match.MakeMove(Square('f', 1), Square('b', 5));

        BoardStateDto state = BoardMapper.ToDto(match);

        Assert.True(state.Check);
        Assert.False(state.Completed);
        Assert.Null(state.Winner);
        Assert.Equal("Black", state.CurrentPlayer);
    }

    [Fact]
    public void ToDto_ReportsTheWinnerOnCheckmate()
    {
        // Mate do pastor invertido (fool's mate): 1. f3 e5 2. g4 Qh4#
        ChessMatch match = new();
        match.MakeMove(Square('f', 2), Square('f', 3));
        match.MakeMove(Square('e', 7), Square('e', 5));
        match.MakeMove(Square('g', 2), Square('g', 4));
        match.MakeMove(Square('d', 8), Square('h', 4));

        BoardStateDto state = BoardMapper.ToDto(match);

        Assert.True(state.Check);
        Assert.True(state.Completed);
        Assert.Equal("Black", state.Winner);
    }

    [Fact]
    public void ToSquares_TurnsTheMatrixIntoChessNotation()
    {
        ChessMatch match = new();

        PossibleMovesDto moves = BoardMapper.ToPossibleMoves(match, Square('e', 2));

        Assert.Equal("e2", moves.Origin);
        Assert.Equal(new[] { "e4", "e3" }, moves.Targets);
    }

    [Fact]
    public void ToSquares_ReturnsAnEmptyListWhenNothingIsReachable()
    {
        Assert.Empty(BoardMapper.ToSquares(new bool[8, 8]));
    }

    [Fact]
    public void ToPossibleMoves_ReusesTheOriginValidationOfTheMatch()
    {
        ChessMatch match = new();

        BoardException empty = Assert.Throws<BoardException>(
            () => BoardMapper.ToPossibleMoves(match, Square('e', 4)));
        Assert.Equal("Piece not found in that position", empty.Message);

        BoardException wrongColor = Assert.Throws<BoardException>(
            () => BoardMapper.ToPossibleMoves(match, Square('e', 7)));
        Assert.Equal("This piece is not yours", wrongColor.Message);
    }

    [Fact]
    public void ToMoveResult_CarriesTheLastMoveNextToTheNewState()
    {
        ChessMatch match = new();
        Position origin = Square('g', 1);
        Position target = Square('f', 3);
        match.MakeMove(origin, target);

        MoveResultDto result = BoardMapper.ToMoveResult(match, origin, target);

        Assert.Equal("g1", result.Origin);
        Assert.Equal("f3", result.Target);
        Assert.Equal(new PieceDto("Knight", "White", "f3"), PieceAt(result.State, "f3"));
    }

    private static PieceDto? PieceAt(BoardStateDto state, string square) =>
        state.Pieces.SingleOrDefault(piece => piece.Position == square);

    private static Position Square(char column, int row) => new ChessPosition(column, row).ToPosition();
}
