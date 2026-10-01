namespace chess_console.Core.Dto;

// Resposta de CreateMatch/JoinMatch: além do id da partida, o jogador já descobre
// a cor que ocupou e recebe o tabuleiro, sem precisar de uma segunda chamada.
public sealed record MatchJoinedDto(
    Guid MatchId,
    string Color,
    BoardStateDto State);
