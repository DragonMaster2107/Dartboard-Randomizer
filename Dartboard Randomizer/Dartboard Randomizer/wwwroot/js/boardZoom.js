// In-app zoom/pan for the dartboard SVG.
// The page itself never zooms (see viewport meta), so the surrounding UI (turn banner,
// buttons) stays perfectly still. Transforms are written straight to the DOM here —
// no Blazor round-trip — so the gesture stays smooth.
//
// Gestures: two-finger pinch = zoom, one-finger drag (while zoomed) = pan.
// A plain tap is left alone so field clicks still work. No double-tap reset on purpose:
// hitting the same field twice in a row (T20, T20) is normal and would wipe the zoom.

const MIN_SCALE = 1;
const MAX_SCALE = 6;
const DRAG_THRESHOLD = 8; // px before a touch counts as pan instead of a tap

export function attach(container, svg) {
    if (!container || !svg) return null;

    const state = { scale: 1, tx: 0, ty: 0 };
    const pointers = new Map();

    // gesture baseline, re-taken whenever the number of fingers changes
    let baseDist = 0;
    let baseScale = 1;
    let baseMid = { x: 0, y: 0 };
    let baseTx = 0;
    let baseTy = 0;
    let panning = false;

    const apply = () => {
        svg.style.transformOrigin = '0 0';
        svg.style.transform = `translate3d(${state.tx}px, ${state.ty}px, 0) scale(${state.scale})`;
    };

    const clamp = () => {
        if (state.scale <= MIN_SCALE) {
            state.tx = 0;
            state.ty = 0;
            return;
        }
        const w = container.clientWidth;
        const h = container.clientHeight;
        state.tx = Math.min(0, Math.max(w - w * state.scale, state.tx));
        state.ty = Math.min(0, Math.max(h - h * state.scale, state.ty));
    };

    const reset = () => {
        state.scale = 1;
        state.tx = 0;
        state.ty = 0;
        apply();
    };

    const local = (e) => {
        const r = container.getBoundingClientRect();
        return { x: e.clientX - r.left, y: e.clientY - r.top };
    };
    const midOf = (pts) => pts.length === 1
        ? { ...pts[0] }
        : { x: (pts[0].x + pts[1].x) / 2, y: (pts[0].y + pts[1].y) / 2 };
    const distOf = (pts) => pts.length < 2 ? 0 : Math.hypot(pts[0].x - pts[1].x, pts[0].y - pts[1].y);

    // Re-anchor the gesture to the current fingers. Called on every finger add/remove so
    // adding or lifting a finger never causes a jump.
    const rebase = () => {
        const pts = [...pointers.values()].slice(0, 2);
        if (pts.length === 0) return;
        baseMid = midOf(pts);
        baseDist = distOf(pts);
        baseScale = state.scale;
        baseTx = state.tx;
        baseTy = state.ty;
        panning = false;
    };

    const onDown = (e) => {
        // keep receiving events even when the finger leaves the board's bounds
        try { container.setPointerCapture(e.pointerId); } catch { }
        pointers.set(e.pointerId, local(e));
        rebase();
    };

    const onMove = (e) => {
        if (!pointers.has(e.pointerId)) return;
        pointers.set(e.pointerId, local(e));

        const pts = [...pointers.values()].slice(0, 2);
        const curMid = midOf(pts);

        if (pts.length >= 2 && baseDist > 0) {
            // pinch: scale by finger distance, and keep the point that was under the
            // fingers at gesture start under the *current* midpoint (this also pans).
            const next = Math.min(MAX_SCALE, Math.max(MIN_SCALE, baseScale * (distOf(pts) / baseDist)));
            const k = next / baseScale;
            state.scale = next;
            state.tx = curMid.x - (baseMid.x - baseTx) * k;
            state.ty = curMid.y - (baseMid.y - baseTy) * k;
            clamp();
            apply();
            e.preventDefault();
            return;
        }

        // single finger: pan only when zoomed in and moved past the threshold
        if (state.scale > MIN_SCALE) {
            const dx = curMid.x - baseMid.x;
            const dy = curMid.y - baseMid.y;
            if (!panning && Math.hypot(dx, dy) > DRAG_THRESHOLD) panning = true;
            if (panning) {
                state.tx = baseTx + dx;
                state.ty = baseTy + dy;
                clamp();
                apply();
                e.preventDefault();
            }
        }
    };

    const onUp = (e) => {
        try { container.releasePointerCapture(e.pointerId); } catch { }
        pointers.delete(e.pointerId);
        if (pointers.size === 0) {
            baseDist = 0;
            panning = false;
        } else {
            rebase(); // continue smoothly with the remaining finger(s)
        }
    };

    container.addEventListener('pointerdown', onDown);
    container.addEventListener('pointermove', onMove, { passive: false });
    container.addEventListener('pointerup', onUp);
    container.addEventListener('pointercancel', onUp);
    // NOTE: no 'pointerleave' handler — with pointer capture the finger may travel
    // outside the board mid-gesture, and treating that as "up" caused visible jumps.

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
