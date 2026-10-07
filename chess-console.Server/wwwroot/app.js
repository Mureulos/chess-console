'use strict';

// =====================================================
// Constantes
// =====================================================

const FILES = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];

// Mesmo glifo para as duas cores; a cor vem do CSS (.piece--white / .piece--black)
const GLYPHS = { King: '♚', Queen: '♛', Rook: '♜', Bishop: '♝', Knight: '♞', Pawn: '♟' };

// Limites da escala do tabuleiro (1 = 100%)
const ZOOM = { min: 0.6, max: 1.4, step: 0.1 };

// =====================================================
// Referências do DOM
// =====================================================

const $ = id => document.getElementById(id);

const ui = {
    connection: $('connection'),
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
    shareLink: $('share-link'),
    zoomIn: $('zoom-in'),
    zoomOut: $('zoom-out'),
    zoomLevel: $('zoom-level'),
    log: $('log')
};

// =====================================================
// Estado da partida
// =====================================================

const state = {
    matchId: null,
    myColor: null,
    board: null,
    selected: null,
    targets: [],
    lastMove: null,
    opponentPresent: false,
    zoom: 1
};

// =====================================================
// Funções auxiliares
// =====================================================

const glyphOf = piece => GLYPHS[piece.type] ?? '?';

function pieceAt(square) {
    return state.board.pieces.find(piece => piece.position === square) ?? null;
}

function isMyTurn() {
    const board = state.board;
    return Boolean(board) && !board.completed && state.opponentPresent && board.currentPlayer === state.myColor;
}

function isLightSquare(file, rank) {
    return (FILES.indexOf(file) + rank) % 2 === 0;
}

function clearSelection() {
    state.selected = null;
    state.targets = [];
}

function say(message, kind = 'info') {
    ui.log.textContent = message;
    ui.log.dataset.kind = kind;
}

function setConnectionState(value, label) {
    const connected = value === 'connected';
    ui.connection.dataset.state = value;
    ui.connection.textContent = label;
    ui.create.disabled = !connected;
    ui.joinButton.disabled = !connected;
}

// =====================================================
// Renderização
// =====================================================

// Monta as 64 casas uma única vez, invertendo o tabuleiro para as pretas
function buildBoard() {
    const flipped = state.myColor === 'Black';
    const files = flipped ? [...FILES].reverse() : FILES;
    const ranks = flipped ? [1, 2, 3, 4, 5, 6, 7, 8] : [8, 7, 6, 5, 4, 3, 2, 1];

    ui.board.innerHTML = ranks.flatMap(rank => files.map(file => {
        const square = file + rank;
        const light = isLightSquare(file, rank) ? ' square--light' : '';
        return `<button type="button" class="square${light}" data-square="${square}" aria-label="${square}"></button>`;
    })).join('');

    ui.files.innerHTML = files.map(file => `<span>${file}</span>`).join('');
}

// Atualiza peças e destaques de cada casa
function render() {
    if (!state.board) return;

    const myTurn = isMyTurn();

    ui.board.querySelectorAll('.square').forEach(btn => {
        const square = btn.dataset.square;
        const piece = pieceAt(square);
        const isMine = piece?.color === state.myColor;
        const isTarget = state.targets.includes(square);
        const isCapture = isTarget && Boolean(piece) && !isMine;
        const isLast = state.lastMove?.origin === square || state.lastMove?.target === square;

        btn.textContent = piece ? glyphOf(piece) : '';
        btn.classList.toggle('square--last', isLast);
        btn.classList.toggle('square--target', isTarget && !isCapture);
        btn.classList.toggle('square--capture', isCapture);
        btn.classList.toggle('square--selected', state.selected === square);
        btn.classList.toggle('square--playable', myTurn && (isTarget || isMine));
        btn.classList.toggle('piece--white', piece?.color === 'White');
        btn.classList.toggle('piece--black', piece?.color === 'Black');
    });

    renderPanel();
}

// Atualiza o painel lateral
function renderPanel() {
    const board = state.board;
    const glyphs = pieces => pieces.map(glyphOf).join('') || '—';

    ui.matchLabel.textContent = state.matchId;
    ui.myColor.textContent = state.myColor ?? '—';
    ui.turn.textContent = board.turn;
    ui.currentPlayer.textContent = board.currentPlayer;
    ui.capturedWhite.textContent = glyphs(board.capturedWhitePieces);
    ui.capturedBlack.textContent = glyphs(board.capturedBlackPieces);
    ui.gameState.innerHTML = describeState(board);
}

function describeState(board) {
    if (board.completed) {
        if (board.draw) return 'Draw.';
        return board.winner === state.myColor
            ? `Checkmate — you won as ${board.winner}.`
            : `Checkmate — victory for ${board.winner}.`;
    }
    if (!state.opponentPresent) return '<span class="spinner"></span> Waiting for opponent...';
    if (board.check) return `Check on ${board.currentPlayer}.`;
    return isMyTurn() ? 'Your turn.' : "Opponent's turn.";
}

// =====================================================
// Zoom do tabuleiro
// =====================================================

