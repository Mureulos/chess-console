const $ = id => document.getElementById(id);

export const ui = {
    connection: $('connection'),
    panelConnection: $('panel-connection'),
    lobby: $('lobby'),
    game: $('game'),
    create: $('create'),
    joinForm: $('join-form'),
    joinButton: document.querySelector('#join-form button'),
    matchIdInput: $('match-id'),
    boardArea: $('board-area'),
    board: $('board'),
    files: $('files'),
    matchLabel: $('match-label'),
    myColor: $('my-color'),
    turn: $('turn'),
    currentPlayer: $('current-player'),
    gameState: $('game-state'),
    capturedWhite: $('captured-white'),
    capturedBlack: $('captured-black'),
    connectionMatchLabel: $('connection-match-label'),
    moveHistory: $('move-history'),
    shareLink: $('share-link'),
    zoomIn: $('zoom-in'),
    zoomOut: $('zoom-out'),
    zoomLevel: $('zoom-level'),
    log: $('log')
};

export function say(message, kind = 'info') {
    ui.log.textContent = message;
    ui.log.dataset.kind = kind;
}

export function setConnectionState(value, label) {
    const connected = value === 'connected';

    ui.connection.dataset.state = value;
    ui.connection.textContent = label;
    ui.panelConnection.dataset.state = value;
    ui.panelConnection.textContent = label;
    ui.create.disabled = !connected;
    ui.joinButton.disabled = !connected;
}
