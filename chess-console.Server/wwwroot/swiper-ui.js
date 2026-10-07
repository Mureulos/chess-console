let gameSwiper;

export function initializeGameSwiper() {
    if (!window.Swiper) {
        return null;
    }

    if (gameSwiper) {
        gameSwiper.update();
        return gameSwiper;
    }

    gameSwiper = new window.Swiper('#game-swiper', {
        autoHeight: false,
        grabCursor: true,
        keyboard: {
            enabled: true
        },
        observer: true,
        observeParents: true,
        watchOverflow: true,
        navigation: {
            nextEl: '#panel-next',
            prevEl: '#panel-prev'
        },
        pagination: {
            el: '.swiper-pagination',
            clickable: true
        },
        a11y: {
            prevSlideMessage: 'Previous panel',
            nextSlideMessage: 'Next panel'
        }
    });

    return gameSwiper;
}

export function updateGameSwiper() {
    gameSwiper?.update();
}
