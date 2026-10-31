"""Whitewashed painted concrete-block (CMU) wall for the East Texas kit (deterministic, seed 14).

Writes T_BlockPaint.png (sRGB), T_BlockPaint_Mask.png (HDRP mask: R 0, G AO, B 0, A smoothness) and T_BlockPaint_Normal.png (tangent normal, GL)
at 1920x1920 to Assets/_Game/Art/Textures/Generated/ (gitignored; rerun to rebuild).
World-space triplanar contract: ONE repeat = 4.8 m (400 px per metre) in U and V. U tiles seamlessly (12 blocks of 0.4 m, running bond, courses 0.2 m,
mortar joints 1 cm). V is height above ground in a 4.8 m column that is NOT tiled (import wrapV = Clamp): peel and splash grime concentrate in the
first ~1 m, rain streaks gather below the parapet line (V .75..1), rows above 4.8 m clamp to the clean top row.
"""
import os
from statistics import NormalDist
import numpy as np
from PIL import Image

N = 1920
PPM = N / 4.8  # 400 px per metre
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Game", "Art", "Textures", "Generated")
rng = np.random.default_rng(14)
fy = np.fft.fftfreq(N)[:, None]; fx = np.fft.fftfreq(N)[None, :]


def noise(sx, sy):
    """Periodic gaussian noise, correlation length ~sx px along U and ~sy along V; mean 0, std 1."""
    f = np.fft.fft2(rng.standard_normal((N, N))) * np.exp(-((fx * sx) ** 2 + (fy * sy) ** 2) * 6)
    n = np.real(np.fft.ifft2(f))
    return (n - n.mean()) / n.std()


def smooth(a, lo, hi):
    t = np.clip((a - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


u = ((np.arange(N) + .5) / PPM)[None, :]            # metres along the wall
h = ((N - 1 - np.arange(N) + .5) / PPM)[:, None]    # metres above ground (row 0 = top = 4.8 m)

# ---- block grid (running bond)
course = np.floor(h / .2)
uo = u + .2 * (course % 2)
dh = np.minimum(h % .2, .2 - h % .2)
dv = np.minimum(uo % .4, .4 - uo % .4)
d = np.minimum(dh, dv)                                  # metres to the nearest mortar-joint centre line
joint = 1 - smooth(d, .004, .009)                       # 1 in the joint (about 1 cm wide), soft edge
bid = (np.floor(uo / .4) % 12 + 12 * course).astype(int)
tone = rng.normal(0, 1, 12 * 24 + 1)[bid]

# ---- painted face
paint_grain = 1 + .03 * np.clip(noise(3, 3), -2, 2) / 2 + .025 * noise(30, 30) / 2
face = np.array([218, 214, 202]) / 255 * ((1 + .018 * tone) * paint_grain * (1 - .03 * smooth(noise(80, 80), 0, 2)))[..., None]
pits = smooth(noise(1.6, 1.6), 2.4, 3.1)                # tiny air-hole pits in the paint
face = face * (1 - .35 * pits[..., None])

# ---- peel: unpainted CMU shows, mostly near the ground
cov = np.interp(h, [0, .5, 1, 2, 3, 4.2, 4.8], [.34, .30, .14, .06, .035, .02, 0])
thr = np.vectorize(lambda c: NormalDist().inv_cdf(1 - c) if c > 0 else 9.0)(cov)
field = noise(60, 22) + .6 * noise(18, 9)
field = (field - field.mean()) / field.std()
peel = smooth(field, thr - .03, thr + .03)
rim = smooth(field, thr - .35, thr - .03) * (1 - peel)
cmu = np.array([172, 168, 158]) / 255 * (1 + .09 * noise(5, 5) / 2 + .05 * noise(40, 40) / 2 + .02 * tone)[..., None]
mortar_bare = np.array([184, 180, 170]) / 255 * (1 + .07 * noise(4, 4) / 2)[..., None]
bare = cmu * (1 - joint[..., None]) + mortar_bare * joint[..., None]
rgb = face * (1 - .12 * rim)[..., None] * (1 - rim[..., None] * np.array([0, .03, .10]))
rgb = rgb * (1 - peel[..., None]) + bare * peel[..., None]

# ---- grime: dirt packs the (recessed) joints, ground splash, rain streaks under the parapet
rgb = rgb * (1 - .18 * joint * (1 - peel))[..., None]
splash = smooth(1 - h / .9 + .3 * noise(40, 10), 0, 1) * (.6 + .4 * smooth(noise(50, 20), -1, 1))
rgb = rgb * (1 - .5 * splash[..., None]) + np.array([.30, .26, .21]) * .5 * splash[..., None] * rgb.mean(-1, keepdims=True) * 1.6
streak = smooth(noise(5, 260), .2, 2.2) * (.5 + .5 * smooth(noise(70, 30), -1, 1))
rain = streak * smooth(h, 1.2, 4.3) * (1 - smooth(h, 4.6, 4.8))
rgb = rgb * (1 - .22 * rain[..., None]) * (1 - .04 * rain[..., None] * np.array([0, 0, 1]))
rgb = np.clip(rgb, 0, 1)

# ---- height (metres) -> tangent normal; joints recessed 4 mm, peeled paint 0.5 mm lower, pits 0.3 mm
ht = -.004 * joint - .0005 * peel - .0003 * pits + .0002 * noise(3, 3)
# periodic gradient in U; V is clamped, so one-sided at the ends
du = (np.roll(ht, -1, 1) - np.roll(ht, 1, 1)) / (2 / PPM)
dvv = np.gradient(ht[::-1], 1 / PPM, axis=0)[::-1]       # d/d(height-up)
nrm = np.stack([-du, -dvv, np.ones_like(du)], -1)
nrm /= np.linalg.norm(nrm, axis=-1, keepdims=True)

mask = np.zeros((N, N, 4))
mask[..., 1] = np.clip(1 - .55 * joint - .1 * peel - .1 * splash, 0, 1)
mask[..., 3] = .35 * (1 - peel) + .12 * peel

os.makedirs(OUT, exist_ok=True)
Image.fromarray((rgb * 255 + .5).astype(np.uint8)).save(os.path.join(OUT, "T_BlockPaint.png"))
Image.fromarray((mask * 255 + .5).astype(np.uint8), "RGBA").save(os.path.join(OUT, "T_BlockPaint_Mask.png"))
Image.fromarray(((nrm * .5 + .5) * 255 + .5).astype(np.uint8)).save(os.path.join(OUT, "T_BlockPaint_Normal.png"))

print("mean albedo %s" % np.round(rgb.mean((0, 1)) * 255).astype(int))
for lo, hi in [(0, .5), (.5, 1), (1, 3), (3, 4.8)]:
    m = (h[:, 0] >= lo) & (h[:, 0] < hi)
    print("%.1f-%.1f m: peel %.1f%%" % (lo, hi, peel[m].mean() * 100))
edge = np.abs(rgb[:, 0] - rgb[:, -1]).mean(); inner = np.abs(rgb[:, 0] - rgb[:, 1]).mean()
print("U wrap diff %.5f vs interior neighbour diff %.5f" % (edge, inner))
