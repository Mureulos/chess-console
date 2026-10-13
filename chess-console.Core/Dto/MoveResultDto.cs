namespace chess_console.Core.Dto;

// O que vai no broadcast depois de uma jogada: o estado novo mais a jogada que
// acabou de sair, para o front conseguir destacar as casas de origem e destino.
public sealed record MoveResultDto(
    string Origin,
    string Target,
    BoardStateDto State);
