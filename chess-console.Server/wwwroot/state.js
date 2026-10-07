export const FILES = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];

export const GLYPHS = {
    King: '♚',
    Queen: '♛',
    Rook: '♜',
    Bishop: '♝',
    Knight: '♞',
    Pawn: '♟'
};

export const ZOOM = {
    min: 0.6,
    max: 1.4,
    step: 0.1
};

export const state = {
    matchId: null,
    myColor: null,
    board: null,
    selected: null,
    targets: [],
    lastMove: null,
    moveHistory: [],
    opponentPresent: false,
    zoom: 1
};

export function glyphOf(piece) {
    return GLYPHS[piece.type] ?? '?';
}

export function pieceAt(square) {
    return state.board?.pieces.find(piece => piece.position === square) ?? null;
}

export function isMyTurn() {
    const board = state.board;

    return Boolean(board)
        && !board.completed
        && state.opponentPresent
        && board.currentPlayer === state.myColor;
}

export function isLightSquare(file, rank) {
    return (FILES.indexOf(file) + rank) % 2 === 0;
}

export function clearSelection() {
    state.selected = null;
    state.targets = [];
}
