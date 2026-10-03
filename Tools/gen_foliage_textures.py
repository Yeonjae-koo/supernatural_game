"""Procedural RGBA foliage cards for the Atchafalaya kit (deterministic, seed 7).

Writes T_SpanishMoss.png (512x1024), T_CypressLeaves.png (1024x1024) and T_CypressTwigs.png (1024x512) to
Assets/_Game/Art/Textures/Generated/ (gitignored; rerun this script to rebuild).
Drawn at 2x and downsampled with LANCZOS for anti-aliased alpha. RGB and alpha are
drawn separately so transparent pixels carry the average colour (no dark fringes).
"""
import math, os
import numpy as np
from PIL import Image, ImageDraw

S = 2
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Game", "Art", "Textures", "Generated")
rng = np.random.default_rng(7)


def canvas(w, h, avg):
    rgb = Image.new("RGB", (w * S, h * S), tuple(int(round(c * 255)) for c in avg))
    a = Image.new("L", (w * S, h * S), 0)
    return rgb, a, ImageDraw.Draw(rgb), ImageDraw.Draw(a)


def dot(drgb, da, x, y, r, col):
    box = [x - r, y - r, x + r, y + r]
    drgb.ellipse(box, fill=col); da.ellipse(box, fill=255)


def stroke(drgb, da, pts, widths, col):
    """Polyline of round dots; pts/widths in final-res px."""
    for (x, y), w in zip(pts, widths):
        dot(drgb, da, x * S, y * S, max(w, 0.5) * S / 2, col)


def finish(rgb, a, w, h, name):
    rgb = rgb.resize((w, h), Image.LANCZOS)
    a = a.resize((w, h), Image.LANCZOS)
    rgb.putalpha(a)
    os.makedirs(OUT, exist_ok=True)
    rgb.save(os.path.join(OUT, name))


def col8(c):
    return tuple(int(round(min(max(v, 0), 1) * 255)) for v in c)


def moss():
    W, H, N = 512, 1024, 60
    strands = []
    for _ in range(N):
        strands.append(dict(
            x=rng.uniform(0, W), y=rng.uniform(0, 0.12 * H), L=rng.uniform(0.45, 0.95) * H,
            amp=rng.uniform(3, 9), wl=rng.uniform(90, 220), ph=rng.uniform(0, 2 * math.pi),
            base=np.array([.45, .48, .38]) + rng.uniform(-.06, .06),
            tufts=[(rng.uniform(.15, .9), rng.choice([-1, 1]) * rng.uniform(25, 55), rng.uniform(12, 32))
                   for _ in range(rng.integers(0, 4))]))
    avg = np.mean([s["base"] * .875 for s in strands], axis=0)
    rgb, a, drgb, da = canvas(W, H, avg)
    for s in strands:
        n = int(s["L"] / 2)
        ts = np.linspace(0, 1, n)
        ys = s["y"] + ts * s["L"]
        xs = s["x"] + s["amp"] * np.sin(2 * math.pi * (ys - s["y"]) / s["wl"] + s["ph"])
        ws = 4 - 3 * ts
        cols = [col8(s["base"] * (1 - .25 * t)) for t in ts]
        for (x, y, w, c) in zip(xs, ys, ws, cols):
            dot(drgb, da, x * S, y * S, w * S / 2, c)
        for t, ang, ln in s["tufts"]:
            i = int(t * (n - 1)); x0, y0 = xs[i], ys[i]
            d = math.radians(ang); m = int(ln)
            pts = [(x0 + k * math.sin(d), y0 + k * math.cos(d)) for k in range(m)]
            stroke(drgb, da, pts, np.linspace(2, .8, m), cols[i])
    finish(rgb, a, W, H, "T_SpanishMoss.png")