// Limita o valor ao intervalo permitido e repassa para o CSS via --zoom
function setZoom(value) {
    const rounded = Math.round(value * 10) / 10;
    state.zoom = Math.min(ZOOM.max, Math.max(ZOOM.min, rounded));

    ui.boardArea.style.setProperty('--zoom', state.zoom);
    ui.zoomLevel.textContent = `${Math.round(state.zoom * 100)}%`;
    ui.zoomOut.disabled = state.zoom <= ZOOM.min;
    ui.zoomIn.disabled = state.zoom >= ZOOM.max;
}

// =====================================================
// Conexão SignalR
// =====================================================

const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/chess')
    .withAutomaticReconnect()
    .build();

connection.onreconnecting(() => setConnectionState('connecting', 'reconnecting...'));
connection.onclose(() => setConnectionState('disconnected', 'disconnected'));
connection.onreconnected(async () => {
    setConnectionState('connected', 'connected');
    if (state.matchId) await enterMatch('JoinMatch', state.matchId);
});

// =====================================================
// Ações do jogador (chamadas ao servidor)
// =====================================================

// Cria (sem matchId) ou entra (com matchId) em uma partida
async function enterMatch(method, matchId) {
    const joining = method === 'JoinMatch';
    const args = joining ? [matchId] : [];
    const match = await connection.invoke(method, ...args);
    if (!match) return;

    Object.assign(state, {
        matchId: match.matchId,
        myColor: match.color,
        board: match.state,
        opponentPresent: joining,
        lastMove: null
    });
    clearSelection();

    location.hash = match.matchId;
    ui.lobby.hidden = true;
    ui.game.hidden = false;

    buildBoard();
    render();

    say(joining
        ? `You play as ${match.color}.`
        : 'Match created. Share the link or ID for the opponent to join.');
}

// Clique numa casa: move se for destino, seleciona se for peça própria
async function onSquareClick(square) {
    if (!isMyTurn()) return;

    if (state.targets.includes(square)) {
        const origin = state.selected;
        const movingPiece = pieceAt(origin);
        const promotion = promotionChoice(movingPiece, square);
        if (promotion === false) {
            say('Choose a valid promotion piece.', 'error');
            return;
        }

        clearSelection();
        render();
        const method = promotion ? 'MakeMoveWithPromotion' : 'MakeMove';
        await connection.invoke(method, state.matchId, origin, square, ...(promotion ? [promotion] : []));
        return;
    }

    clearSelection();

    if (pieceAt(square)?.color === state.myColor) {
        const moves = await connection.invoke('GetPossibleMoves', state.matchId, square);
        if (moves) {
            state.selected = moves.origin;
            state.targets = moves.targets;
            say('');
        }
    }

    render();
}

function promotionChoice(piece, square) {
    if (piece?.type !== 'Pawn' || !['1', '8'].includes(square.at(-1))) return null;

    const choice = window.prompt('Promote to: Queen (Q), Rook (R), Bishop (B) or Knight (N)', 'Q');
    if (choice === null) return false;

    return {
        q: 'Queen',
        r: 'Rook',
        b: 'Bishop',
        n: 'Knight'
    }[choice.trim().toLowerCase()] ?? false;
}

// =====================================================
// Eventos recebidos do servidor
// =====================================================

connection.on('ReceiveBoardState', result => {
    state.board = result.state;
    state.lastMove = { origin: result.origin, target: result.target };
    clearSelection();
    render();
});

connection.on('ReceiveError', message => {
    clearSelection();
    say(message, 'error');
    render();
});

connection.on('OpponentJoined', color => {
    state.opponentPresent = true;
    say(`Opponent joined as ${color}.`);
    render();
});

connection.on('OpponentLeft', () => {
    state.opponentPresent = false;
    say('Opponent left the match.');
    render();
});

// =====================================================
// Eventos da interface
// =====================================================

ui.create.addEventListener('click', () => enterMatch('CreateMatch'));

ui.joinForm.addEventListener('submit', event => {
    event.preventDefault();
    const matchId = ui.matchIdInput.value.trim();
    if (matchId) enterMatch('JoinMatch', matchId);
});

ui.board.addEventListener('click', event => {
    const square = event.target.closest('.square--playable');
    if (square) onSquareClick(square.dataset.square);
});

ui.zoomIn.addEventListener('click', () => setZoom(state.zoom + ZOOM.step));
ui.zoomOut.addEventListener('click', () => setZoom(state.zoom - ZOOM.step));

ui.shareLink.addEventListener('click', async () => {
    try {
        await navigator.clipboard.writeText(location.href);
        const original = ui.shareLink.textContent;
        ui.shareLink.textContent = 'Copied!';
        setTimeout(() => ui.shareLink.textContent = original, 2000);
    } catch {
        say('Failed to copy link.', 'error');
    }
});

// =====================================================
// Inicialização
// =====================================================

setConnectionState('connecting', 'connecting...');
setZoom(state.zoom);

// Preenche o ID da partida se a página foi aberta por um link compartilhado
if (location.hash.length > 1) {
    ui.matchIdInput.value = decodeURIComponent(location.hash.slice(1));
}

connection.start()
    .then(() => setConnectionState('connected', 'connected'))
    .catch(error => {
        setConnectionState('disconnected', 'failed to connect');
        say(String(error), 'error');
    });