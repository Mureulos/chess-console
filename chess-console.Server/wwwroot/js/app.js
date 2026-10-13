'use strict';
import { ui, say, setConnectionState } from './dom.js';
import { state } from './state.js';
import { render, setZoom } from './render.js';
import {
    connection,
    enterMatch,
    searchMatches,
    leaveMatch,
    onSquareClick,
    registerConnectionEvents,
    offerSideSwap,
    respondToSideSwap
} from './connection.js';
import { registerUiEvents } from './ui.js';

registerConnectionEvents();

registerUiEvents({
    enterMatch,
    searchMatches,
    leaveMatch,
    onSquareClick,
    setZoom,
    offerSideSwap,
    respondToSideSwap
});

setZoom(state.zoom);
setInterval(() => {
    if (state.board) {
        render();
    }
}, 250);

if (location.hash.length > 1) {
    ui.matchIdInput.value = decodeURIComponent(location.hash.slice(1));
}

async function startConnection() {
    setConnectionState('connecting', 'connecting...');

    try {
        await connection.start();
        setConnectionState('connected', `connected as ${state.playerName}`);

        if (location.hash.length > 1) {
            await enterMatch('JoinMatch', decodeURIComponent(location.hash.slice(1)));
        }
    } catch (error) {
        setConnectionState('disconnected', 'failed to connect');
        say(String(error), 'error');
    }
}

ui.playerForm.addEventListener('submit', async event => {
    event.preventDefault();
    const name = ui.playerName.value.trim();

    if (!ui.playerForm.reportValidity())
        return;

    try {
        const response = await fetch('/api/player/session', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name })
        });
        const session = await response.json();

        if (!response.ok)
            throw new Error(session.error ?? 'Could not create player session.');

        state.playerToken = session.token;
        state.playerName = session.name;
        state.playerExpiresAt = session.expiresAtUtc;
        localStorage.setItem('chess-console.player-name', session.name);
        sessionStorage.setItem('chess-console.player-token', session.token);
        sessionStorage.setItem('chess-console.player-expires-at', session.expiresAtUtc);
        await startConnection();
    } catch (error) {
        setConnectionState('disconnected', 'failed to connect');
        say(String(error), 'error');
    }
});

ui.playerName.value = localStorage.getItem('chess-console.player-name') ?? '';
localStorage.removeItem('chess-console.player-token');
localStorage.removeItem('chess-console.player-expires-at');

const savedToken = sessionStorage.getItem('chess-console.player-token');
const savedExpiresAt = sessionStorage.getItem('chess-console.player-expires-at');

if (savedToken && savedExpiresAt && new Date(savedExpiresAt) > new Date()) {
    state.playerToken = savedToken;
    state.playerName = ui.playerName.value;
    state.playerExpiresAt = savedExpiresAt;
    startConnection();
} else {
    sessionStorage.removeItem('chess-console.player-token');
    sessionStorage.removeItem('chess-console.player-expires-at');
    setConnectionState('disconnected', 'enter your name');
}
