import { ui } from './dom.js';
import { updateGameSwiper } from './swiper-ui.js';
import {
    FILES,
    ZOOM,
    ZOOM_STORAGE_KEY,
    state,
    glyphOf,
    pieceAt,
    isMyTurn,
    canOfferSideSwap,
    isLightSquare,
    clockSeconds
} from './state.js';

export function buildBoard() {
    const flipped = state.myColor === 'Black';
    const files = flipped ? [...FILES].reverse() : FILES;
    const ranks = flipped
        ? [1, 2, 3, 4, 5, 6, 7, 8]
        : [8, 7, 6, 5, 4, 3, 2, 1];

    ui.board.innerHTML = ranks
        .flatMap(rank => files.map(file => {
            const square = file + rank;
            const light = isLightSquare(file, rank)
                ? ' square--light'
                : '';

            return `<button type="button" class="square${light}" data-square="${square}" aria-label="${square}"></button>`;
        }))
        .join('');

    ui.files.innerHTML = files.map(file => `<span>${file}</span>`).join('');
    ui.ranks.innerHTML = ranks.map(rank => `<span>${rank}</span>`).join('');
}

export function render() {
    if (!state.board) {
        return;
    }

    const myTurn = isMyTurn();

    ui.board.querySelectorAll('.square').forEach(button => {
        const square = button.dataset.square;
        const piece = pieceAt(square);
        const isMine = piece?.color === state.myColor;
        const isTarget = state.targets.includes(square);
        const isCapture = isTarget && Boolean(piece) && !isMine;
        const isLast = state.lastMove?.origin === square
            || state.lastMove?.target === square;

        button.textContent = piece ? glyphOf(piece) : '';
        button.classList.toggle('square--last', isLast);
        button.classList.toggle('square--target', isTarget && !isCapture);
        button.classList.toggle('square--capture', isCapture);
        button.classList.toggle('square--selected', state.selected === square);
        button.classList.toggle(
            'square--playable',
            myTurn && (isTarget || isMine)
        );
        button.classList.toggle('piece--white', piece?.color === 'White');
        button.classList.toggle('piece--black', piece?.color === 'Black');
    });

    renderPanel();
}

function renderPanel() {
    const board = state.board;
    const glyphs = pieces => pieces.map(glyphOf).join('') || '—';

    ui.matchLabel.textContent = state.matchId;
    ui.connectionMatchLabel.textContent = state.matchId;
    ui.myColor.textContent = state.myColor ?? '—';
    ui.whiteClock.textContent = formatClock(clockSeconds('White'));
    ui.blackClock.textContent = formatClock(clockSeconds('Black'));
    ui.whiteClock.classList.toggle(
        'clock--active',
        board.clockRunning && board.currentPlayer === 'White'
    );
    ui.blackClock.classList.toggle(
        'clock--active',
        board.clockRunning && board.currentPlayer === 'Black'
    );
    ui.turn.textContent = board.turn;
    ui.currentPlayer.textContent = board.currentPlayer;
    ui.capturedWhite.textContent = glyphs(board.capturedWhitePieces);
    ui.capturedBlack.textContent = glyphs(board.capturedBlackPieces);
    ui.gameState.innerHTML = describeState(board);
    ui.swapButton.hidden = !canOfferSideSwap();
    renderMoveHistory();
    updateGameSwiper();
}

function renderMoveHistory() {
    const fragment = document.createDocumentFragment();

    for (let index = 0; index < state.moveHistory.length; index += 2) {
        const row = document.createElement('li');
        const number = document.createElement('span');
        const whiteMove = document.createElement('span');
        const blackMove = document.createElement('span');

        row.className = 'move-history__row';
        number.className = 'move-history__number';
        number.textContent = `${Math.floor(index / 2) + 1}.`;
        whiteMove.textContent = state.moveHistory[index];
        blackMove.textContent = state.moveHistory[index + 1] ?? '';
        row.append(number, whiteMove, blackMove);
        fragment.append(row);
    }

    ui.moveHistory.replaceChildren(fragment);
    ui.moveHistory.scrollTop = ui.moveHistory.scrollHeight;
}

function describeState(board) {
    if (board.completed) {
        if (board.timeout) {
            return board.winner === state.myColor
                ? `Time out — you won as ${board.winner}.`
                : `Time out — victory for ${board.winner}.`;
        }

        if (board.draw) {
            return 'Draw.';
        }

        return board.winner === state.myColor
            ? `Checkmate — you won as ${board.winner}.`
            : `Checkmate — victory for ${board.winner}.`;
    }

    if (!state.opponentPresent) {
        return '<span class="spinner"></span> Waiting opponents';
    }

    if (board.check) {
        return `Check on ${board.currentPlayer}.`;
    }

    return isMyTurn() ? 'Your turn.' : "Opponent's turn.";
}

function formatClock(seconds) {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = String(seconds % 60).padStart(2, '0');

    return `${minutes}:${remainingSeconds}`;
}

export function setZoom(value) {
    const rounded = Math.round(value * 10) / 10;

    state.zoom = Math.min(ZOOM.max, Math.max(ZOOM.min, rounded));
    localStorage.setItem(ZOOM_STORAGE_KEY, String(state.zoom));

    ui.boardArea.style.setProperty('--zoom', state.zoom);
    ui.zoomLevel.textContent = `${Math.round(state.zoom * 100)}%`;
    ui.zoomOut.disabled = state.zoom <= ZOOM.min;
    ui.zoomIn.disabled = state.zoom >= ZOOM.max;
}
