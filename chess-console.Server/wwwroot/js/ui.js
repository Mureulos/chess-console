import { ui, say } from './dom.js';
import { state, ZOOM } from './state.js';

const WAITING_TIME_SECONDS = 2 * 60;
let waitingTimer;

export function registerUiEvents({
    enterMatch,
    searchMatches,
    leaveMatch,
    onSquareClick,
    setZoom,
    offerSideSwap,
    respondToSideSwap
}) {
    ui.create.addEventListener('click', () => {
        ui.matchIdInput.value = '';
        enterMatch('CreateMatch');
    });

    ui.searchMatches.addEventListener('click', async () => {
        const matches = await searchMatches();
        openSearchDialog(matches, matchId => enterMatch('JoinMatch', matchId));
    });

    ui.joinForm.addEventListener('submit', event => {
        event.preventDefault();

        const matchId = ui.matchIdInput.value.trim();

        if (matchId) {
            ui.matchIdInput.value = '';
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

    ui.swapButton.addEventListener('click', () => {
        openSwapDialog(
            'Offer a side swap to your opponent before the first move?',
            'offer');
    });

    ui.swapCancel.addEventListener('click', () => ui.swapDialog.close());

    ui.swapConfirm.addEventListener('click', () => {
        ui.swapDialog.close();
        offerSideSwap();
    });

    ui.swapAccept.addEventListener('click', () => {
        ui.swapDialog.close();
        respondToSideSwap(true);
    });

    ui.swapDecline.addEventListener('click', () => {
        ui.swapDialog.close();
        respondToSideSwap(false);
    });

    ui.leaveMatch.addEventListener('click', leaveMatch);
    ui.searchClose.addEventListener('click', () => ui.searchDialog.close());
}

export function openSearchDialog(matchIds, onJoin) {
    ui.searchResults.replaceChildren();
    ui.searchEmpty.hidden = matchIds.length > 0;

    for (const matchId of matchIds) {
        const item = document.createElement('li');
        const label = document.createElement('code');
        const joinButton = document.createElement('button');

        label.textContent = matchId;
        joinButton.type = 'button';
        joinButton.textContent = 'Join';
        joinButton.addEventListener('click', () => {
            ui.searchDialog.close();
            onJoin(matchId);
        });
        item.append(label, joinButton);
        ui.searchResults.append(item);
    }

    ui.searchDialog.showModal();
}

export function openSwapDialog(message, mode) {
    ui.swapDialogTitle.textContent = mode === 'offer'
        ? 'Swap sides'
        : 'Side swap request';
    ui.swapDialogMessage.textContent = message;
    ui.swapConfirm.hidden = mode !== 'offer';
    ui.swapAccept.hidden = mode !== 'response';
    ui.swapDecline.hidden = mode !== 'response';
    ui.swapCancel.hidden = mode === 'response';
    ui.swapDialog.showModal();
}

export function openWaitingDialog(onTimeout) {
    clearWaitingTimer();
    updateWaitingCountdown(WAITING_TIME_SECONDS);
    ui.waitingDialog.showModal();

    let remaining = WAITING_TIME_SECONDS;
    waitingTimer = setInterval(() => {
        remaining -= 1;
        updateWaitingCountdown(remaining);

        if (remaining === 0) {
            clearWaitingTimer();
            onTimeout();
        }
    }, 1000);
}

export function closeWaitingDialog() {
    clearWaitingTimer();

    if (ui.waitingDialog.open) {
        ui.waitingDialog.close();
    }
}

function clearWaitingTimer() {
    if (waitingTimer) {
        clearInterval(waitingTimer);
        waitingTimer = undefined;
    }
}

function updateWaitingCountdown(seconds) {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = String(seconds % 60).padStart(2, '0');
    ui.waitingCountdown.textContent = `${minutes}:${remainingSeconds}`;
}
