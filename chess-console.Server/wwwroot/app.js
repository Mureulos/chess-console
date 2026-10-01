'use strict';

const FILES = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];

// O ToString() das peças no Core devolve o glifo preto para as duas cores, porque lá
// quem separa branco de preto é o ConsoleColor. Na web dá para usar os dois jogos.
const GLYPHS = {
    White: { King: '♔', Queen: '♕', Rook: '♖', Bishop: '♗', Knight: '♘', Pawn: '♙' },
    Black: { King: '♚', Queen: '♛', Rook: '♜', Bishop: '♝', Knight: '♞', Pawn: '♟' }
};

const COLOR_LABEL = { White: 'brancas', Black: 'pretas' };

const ui = {
    connection: document.getElementById('connection'),
    lobby: document.getElementById('lobby'),
    game: document.getElementById('game'),
    create: document.getElementById('create'),
    joinForm: document.getElementById('join-form'),
    matchIdInput: document.getElementById('match-id'),
    board: document.getElementById('board'),
    files: document.getElementById('files'),
    matchLabel: document.getElementById('match-label'),
    myColor: document.getElementById('my-color'),
    turn: document.getElementById('turn'),
    currentPlayer: document.getElementById('current-player'),
    gameState: document.getElementById('game-state'),
    capturedWhite: document.getElementById('captured-white'),
    capturedBlack: document.getElementById('captured-black'),
    log: document.getElementById('log')
};

const state = {
    matchId: null,
    myColor: null,
    board: null,
    selected: null,
    targets: [],
    lastMove: null,
    opponentPresent: false
};

const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/chess')
    .withAutomaticReconnect()
    .build();

// ---- Eventos do Hub ----

connection.on('ReceiveBoardState', result => {
    state.board = result.state;
    state.lastMove = { origin: result.origin, target: result.target };
    clearSelection();
    render();
});

// Único canal de erro: os métodos do Hub resolvem com null e a mensagem chega aqui.
connection.on('ReceiveError', message => {
    clearSelection();
    say(message, 'error');
    render();
});

connection.on('OpponentJoined', color => {
    state.opponentPresent = true;
    say(`Adversário entrou com as ${COLOR_LABEL[color] ?? color}.`);
    render();
});

connection.on('OpponentLeft', () => {
    state.opponentPresent = false;
    say('O adversário saiu da partida.');
    render();
});

connection.onreconnecting(() => setConnectionState('connecting', 'reconectando…'));
connection.onclose(() => setConnectionState('disconnected', 'desconectado'));

// Reconectar gera um connectionId novo, e a cadeira está amarrada ao antigo — o
// servidor já liberou a cor no OnDisconnectedAsync, então é preciso entrar de novo.
connection.onreconnected(async () => {
    setConnectionState('connected', 'conectado');

    if (state.matchId) {
        await enterMatch('JoinMatch', state.matchId);
    }
});

// ---- Ações ----

ui.create.addEventListener('click', () => enterMatch('CreateMatch'));

ui.joinForm.addEventListener('submit', event => {
    event.preventDefault();

    const matchId = ui.matchIdInput.value.trim();

    if (matchId) {
        enterMatch('JoinMatch', matchId);
    }
});

async function enterMatch(method, matchId) {
    const joining = method === 'JoinMatch';
    const match = joining
        ? await connection.invoke(method, matchId)
        : await connection.invoke(method);

    if (!match) {
        return; // o motivo chegou por ReceiveError
    }

    state.matchId = match.matchId;
    state.myColor = match.color;
    state.board = match.state;
    state.opponentPresent = joining;
    state.lastMove = null;
    clearSelection();

    location.hash = match.matchId;
    ui.lobby.hidden = true;
    ui.game.hidden = false;

    say(joining
        ? `Você joga com as ${COLOR_LABEL[match.color]}.`
        : `Partida criada. Compartilhe o link ou o id para o adversário entrar.`);

    render();
}

async function onSquareClick(square) {
    if (!isMyTurn()) {
        return;
    }

    if (state.targets.includes(square)) {
        const origin = state.selected;
        clearSelection();
        render();
        await connection.invoke('MakeMove', state.matchId, origin, square);
        return;
    }

    const piece = pieceAt(square);

    if (piece && piece.color === state.myColor) {
        const moves = await connection.invoke('GetPossibleMoves', state.matchId, square);

        if (moves) {
            state.selected = moves.origin;
            state.targets = moves.targets;
            say('');
        }
    } else {
        clearSelection();
    }

    render();
}

