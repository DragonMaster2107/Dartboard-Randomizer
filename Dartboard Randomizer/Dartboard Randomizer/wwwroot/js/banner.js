// Keeps the turn banner glued to the top of the VISUAL viewport while the page is
// pinch-zoomed, at constant on-screen size. When not zoomed it does nothing, so the
// banner keeps its normal CSS position (fixed below the app bar).

export function initBannerPin() {
    const vv = window.visualViewport;
    if (!vv) return;

    const update = () => {
        const el = document.querySelector('.turn-banner');
        if (!el) return;

        if (vv.scale > 1.01) {
            // pin to the visible top-left and counter-scale so size stays constant
            el.style.top = '0px';
            el.style.width = (vv.width * vv.scale) + 'px';
            el.style.transformOrigin = '0 0';
            el.style.transform = `translate(${vv.offsetLeft}px, ${vv.offsetTop}px) scale(${1 / vv.scale})`;
        } else {
            // not zoomed -> revert to the CSS position (below the app bar, full width)
            el.style.top = '';
            el.style.width = '';
            el.style.transform = '';
            el.style.transformOrigin = '';
        }
    };

    vv.addEventListener('scroll', update);
    vv.addEventListener('resize', update);
    update();
}
