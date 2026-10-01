namespace chess_console.Core.Dto;

// Peça achatada para serialização: sem Board e sem ChessMatch pendurados.
// Position vem em notação humana ("e4") e é null para peça capturada, que não
// está mais em nenhuma casa.
public sealed record PieceDto(
    string Type,
    string Color,
    string? Position);
