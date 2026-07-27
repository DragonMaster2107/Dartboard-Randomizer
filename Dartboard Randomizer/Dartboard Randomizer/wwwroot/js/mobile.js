// Mobile-only helper: detects small screens and observes a sentinel element so the
// game page can show a sticky "turn HUD" banner once the player list scrolls out of view.

export function init() {
    // publish the app bar height so the banner can sit right below it
    const bar = document.querySelector('.mud-appbar');
    const h = bar ? Math.round(bar.getBoundingClientRect().height) : 56;
    document.documentElement.style.setProperty('--appbar-h', h + 'px');

    // "mobile" = narrow screen (single-column layout). No fragile user-agent sniffing.
    return window.matchMedia('(max-width: 600px)').matches;
}

export function observe(sentinel, dotNetRef) {
    if (!sentinel) return;

    let last = null;
    const check = () => {
        const bar = document.querySelector('.mud-appbar');
        const threshold = bar ? bar.getBoundingClientRect().height : 0;
        // banner shows once the sentinel (right after the player list) goes behind the app bar
        const past = sentinel.getBoundingClientRect().top < threshold;
        if (past !== last) {
            last = past;
            dotNetRef.invokeMethodAsync('OnScrolledPast', past);
        }
    };

    const onScroll = () => requestAnimationFrame(check);
    window.addEventListener('scroll', onScroll, { passive: true });
    window.visualViewport?.addEventListener('scroll', onScroll, { passive: true });
    window.visualViewport?.addEventListener('resize', onScroll, { passive: true });
    sentinel._onScroll = onScroll;

    check(); // initial state
}

export function unobserve(sentinel) {
    const fn = sentinel && sentinel._onScroll;
    if (!fn) return;
    window.removeEventListener('scroll', fn);
    window.visualViewport?.removeEventListener('scroll', fn);
    window.visualViewport?.removeEventListener('resize', fn);
}
