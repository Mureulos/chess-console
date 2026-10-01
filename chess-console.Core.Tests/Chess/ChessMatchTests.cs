using board;
using chess;
using chess_console.Core.Dto;
using chess_console.Exceptions;

namespace chess_console.Core.Tests.Chess;

// Regras puras, sem Hub e sem UI: o SetBoard() é fixo, então toda posição aqui é
// alcançada jogando lances legais a partir da posição inicial.
public class ChessMatchTests
{
    [Fact]
    public void SetBoard_StartsWithThirtyTwoPiecesAndWhiteToMove()
    {
        ChessMatch match = new();

        Assert.Equal(1, match.turn);
        Assert.Equal(Color.White, match.actualPlayerColor);
        Assert.Equal(16, match.GetPiecesInGame(Color.White).Count);
        Assert.Equal(16, match.GetPiecesInGame(Color.Black).Count);
        Assert.IsType<King>(PieceAt(match, "e1"));
        Assert.IsType<Queen>(PieceAt(match, "d1"));
        Assert.IsType<Tower>(PieceAt(match, "h8"));
        Assert.IsType<Knight>(PieceAt(match, "b8"));
        Assert.Null(PieceAt(match, "e4"));
    }

    [Fact]
    public void MakeMove_AlternatesTurnAndPlayer()
    {
        ChessMatch match = Match("e2e4");

        Assert.Equal(2, match.turn);
        Assert.Equal(Color.Black, match.actualPlayerColor);

        Play(match, "e7e5");

        Assert.Equal(3, match.turn);
        Assert.Equal(Color.White, match.actualPlayerColor);
    }

    [Fact]
    public void ValideOriginPosition_RejectsEmptySquaresOpponentPiecesAndBlockedPieces()
    {
        ChessMatch match = new();

        Assert.Equal("Piece not found in that position", ErrorOf(() => match.ValideOriginPosition(Square("e4"))));
        Assert.Equal("This piece is not yours", ErrorOf(() => match.ValideOriginPosition(Square("e7"))));
        Assert.Equal("There are no possible moves for this piece", ErrorOf(() => match.ValideOriginPosition(Square("c1"))));
    }

    [Fact]
    public void ValidadeTargetPosition_RejectsASquareThePieceCannotReach()
    {
        ChessMatch match = new();

        Assert.Equal("Invalid target position", ErrorOf(() => match.ValidadeTargetPosition(Square("e2"), Square("e5"))));
    }

    [Fact]
    public void GetCapturedPieces_GroupsCapturesByColour()
    {
        ChessMatch match = Match("e2e4", "d7d5", "e4d5");

        Assert.Empty(match.GetCapturedPieces(Color.White));
        Assert.IsType<Pawn>(Assert.Single(match.GetCapturedPieces(Color.Black)));
        Assert.Equal(15, match.GetPiecesInGame(Color.Black).Count);
        Assert.Equal(16, match.GetPiecesInGame(Color.White).Count);
    }

    // ---- Xeque ----

    [Fact]
    public void IsInCheck_DetectsAnAttackOnTheKing()
    {
        ChessMatch match = Match("e2e4", "d7d5", "f1b5");

        Assert.True(match.check);
        Assert.True(match.IsInCheck(Color.Black));
        Assert.False(match.IsInCheck(Color.White));
        Assert.False(match.completed);
    }

    [Fact]
    public void MakeMove_RefusesAMoveThatLeavesYourOwnKingInCheck()
    {
        ChessMatch match = Match("e2e4", "e7e5", "f1b5");

        Assert.Equal("You can't put yourself in check", ErrorOf(() => Play(match, "d7d6")));

        Assert.IsType<Pawn>(PieceAt(match, "d7"));
        Assert.Null(PieceAt(match, "d6"));
        Assert.Equal(Color.Black, match.actualPlayerColor);
        Assert.Equal(4, match.turn);
    }

    // ---- Xeque-mate ----

    [Fact]
    public void IsInCheckmate_DetectsFoolsMate()
    {
        ChessMatch match = Match("f2f3", "e7e5", "g2g4", "d8h4");

        Assert.True(match.check);
        Assert.True(match.completed);
        Assert.True(match.IsInCheckmate(Color.White));
        Assert.Equal(Color.Black, match.Opponent(match.actualPlayerColor));
    }

    [Fact]
    public void IsInCheckmate_IsFalseWhileTheCheckCanStillBeAnswered()
    {
        ChessMatch match = Match("e2e4", "d7d5", "f1b5");

        Assert.False(match.IsInCheckmate(Color.Black));
        Assert.False(match.completed);
    }

    // IsInCheckmate executa e desfaz todos os lances do lado em xeque. Se o UndoMoviment
    // errar em algum deles, o tabuleiro volta diferente do que entrou.
    [Fact]
    public void IsInCheckmate_LeavesTheBoardExactlyAsItWas()
    {
        ChessMatch match = Match("e2e4", "d7d5", "f1b5");
        IReadOnlyList<PieceDto> before = BoardMapper.ToDto(match).Pieces;

        match.IsInCheckmate(Color.Black);

        Assert.Equal(before, BoardMapper.ToDto(match).Pieces);
    }

    // ---- Roque ----

