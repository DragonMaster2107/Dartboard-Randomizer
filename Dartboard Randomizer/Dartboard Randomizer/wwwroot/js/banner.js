// Hält das Turn-Banner beim Pinch-Zoom am sichtbaren oberen Rand, in konstanter
// Bildschirmgröße. Solange gezoomt ist, wird die Position pro Frame (rAF) nachgeführt.
//
// Pinch und Pan laufen auf dem Compositor-Thread, das Nachführen im Main-Thread — beim
// schnellen Wischen hinkt das Banner deshalb rund einen Frame hinterher. Damit dabei an
// den Kanten keine Lücke aufblitzt, ragt es oben/links/rechts um `--tb-over` über den
// sichtbaren Bereich hinaus (Klasse `tb-pinned`, siehe GameBoard.razor.css). Im Normalfall
// liegt dieser Überstand außerhalb des Bildschirms und ist unsichtbar; er wird nur in dem
// Moment sichtbar, in dem das Banner der Bewegung nachhinkt, und füllt dann die Lücke.

export function initBannerPin() {
    const vv = window.visualViewport;
    if (!vv) return;

    let el = null;
    let over = 0;      // Überstand in Bildschirm-Pixeln (aus --tb-over)
    let running = false;

    const banner = () => {
        if (!el || !el.isConnected) {
            el = document.querySelector('.turn-banner');
            over = el
                ? parseFloat(getComputedStyle(el).getPropertyValue('--tb-over')) || 0
                : 0;
        }
        return el;
    };

    const unpin = (b) => {
        b.classList.remove('tb-pinned');
        b.style.top = '';
        b.style.width = '';
        b.style.transform = '';
        b.style.transformOrigin = '';
    };

    const frame = () => {
        const b = banner();

        if (b && vv.scale > 1.01) {
            const s = vv.scale;

            // Das Element wird mit 1/s gegenskaliert, dadurch entspricht 1 CSS-Pixel des
            // Elements genau 1 Bildschirm-Pixel. `translate` wirkt dagegen noch im
            // Layout-Koordinatensystem, der Überstand muss dort also durch s geteilt werden.
            const o = over / s;

            b.classList.add('tb-pinned');
            b.style.top = '0px';
            b.style.width = (vv.width * s + 2 * over) + 'px';
            b.style.transformOrigin = '0 0';
            b.style.transform =
                `translate3d(${vv.offsetLeft - o}px, ${vv.offsetTop - o}px, 0) scale(${1 / s})`;

            requestAnimationFrame(frame); // weiter tracken, solange gezoomt
        } else {
            running = false;
            if (b) unpin(b);
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
