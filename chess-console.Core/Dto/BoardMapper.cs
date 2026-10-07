using board;
using chess;

namespace chess_console.Core.Dto;

// Faz o mesmo trabalho do GameDisplay — ler o ChessMatch inteiro e montar uma
// representação de saída — só que devolvendo objetos serializáveis em vez de
// escrever no Console.
public static class BoardMapper
{
    public static BoardStateDto ToDto(ChessMatch match)
    {
        ArgumentNullException.ThrowIfNull(match);

        return new BoardStateDto(
            Pieces: ReadBoard(match.board),
            Turn: match.turn,
            CurrentPlayer: match.actualPlayerColor.ToString(),
            Check: match.check,
            Completed: match.completed,
            Draw: match.draw,
            // Em xeque-mate o ChessMatch já trocou de jogador, então quem leva é o
            // adversário da vez — a mesma conta que o Program.cs faz no fim do jogo.
            Winner: match.completed && !match.draw ? match.Opponent(match.actualPlayerColor).ToString() : null,
            CapturedWhitePieces: ToDto(match.GetCapturedPieces(Color.White)),
            CapturedBlackPieces: ToDto(match.GetCapturedPieces(Color.Black)),
            MoveHistory: match.MoveHistory.ToArray());
    }

    public static PieceDto ToDto(Piece piece)
    {
        ArgumentNullException.ThrowIfNull(piece);

        return new PieceDto(
            TypeNameOf(piece),
            piece.color.ToString(),
            piece.position is null ? null : ChessPosition.FromPosition(piece.position).ToString());
    }

    // Valida a origem com as regras que já existem no ChessMatch (mesmo caminho do
    // Program.cs), então a BoardException continua sendo a forma de reportar erro.
    public static PossibleMovesDto ToPossibleMoves(ChessMatch match, Position origin)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(origin);

        match.ValideOriginPosition(origin);

        return new PossibleMovesDto(
            ChessPosition.FromPosition(origin).ToString(),
            ToSquares(match.board.piece(origin).PossibleMoves()));
    }

    public static IReadOnlyList<string> ToSquares(bool[,] possibleMoves)
    {
        ArgumentNullException.ThrowIfNull(possibleMoves);

        List<string> squares = new();

        for (int row = 0; row < possibleMoves.GetLength(0); row++)
        {
            for (int column = 0; column < possibleMoves.GetLength(1); column++)
            {
                if (possibleMoves[row, column])
                    squares.Add(ChessPosition.FromPosition(new Position(row, column)).ToString());
            }
        }

        return squares;
    }

    public static MoveResultDto ToMoveResult(ChessMatch match, Position origin, Position target)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(target);

        return new MoveResultDto(
            ChessPosition.FromPosition(origin).ToString(),
            ChessPosition.FromPosition(target).ToString(),
            ToDto(match));
    }

    // Lê direto do tabuleiro, e não do HashSet interno de peças, porque o tabuleiro
    // é o que o jogador enxerga — é a mesma varredura do GameDisplay.DisplayBoard.
    private static IReadOnlyList<PieceDto> ReadBoard(Board board)
    {
        List<PieceDto> pieces = new();

        for (int row = 0; row < board.rows; row++)
        {
            for (int column = 0; column < board.columns; column++)
            {
                Piece piece = board.piece(row, column);

                if (piece is not null)
                    pieces.Add(ToDto(piece));
            }
        }

        return pieces;
    }

    private static IReadOnlyList<PieceDto> ToDto(IEnumerable<Piece> pieces) =>
        pieces.Select(ToDto).ToArray();

    // O ToString() das peças devolve o glifo do console (♚, ♟) e é igual para as duas
    // cores — serve para o terminal, não para um contrato de API. O front recebe o
    // nome e decide como desenhar.
    private static string TypeNameOf(Piece piece) => piece switch
    {
        King => "King",
        Queen => "Queen",
        Tower => "Rook",
        Bishop => "Bishop",
        Knight => "Knight",
        Pawn => "Pawn",
        _ => piece.GetType().Name,
    };
}
