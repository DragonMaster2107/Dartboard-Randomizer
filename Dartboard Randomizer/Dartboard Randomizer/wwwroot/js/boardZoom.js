// In-app zoom/pan for the dartboard SVG.
// The page itself never zooms (see viewport meta), so the surrounding UI (turn banner,
// buttons) stays perfectly still. Transforms are written straight to the DOM here —
// no Blazor round-trip — so the gesture stays smooth.
//
// Gestures: two-finger pinch = zoom, one-finger drag (while zoomed) = pan,
// double-tap = reset. A plain tap is left alone so field clicks still work.

const MIN_SCALE = 1;
const MAX_SCALE = 6;
const DRAG_THRESHOLD = 8; // px before a touch counts as pan instead of a tap

export function attach(container, svg) {
    if (!container || !svg) return null;

    const state = { scale: 1, tx: 0, ty: 0 };
    const pointers = new Map();
    let startDist = 0;
    let startScale = 1;
    let startMid = { x: 0, y: 0 };
    let startTx = 0;
    let startTy = 0;
    let panning = false;

    const apply = () => {
        svg.style.transformOrigin = '0 0';
        svg.style.transform = `translate3d(${state.tx}px, ${state.ty}px, 0) scale(${state.scale})`;
    };

    const clamp = () => {
        // keep the board from being dragged completely out of its frame
        const w = container.clientWidth;
        const h = container.clientHeight;
        const maxX = 0;
        const maxY = 0;
        const minX = w - w * state.scale;
        const minY = h - h * state.scale;
        state.tx = Math.min(maxX, Math.max(minX, state.tx));
        state.ty = Math.min(maxY, Math.max(minY, state.ty));
    };

    const reset = () => {
        state.scale = 1;
        state.tx = 0;
        state.ty = 0;
        apply();
    };

    const mid = (a, b) => ({ x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 });
    const dist = (a, b) => Math.hypot(a.x - b.x, a.y - b.y);
    const local = (e) => {
        const r = container.getBoundingClientRect();
        return { x: e.clientX - r.left, y: e.clientY - r.top };
    };

    const onDown = (e) => {
        pointers.set(e.pointerId, local(e));

        if (pointers.size === 2) {
            const [p1, p2] = [...pointers.values()];
            startDist = dist(p1, p2);
            startScale = state.scale;
            startMid = mid(p1, p2);
            startTx = state.tx;
            startTy = state.ty;
        } else if (pointers.size === 1) {
            // NOTE: deliberately no double-tap-to-reset — hitting the same field twice in
            // a row is normal in darts (T20, T20) and would wipe the zoom. Use the button.
            startMid = local(e);
            startTx = state.tx;
            startTy = state.ty;
            panning = false;
        }
    };

    const onMove = (e) => {
        if (!pointers.has(e.pointerId)) return;
        pointers.set(e.pointerId, local(e));

        if (pointers.size >= 2) {
            const [p1, p2] = [...pointers.values()];
            const d = dist(p1, p2);
            if (startDist > 0) {
                const next = Math.min(MAX_SCALE, Math.max(MIN_SCALE, startScale * (d / startDist)));
                // keep the pinch midpoint anchored under the fingers
                const k = next / startScale;
                state.scale = next;
                state.tx = startMid.x - (startMid.x - startTx) * k;
                state.ty = startMid.y - (startMid.y - startTy) * k;
                clamp();
                apply();
            }
            e.preventDefault();
            return;
        }

        // single pointer: pan only when zoomed in and moved past the threshold
        if (state.scale > 1) {
            const p = local(e);
            const dx = p.x - startMid.x;
            const dy = p.y - startMid.y;
            if (!panning && Math.hypot(dx, dy) > DRAG_THRESHOLD) panning = true;
            if (panning) {
                state.tx = startTx + dx;
                state.ty = startTy + dy;
                clamp();
                apply();
                e.preventDefault();
            }
        }
    };

    const onUp = (e) => {
        pointers.delete(e.pointerId);
        if (pointers.size < 2) startDist = 0;
        if (pointers.size === 0) panning = false;
    };

    container.addEventListener('pointerdown', onDown);
    container.addEventListener('pointermove', onMove, { passive: false });
    container.addEventListener('pointerup', onUp);
    container.addEventListener('pointercancel', onUp);
    container.addEventListener('pointerleave', onUp);

    // desktop: ctrl/⌘ + wheel zooms the board
    const onWheel = (e) => {
        if (!e.ctrlKey && !e.metaKey) return;
        e.preventDefault();
        const p = local(e);
        const next = Math.min(MAX_SCALE, Math.max(MIN_SCALE, state.scale * (e.deltaY < 0 ? 1.1 : 0.9)));
        const k = next / state.scale;
        state.tx = p.x - (p.x - state.tx) * k;
        state.ty = p.y - (p.y - state.ty) * k;
        state.scale = next;
        clamp();
        apply();
    };
    container.addEventListener('wheel', onWheel, { passive: false });

    apply();

    return {
        reset,
        dispose() {
            container.removeEventListener('pointerdown', onDown);
            container.removeEventListener('pointermove', onMove);
            container.removeEventListener('pointerup', onUp);
            container.removeEventListener('pointercancel', onUp);
            container.removeEventListener('pointerleave', onUp);
            container.removeEventListener('wheel', onWheel);
        },
    };
}

export function resetZoom(handle) {
    handle && handle.reset && handle.reset();
}

export function dispose(handle) {
    handle && handle.dispose && handle.dispose();
}
