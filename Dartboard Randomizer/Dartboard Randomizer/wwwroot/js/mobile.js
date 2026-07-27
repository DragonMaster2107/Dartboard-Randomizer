// Shows the game page's sticky "turn HUD" banner once the player list scrolls out of view.
// Uses IntersectionObserver so it works regardless of which element actually scrolls
// (window or an inner container) and on any real device.

export function init() {
    // publish the app bar height so the banner can sit right below it
    const bar = document.querySelector('.mud-appbar');
    const h = bar ? Math.round(bar.getBoundingClientRect().height) : 56;
    document.documentElement.style.setProperty('--appbar-h', h + 'px');
}

export function observe(sentinel, dotNetRef) {
    if (!sentinel) return;

    const bar = document.querySelector('.mud-appbar');
    const barH = bar ? Math.round(bar.getBoundingClientRect().height) : 0;

    // rootMargin pulls the "visible" top edge down to the app bar's bottom, so the banner
    // triggers exactly when the sentinel (right after the player list) hides behind the bar.
    const obs = new IntersectionObserver((entries) => {
        dotNetRef.invokeMethodAsync('OnScrolledPast', !entries[0].isIntersecting);
    }, { rootMargin: `-${barH}px 0px 0px 0px`, threshold: 0 });

    obs.observe(sentinel);
    sentinel._obs = obs;
}

export function unobserve(sentinel) {
    try { sentinel && sentinel._obs && sentinel._obs.disconnect(); } catch { }
}
