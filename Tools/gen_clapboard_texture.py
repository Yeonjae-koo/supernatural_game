"""Seamless weathered white clapboard paint for the East Texas kit (deterministic, seed 11).

Writes T_ClapboardPaint.png (sRGB albedo) and T_ClapboardPaint_Mask.png (HDRP mask: R metal 0, G AO, B detail 0,
A smoothness) at 1024x1024 to Assets/_Game/Art/Textures/Generated/ (gitignored; rerun to rebuild).
All noise is FFT-filtered white noise, so it is periodic and tiles in both axes.
"""
import os
import numpy as np
from PIL import Image

N = 1024
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Game", "Art", "Textures", "Generated")
rng = np.random.default_rng(11)
fy = np.fft.fftfreq(N)[:, None]; fx = np.fft.fftfreq(N)[None, :]


def noise(sx, sy):
    """Periodic gaussian noise, correlation length ~sx px along U and ~sy along V; normalised to mean 0, std 1."""
    f = np.fft.fft2(rng.standard_normal((N, N))) * np.exp(-((fx * sx) ** 2 + (fy * sy) ** 2) * 6)
    n = np.real(np.fft.ifft2(f))
    return (n - n.mean()) / n.std()


def smooth(a, lo, hi):
    t = np.clip((a - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


grain = noise(120, 6)
paint_grain = 1 + .035 * np.clip(grain, -2, 2) / 2 + .02 * noise(40, 3) / 2
grime = 1 - .05 * smooth(noise(60, 60), 0, 2) - .02 * noise(20, 20) / 2
paint = np.array([214, 208, 192]) / 255 * (paint_grain * grime)[..., None]

field = noise(70, 14) + .5 * noise(25, 6)
field = (field - field.mean()) / field.std()
thr = np.quantile(field, 1 - .12)
peel = smooth(field, thr - .03, thr + .03)
rim = smooth(field, thr - .35, thr - .03) * (1 - peel)
wood = np.array([118, 110, 98]) / 255 * (1 + .12 * np.clip(noise(150, 3), -2, 2) / 2 + .06 * noise(30, 4) / 2)[..., None]

rgb = paint * (1 - .14 * rim)[..., None] * (1 - rim[..., None] * np.array([0, .03, .10]))
rgb = rgb * (1 - peel[..., None]) + wood * peel[..., None]
speck = smooth(noise(2, 2), 2.6, 3.2)
rgb = rgb * (1 - .6 * speck[..., None])
rgb = np.clip(rgb, 0, 1)

mask = np.zeros((N, N, 4))
mask[..., 1] = 1 - .15 * peel
mask[..., 3] = .35 * (1 - peel) + .12 * peel

os.makedirs(OUT, exist_ok=True)
Image.fromarray((rgb * 255 + .5).astype(np.uint8)).save(os.path.join(OUT, "T_ClapboardPaint.png"))
Image.fromarray((mask * 255 + .5).astype(np.uint8), "RGBA").save(os.path.join(OUT, "T_ClapboardPaint_Mask.png"))

edge = (np.abs(rgb[0] - rgb[-1]).mean() + np.abs(rgb[:, 0] - rgb[:, -1]).mean()) / 2
inner = (np.abs(rgb[0] - rgb[1]).mean() + np.abs(rgb[:, 0] - rgb[:, 1]).mean()) / 2
print("mean albedo (sRGB 0-1): %.3f (%s), peel coverage %.1f%%" % (rgb.mean(), np.round(rgb.mean((0, 1)) * 255).astype(int), peel.mean() * 100))
print("wrap diff %.5f vs interior neighbour diff %.5f" % (edge, inner))
