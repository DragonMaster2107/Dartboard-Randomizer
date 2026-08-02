// Setzt das Turn-Banner NACH einer Zoom-/Pan-Geste einmal an den sichtbaren oberen Rand
// (bei konstanter Bildschirmgröße).
//
// Bewusst KEINE rAF-Nachführung pro Frame: Pinch und Pan laufen auf dem Compositor-Thread,
// jedes Nachziehen aus dem Main-Thread hinkt strukturell mindestens einen Frame hinterher
// -> das Banner "schwimmt" beim schnellen Wischen und an den Kanten blitzt Seiteninhalt auf.
// Während der Geste bleibt das Banner daher unangetastet: es klebt nativ am Layout-Viewport
// und fährt völlig flüssig mit dem Inhalt mit. Erst wenn die Bewegung zur Ruhe kommt,
// schnappt es an seinen Platz.

// Wie lange keine Viewport-Änderung kommen muss, damit die Geste als beendet gilt.
const SettleMs = 140;

export function initBannerPin() {
    const vv = window.visualViewport;
    if (!vv) return;

    let el = null;
    let timer = 0;

    const banner = () => {
        if (!el || !el.isConnected)
            el = document.querySelector('.turn-banner');
        return el;
    };

    const settle = () => {
        timer = 0;

        const b = banner();
        if (!b) return;

        if (vv.scale > 1.01) {
            // an den sichtbaren oberen Rand pinnen, per Gegen-Skalierung in Originalgröße
            b.style.top = '0px';
            b.style.width = (vv.width * vv.scale) + 'px';
            b.style.transformOrigin = '0 0';
            b.style.transform =
                `translate3d(${vv.offsetLeft}px, ${vv.offsetTop}px, 0) scale(${1 / vv.scale})`;
        } else {
            // nicht gezoomt -> zurück auf die reine CSS-Positionierung
            b.style.top = '';
            b.style.width = '';
            b.style.transform = '';
            b.style.transformOrigin = '';
        }
    };

    // Jede Viewport-Änderung verschiebt den Zeitpunkt nach hinten; gesetzt wird erst,
    // wenn SettleMs lang Ruhe war.
    const schedule = () => {
        if (timer) clearTimeout(timer);
        timer = setTimeout(settle, SettleMs);
    };

    vv.addEventListener('scroll', schedule);
    vv.addEventListener('resize', schedule);
    settle();
}
