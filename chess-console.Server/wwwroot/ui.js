import { ui, say } from './dom.js';
import { state, ZOOM } from './state.js';

export function registerUiEvents({ enterMatch, onSquareClick, setZoom }) {
    ui.create.addEventListener('click', () => {
        enterMatch('CreateMatch');
    });

    ui.joinForm.addEventListener('submit', event => {
        event.preventDefault();

        const matchId = ui.matchIdInput.value.trim();

        if (matchId) {
            enterMatch('JoinMatch', matchId);
        }
    });

    ui.board.addEventListener('click', event => {
        const square = event.target.closest('.square--playable');

        if (square) {
            onSquareClick(square.dataset.square);
        }
    });

    ui.zoomIn.addEventListener('click', () => {
        setZoom(state.zoom + ZOOM.step);
    });

    ui.zoomOut.addEventListener('click', () => {
        setZoom(state.zoom - ZOOM.step);
    });

    ui.shareLink.addEventListener('click', async () => {
        try {
            await navigator.clipboard.writeText(location.href);

            const original = ui.shareLink.textContent;
            ui.shareLink.textContent = 'Copied!';

            setTimeout(() => {
                ui.shareLink.textContent = original;
            }, 2000);
        } catch {
            say('Failed to copy link.', 'error');
        }
    });
}
