"""Height-weathered white siding for the East Texas kit (deterministic, seed 12).

T_SidingPaint.png (sRGB) + T_SidingPaint_Mask.png (HDRP mask: R metal 0, G AO, B 0, A smoothness), 1024x2048.
UV contract: U 0..1 = 4 m along the wall (tiles seamlessly), V 0..1 = 0..8 m above ground (not tiled; top band V>.95 clean).
Peel coverage falls with height; ground splash grime fades by ~0.8 m. Images gitignored; rerun to rebuild.
"""
import os
from statistics import NormalDist
import numpy as np
from PIL import Image

W, H = 1024, 2048  # 256 px per metre both ways
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Game", "Art", "Textures", "Generated")
rng = np.random.default_rng(12)
fy = np.fft.fftfreq(H)[:, None]; fx = np.fft.fftfreq(W)[None, :]


def noise(sx, sy):
    f = np.fft.fft2(rng.standard_normal((H, W))) * np.exp(-((fx * sx) ** 2 + (fy * sy) ** 2) * 6)
    n = np.real(np.fft.ifft2(f))
    return (n - n.mean()) / n.std()


def smooth(a, lo, hi):
    t = np.clip((a - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


h = ((1 - (np.arange(H) + .5) / H) * 8)[:, None]  # metres above ground per row (row 0 = top of image = V 1)
cov = np.interp(h, [0, .6, 1, 4, 5, 7.5, 7.7, 8], [.25, .25, .10, .10, .03, .03, 0, 0])
thr = np.vectorize(lambda c: NormalDist().inv_cdf(1 - c) if c > 0 else 9.0)(cov)

paint_grain = 1 + .035 * np.clip(noise(120, 6), -2, 2) / 2 + .02 * noise(40, 3) / 2
grime = 1 - .05 * smooth(noise(60, 60), 0, 2) - .02 * noise(20, 20) / 2
paint = np.array([214, 208, 192]) / 255 * (paint_grain * grime)[..., None]

field = noise(70, 14) + .5 * noise(25, 6)
field = (field - field.mean()) / field.std()
peel = smooth(field, thr - .03, thr + .03)
rim = smooth(field, thr - .35, thr - .03) * (1 - peel)
wood = np.array([118, 110, 98]) / 255 * (1 + .12 * np.clip(noise(150, 3), -2, 2) / 2 + .06 * noise(30, 4) / 2)[..., None]

rgb = paint * (1 - .14 * rim)[..., None] * (1 - rim[..., None] * np.array([0, .03, .10]))
rgb = rgb * (1 - peel[..., None]) + wood * peel[..., None]
# ground splash: grey-brown grime, strongest at the foot, ragged top edge, gone by 0.8 m
splash = smooth(1 - h / .8 + .25 * noise(40, 10), 0, 1) * (.6 + .4 * smooth(noise(50, 20), -1, 1))
rgb = rgb * (1 - .55 * splash[..., None]) + np.array([.28, .25, .21]) * .55 * splash[..., None] * rgb.mean(-1, keepdims=True) * 1.6
rgb = rgb * (1 - .6 * smooth(noise(2, 2), 2.6, 3.2)[..., None])
rgb = np.clip(rgb, 0, 1)

mask = np.zeros((H, W, 4))
mask[..., 1] = 1 - .15 * peel - .1 * splash
mask[..., 3] = .35 * (1 - peel) + .12 * peel

os.makedirs(OUT, exist_ok=True)
Image.fromarray((rgb * 255 + .5).astype(np.uint8)).save(os.path.join(OUT, "T_SidingPaint.png"))
Image.fromarray((mask * 255 + .5).astype(np.uint8), "RGBA").save(os.path.join(OUT, "T_SidingPaint_Mask.png"))

print("mean albedo %s" % np.round(rgb.mean((0, 1)) * 255).astype(int))
for lo, hi in [(0, .6), (.8, 1), (1, 4), (5, 7.5), (7.6, 8)]:
    m = (h[:, 0] >= lo) & (h[:, 0] < hi)
    print("%.1f-%.1f m: peel %.1f%%" % (lo, hi, peel[m].mean() * 100))
edge = np.abs(rgb[:, 0] - rgb[:, -1]).mean(); inner = np.abs(rgb[:, 0] - rgb[:, 1]).mean()
print("U wrap diff %.5f vs interior neighbour diff %.5f" % (edge, inner))
