import { ui, say, setConnectionState } from './dom.js';
import { clearSelection, isMyTurn, pieceAt, state } from './state.js';
import { buildBoard, render } from './render.js';
import { initializeGameSwiper } from './swiper-ui.js';

export const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/chess')
    .withAutomaticReconnect()
    .build();

export async function enterMatch(method, matchId) {
    const joining = method === 'JoinMatch';
    const args = joining ? [matchId] : [];
    const match = await connection.invoke(method, ...args);

    if (!match) {
        return;
    }

    Object.assign(state, {
        matchId: match.matchId,
        myColor: match.color,
        board: match.state,
        opponentPresent: joining,
        lastMove: null,
        moveHistory: match.state.moveHistory ?? []
    });

    clearSelection();

    location.hash = match.matchId;
    ui.lobby.hidden = true;
    ui.game.hidden = false;

    buildBoard();
    render();
    initializeGameSwiper();

    say(joining
        ? `You play as ${match.color}.`
        : 'Match created. Share the link or ID for the opponent to join.');
}

export async function onSquareClick(square) {
    if (!isMyTurn()) {
        return;
    }

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

        const method = promotion
            ? 'MakeMoveWithPromotion'
            : 'MakeMove';

        await connection.invoke(
            method,
            state.matchId,
            origin,
            square,
            ...(promotion ? [promotion] : [])
        );

        return;
    }

    clearSelection();

    if (pieceAt(square)?.color === state.myColor) {
        const moves = await connection.invoke(
            'GetPossibleMoves',
            state.matchId,
            square
        );

        if (moves) {
            state.selected = moves.origin;
            state.targets = moves.targets;
            say('');
        }
    }

    render();
}

function promotionChoice(piece, square) {
    if (piece?.type !== 'Pawn' || !['1', '8'].includes(square.at(-1))) {
        return null;
    }

    const choice = window.prompt(
        'Promote to: Queen (Q), Rook (R), Bishop (B) or Knight (N)',
        'Q'
    );

    if (choice === null) {
        return false;
    }

    return {
        q: 'Queen',
        r: 'Rook',
        b: 'Bishop',
        n: 'Knight'
    }[choice.trim().toLowerCase()] ?? false;
}

export function registerConnectionEvents() {
    connection.onreconnecting(() => {
        setConnectionState('connecting', 'reconnecting...');
    });

    connection.onclose(() => {
        setConnectionState('disconnected', 'disconnected');
    });

    connection.onreconnected(async () => {
        setConnectionState('connected', 'connected');

        if (state.matchId) {
            await enterMatch('JoinMatch', state.matchId);
        }
    });

    connection.on('ReceiveBoardState', result => {
        state.board = result.state;
        state.moveHistory = result.state.moveHistory ?? state.moveHistory;
        state.lastMove = {
            origin: result.origin,
            target: result.target
        };

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
}
