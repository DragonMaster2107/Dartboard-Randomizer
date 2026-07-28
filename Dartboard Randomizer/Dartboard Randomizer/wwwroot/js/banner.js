// Keeps the turn banner glued to the top of the VISUAL viewport while pinch-zoomed,
// at constant on-screen size. While zoomed it tracks the viewport every animation frame
// (rAF loop) so it stays smooth instead of lagging behind sparse scroll events.

export function initBannerPin() {
    const vv = window.visualViewport;
    if (!vv) return;

    let el = null;
    let running = false;

    const reset = () => {
        if (!el) return;
        el.style.top = '';
        el.style.width = '';
        el.style.transform = '';
        el.style.transformOrigin = '';
    };

    const frame = () => {
        if (!el || !el.isConnected) el = document.querySelector('.turn-banner');

        if (el && vv.scale > 1.01) {
            // pin to the visible top-left, counter-scale to keep constant on-screen size
            el.style.top = '0px';
            el.style.width = (vv.width * vv.scale) + 'px';
            el.style.transformOrigin = '0 0';
            el.style.transform = `translate3d(${vv.offsetLeft}px, ${vv.offsetTop}px, 0) scale(${1 / vv.scale})`;
            requestAnimationFrame(frame); // keep tracking each frame while zoomed
        } else {
            running = false;
            reset();
        }
    };

    const start = () => {
        if (!running) {
            running = true;
            requestAnimationFrame(frame);
        }
    };

    vv.addEventListener('scroll', start);
    vv.addEventListener('resize', start);
    start();
}