// ---- Render ----

function render() {
    if (!state.board) {
        return;
    }

    renderBoard();
    renderPanel();
}

function renderBoard() {
    const ranks = state.myColor === 'Black' ? [1, 2, 3, 4, 5, 6, 7, 8] : [8, 7, 6, 5, 4, 3, 2, 1];
    const files = state.myColor === 'Black' ? [...FILES].reverse() : FILES;

    ui.board.replaceChildren(...ranks.flatMap(rank => files.map(file => buildSquare(file, rank))));
    ui.files.replaceChildren(...files.map(file => {
        const label = document.createElement('span');
        label.textContent = file;
        return label;
    }));
}

function buildSquare(file, rank) {
    const square = file + rank;
    const piece = pieceAt(square);
    const element = document.createElement('button');

    element.type = 'button';
    element.className = 'square';
    element.dataset.square = square;
    element.setAttribute('aria-label', square);

    // Mesma decisão do GameDisplay: selecionada > possível > paridade da casa.
    if (isLightSquare(file, rank)) {
        element.classList.add('square--light');
    }

    if (state.lastMove && (state.lastMove.origin === square || state.lastMove.target === square)) {
        element.classList.add('square--last');
    }

    if (state.targets.includes(square)) {
        element.classList.add('square--target');
    }

    if (state.selected === square) {
        element.classList.add('square--selected');
    }

    if (piece) {
        element.textContent = glyphOf(piece);
        element.classList.add(`square__piece--${piece.color.toLowerCase()}`);
    }

    const playable = isMyTurn() && (state.targets.includes(square) || (piece && piece.color === state.myColor));

    if (playable) {
        element.classList.add('square--playable');
        element.addEventListener('click', () => onSquareClick(square));
    }

    return element;
}

function renderPanel() {
    const board = state.board;

    ui.matchLabel.textContent = state.matchId;
    ui.myColor.textContent = COLOR_LABEL[state.myColor] ?? '—';
    ui.turn.textContent = board.turn;
    ui.currentPlayer.textContent = COLOR_LABEL[board.currentPlayer] ?? board.currentPlayer;

    ui.capturedWhite.textContent = board.capturedWhitePieces.map(glyphOf).join('') || '—';
    ui.capturedBlack.textContent = board.capturedBlackPieces.map(glyphOf).join('') || '—';

    ui.gameState.textContent = describeState(board);
}

function describeState(board) {
    if (board.completed) {
        return board.winner === state.myColor
            ? `Xeque-mate — você venceu com as ${COLOR_LABEL[board.winner]}.`
            : `Xeque-mate — vitória das ${COLOR_LABEL[board.winner] ?? board.winner}.`;
    }

    if (!state.opponentPresent) {
        return 'Aguardando o adversário entrar…';
    }

    if (board.check) {
        return `Xeque nas ${COLOR_LABEL[board.currentPlayer]}.`;
    }

    return isMyTurn() ? 'Sua vez.' : 'Vez do adversário.';
}

// ---- Auxiliares ----

function isMyTurn() {
    return Boolean(state.board)
        && !state.board.completed
        && state.opponentPresent
        && state.board.currentPlayer === state.myColor;
}

function pieceAt(square) {
    return state.board.pieces.find(piece => piece.position === square) ?? null;
}

function glyphOf(piece) {
    return GLYPHS[piece.color]?.[piece.type] ?? '?';
}

// a8 é casa clara: com row = 8 - rank e col = índice da coluna, (row + col) par é clara,
// exatamente a conta que o GameDisplay faz para escolher o ConsoleColor de fundo.
function isLightSquare(file, rank) {
    return ((8 - rank) + FILES.indexOf(file)) % 2 === 0;
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
    ui.connection.dataset.state = value;
    ui.connection.textContent = label;
    ui.create.disabled = value !== 'connected';
    ui.joinForm.querySelector('button').disabled = value !== 'connected';
}

// ---- Start ----

setConnectionState('connecting', 'conectando…');

if (location.hash.length > 1) {
    ui.matchIdInput.value = decodeURIComponent(location.hash.slice(1));
}

connection.start()
    .then(() => setConnectionState('connected', 'conectado'))
    .catch(error => {
        setConnectionState('disconnected', 'falha ao conectar');
        say(String(error), 'error');
    });
