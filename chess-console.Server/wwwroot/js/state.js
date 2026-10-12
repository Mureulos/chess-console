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

export const ZOOM_STORAGE_KEY = 'chess-console.zoom';
const storedZoom = Number.parseFloat(localStorage.getItem(ZOOM_STORAGE_KEY));

export const state = {
    matchId: null,
    myColor: null,
    board: null,
    selected: null,
    targets: [],
    lastMove: null,
    moveHistory: [],
    opponentPresent: false,
    clockSyncedAt: 0,
    zoom: Number.isFinite(storedZoom) ? storedZoom : 1
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

export function canOfferSideSwap() {
    return Boolean(state.board)
        && state.opponentPresent
        && !state.board.completed
        && state.moveHistory.length === 0;
}

export function isLightSquare(file, rank) {
    return (FILES.indexOf(file) + rank) % 2 === 0;
}

export function clearSelection() {
    state.selected = null;
    state.targets = [];
}

export function clockSeconds(color) {
    const board = state.board;
    const seconds = color === 'White'
        ? board?.whiteTimeSeconds
        : board?.blackTimeSeconds;

    if (!Number.isFinite(seconds))
        return 0;

    if (!board?.clockRunning || board.currentPlayer !== color || !state.clockSyncedAt)
        return Math.max(0, seconds);

    const elapsed = Math.floor((performance.now() - state.clockSyncedAt) / 1000);
    return Math.max(0, seconds - elapsed);
}