def cypress():
    W = H = 1024
    sprigs = []
    while len(sprigs) < 80:
        x0, y0 = rng.normal(512, 220, 2)
        ln = rng.uniform(120, 260); ang = rng.uniform(0, 2 * math.pi); curv = rng.uniform(-.6, .6)
        # keep the whole sprig inside the card
        ex, ey = x0 + ln * math.cos(ang + curv / 2), y0 + ln * math.sin(ang + curv / 2)
        if not (25 < x0 < W - 25 and 25 < y0 < H - 25 and 25 < ex < W - 25 and 25 < ey < H - 25):
            continue
        sprigs.append(dict(x=x0, y=y0, L=ln, a=ang, c=curv,
                           col=np.array([rng.uniform(.16, .28), rng.uniform(.26, .40), rng.uniform(.10, .18)])))
    avg = np.mean([s["col"] for s in sprigs], axis=0)
    rgb, a, drgb, da = canvas(W, H, avg)
    for s in sprigs:
        x, y = s["x"], s["y"]
        steps = int(s["L"])
        stem = []
        for i in range(steps + 1):
            t = i / steps
            th = s["a"] + s["c"] * (t - .5)
            stem.append((x, y, th))
            x += math.cos(th); y += math.sin(th)
        stroke(drgb, da, [(p[0], p[1]) for p in stem], [2.5] * len(stem), col8(s["col"] * .8))
        for i in range(0, len(stem), 5):
            px, py, th = stem[i]
            for side in (-1, 1):
                d = th + side * math.radians(35)
                l = rng.uniform(12, 20)
                c = col8(s["col"] * rng.uniform(.9, 1.1))
                p1 = (px + l * math.cos(d), py + l * math.sin(d))
                drgb.line([px * S, py * S, p1[0] * S, p1[1] * S], fill=c, width=2 * S)
                da.line([px * S, py * S, p1[0] * S, p1[1] * S], fill=255, width=2 * S)
                dot(drgb, da, p1[0] * S, p1[1] * S, S, c)
    finish(rgb, a, W, H, "T_CypressLeaves.png")


def twigs():
    W, H = 1024, 512
    prims = []  # (kind, pts, widths, colour) collected first so the fringe colour is the true average

    def olive():
        return np.array([rng.uniform(.16, .28), rng.uniform(.26, .40), rng.uniform(.10, .18)])

    def branch(x, y, th, ln, w0, depth, maxd, curv, tcol):
        pts = []
        for i in range(int(ln)):
            t = i / ln
            pts.append((x, y, th + curv * (t - .5)))
            x += math.cos(th + curv * (t - .5)); y += math.sin(th + curv * (t - .5))
        ws = [w0 + (1.5 - w0) * i / len(pts) for i in range(len(pts))]
        prims.append(("stem", [(p[0], p[1]) for p in pts], ws, tcol))
        for i in range(0, len(pts), 6):  # needle sprays on both sides
            px, py, a = pts[i]
            for side in (-1, 1):
                d = a + side * math.radians(40); l = rng.uniform(8, 14)
                prims.append(("needle", [(px, py), (px + l * math.cos(d), py + l * math.sin(d))], None, olive() * rng.uniform(.9, 1.1)))
        if depth < maxd:
            for _ in range(int(rng.integers(2, 4))):
                i = int(rng.uniform(.25, .85) * len(pts)); px, py, a = pts[i]
                ca = a + rng.choice([-1, 1]) * math.radians(rng.uniform(25, 50))
                branch(px, py, ca, ln * rng.uniform(.3, .5), max(ws[i] * .8, 2), depth + 1, maxd, rng.uniform(-.3, .3), tcol)

    for k in range(5):
        tcol = np.array([.18, .14, .11]) * rng.uniform(.9, 1.1)
        branch(0, 70 + k * 95 + rng.uniform(-15, 15), rng.uniform(-.12, .12), rng.uniform(780, 900), 6, 0,
               int(rng.integers(2, 4)), rng.uniform(-.25, .25), tcol)
    avg = np.mean([c for *_, c in prims], axis=0)
    rgb, a, drgb, da = canvas(W, H, avg)
    for kind, pts, ws, c in prims:
        c8 = col8(c)
        if kind == "stem":
            stroke(drgb, da, pts, ws, c8)
        else:
            (x0, y0), (x1, y1) = pts
            drgb.line([x0 * S, y0 * S, x1 * S, y1 * S], fill=c8, width=2 * S)
            da.line([x0 * S, y0 * S, x1 * S, y1 * S], fill=255, width=2 * S)
            dot(drgb, da, x1 * S, y1 * S, S, c8)
    finish(rgb, a, W, H, "T_CypressTwigs.png")


if __name__ == "__main__":
    moss(); cypress(); twigs()
