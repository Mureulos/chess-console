'use strict';
import { ui, say, setConnectionState } from './dom.js';
import { state } from './state.js';
import { setZoom } from './render.js';
import {
    connection,
    enterMatch,
    onSquareClick,
    registerConnectionEvents,
    offerSideSwap,
    respondToSideSwap
} from './connection.js';
import { registerUiEvents } from './ui.js';

registerConnectionEvents();

registerUiEvents({
    enterMatch,
    onSquareClick,
    setZoom,
    offerSideSwap,
    respondToSideSwap
});

setConnectionState('connecting', 'connecting...');
setZoom(state.zoom);

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
