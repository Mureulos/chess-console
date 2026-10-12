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

setConnectionState('connecting', 'connecting...');
setZoom(state.zoom);
setInterval(() => {
    if (state.board) {
        render();
    }
}, 250);

if (location.hash.length > 1) {
    ui.matchIdInput.value = decodeURIComponent(location.hash.slice(1));
}

connection.start()
    .then(async () => {
        setConnectionState('connected', 'connected');

        if (location.hash.length > 1) {
            await enterMatch('JoinMatch', decodeURIComponent(location.hash.slice(1)));
        }
    })
    .catch(error => {
        setConnectionState('disconnected', 'failed to connect');
        say(String(error), 'error');
    });
