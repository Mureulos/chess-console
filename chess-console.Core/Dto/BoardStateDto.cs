namespace chess_console.Core.Dto;

// Estado completo da partida — o equivalente serializável do que
// GameDisplay.DisplayFullGame imprime hoje no terminal.
public sealed record BoardStateDto(
    IReadOnlyList<PieceDto> Pieces,
    int Turn,
    string CurrentPlayer,
    bool Check,
    bool Completed,
    bool Draw,
    string? Winner,
    IReadOnlyList<PieceDto> CapturedWhitePieces,
    IReadOnlyList<PieceDto> CapturedBlackPieces,
    IReadOnlyList<string> MoveHistory);