    [Fact]
    public void PossibleMoves_OnlyOffersCastlingOnceThePathIsClear()
    {
        ChessMatch match = Match("e2e4", "e7e5");

        Assert.False(CanReach(match, "e1", "g1"));

        Play(match, "g1f3");
        Play(match, "b8c6");
        Play(match, "f1c4");
        Play(match, "g8f6");

        Assert.True(CanReach(match, "e1", "g1"));
    }

    [Fact]
    public void MakeMove_PerformsKingsideCastling()
    {
        ChessMatch match = Match("e2e4", "e7e5", "g1f3", "b8c6", "f1c4", "g8f6", "e1g1");

        Assert.IsType<King>(PieceAt(match, "g1"));
        Assert.IsType<Tower>(PieceAt(match, "f1"));
        Assert.Null(PieceAt(match, "e1"));
        Assert.Null(PieceAt(match, "h1"));
    }

    [Fact]
    public void MakeMove_PerformsQueensideCastling()
    {
        ChessMatch match = Match("d2d4", "d7d5", "b1c3", "b8c6", "c1f4", "c8f5", "d1d2", "d8d7", "e1c1");

        Assert.IsType<King>(PieceAt(match, "c1"));
        Assert.IsType<Tower>(PieceAt(match, "d1"));
        Assert.Null(PieceAt(match, "e1"));
        Assert.Null(PieceAt(match, "a1"));
    }

    [Fact]
    public void PossibleMoves_DropsCastlingOnceTheKingHasMoved()
    {
        ChessMatch match = Match(
            "e2e4", "e7e5", "g1f3", "b8c6", "f1c4", "g8f6",
            "e1f1", "f8c5", "f1e1", "d8e7");

        Assert.False(CanReach(match, "e1", "g1"));
    }

    [Fact]
    public void PossibleMoves_DropsCastlingOnceTheRookHasMoved()
    {
        ChessMatch match = Match(
            "e2e4", "e7e5", "g1f3", "b8c6", "f1c4", "g8f6",
            "h1g1", "f8c5", "g1h1", "d8e7");

        Assert.False(CanReach(match, "e1", "g1"));
    }

    // ---- En passant ----

    [Fact]
    public void VulnerableEnPassant_LastsOnlyForTheFollowingMove()
    {
        ChessMatch match = Match("e2e4", "a7a6", "e4e5", "d7d5");

        Assert.Same(PieceAt(match, "d5"), match.vulnerableEnPassant);

        Play(match, "a2a3");

        Assert.Null(match.vulnerableEnPassant);
    }

    [Fact]
    public void MakeMove_CapturesEnPassantToTheLeftAsWhite()
    {
        ChessMatch match = Match("e2e4", "a7a6", "e4e5", "d7d5", "e5d6");

        AssertEnPassant(match, landedOn: "d6", captureCameFrom: "d5", capturedColor: Color.Black);
    }

    [Fact]
    public void MakeMove_CapturesEnPassantToTheRightAsWhite()
    {
        ChessMatch match = Match("e2e4", "a7a6", "e4e5", "f7f5", "e5f6");

        AssertEnPassant(match, landedOn: "f6", captureCameFrom: "f5", capturedColor: Color.Black);
    }

    [Fact]
    public void MakeMove_CapturesEnPassantToTheRightAsBlack()
    {
        ChessMatch match = Match("a2a3", "e7e5", "a3a4", "e5e4", "f2f4", "e4f3");

        AssertEnPassant(match, landedOn: "f3", captureCameFrom: "f4", capturedColor: Color.White);
    }

    // ---- Promoção ----

    [Fact]
    public void MakeMove_PromotesAPawnThatReachesTheLastRank()
    {
        ChessMatch match = Match(
            "a2a4", "b7b5", "a4b5", "a7a6", "b5b6", "a6a5", "b6c7", "a5a4", "c7b8");

        Piece? promoted = PieceAt(match, "b8");

        Assert.IsType<Queen>(promoted);
        Assert.Equal(Color.White, promoted!.color);
        Assert.Contains(promoted, match.GetPiecesInGame(Color.White));
    }

    // ---- Auxiliares ----

    private static ChessMatch Match(params string[] moves)
    {
        ChessMatch match = new();

        foreach (string move in moves)
        {
            Play(match, move);
        }

        return match;
    }

    private static void Play(ChessMatch match, string move)
    {
        Position origin = Square(move[..2]);
        Position target = Square(move[2..]);

        match.ValideOriginPosition(origin);
        match.ValidadeTargetPosition(origin, target);
        match.MakeMove(origin, target);
    }

    private static bool CanReach(ChessMatch match, string from, string to)
    {
        Position target = Square(to);

        return PieceAt(match, from)!.PossibleMoves()[target.row, target.column];
    }

    private static void AssertEnPassant(ChessMatch match, string landedOn, string captureCameFrom, Color capturedColor)
    {
        Piece? pawn = PieceAt(match, landedOn);

        Assert.IsType<Pawn>(pawn);
        Assert.Equal(match.Opponent(capturedColor), pawn!.color);
        Assert.Null(PieceAt(match, captureCameFrom));
        Assert.IsType<Pawn>(Assert.Single(match.GetCapturedPieces(capturedColor)));
    }

    private static string ErrorOf(Action action) => Assert.Throws<BoardException>(action).Message;

    private static Position Square(string square) => ChessPosition.Parse(square).ToPosition();

    private static Piece? PieceAt(ChessMatch match, string square) => match.board.piece(Square(square));
}
