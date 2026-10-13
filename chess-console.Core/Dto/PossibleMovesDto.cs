namespace chess_console.Core.Dto;

// O bool[,] de PossibleMoves() vira uma lista de casas ("e3", "e4"): array 2D de
// bool serializa mal em JSON e obriga o front a conhecer a indexação do tabuleiro.
public sealed record PossibleMovesDto(
    string Origin,
    IReadOnlyList<string> Targets);
