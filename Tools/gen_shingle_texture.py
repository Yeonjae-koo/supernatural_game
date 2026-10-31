"""Weathered slate-grey 3-tab asphalt shingles (deterministic, seed 13), 1024x1024, tiles in U and V.

UV contract: U 0..1 = 1 m (3 tabs of 1/3), V 0..1 = two courses up-slope, butts at V=0 and V=.5, upper course offset 1/6 U.
Writes T_Shingle.png (sRGB) + T_Shingle_Mask.png (HDRP mask: R 0, G AO, B 0, A smoothness .15) to Generated/ (gitignored).
"""
import os
import numpy as np
from PIL import Image

N = 1024
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Game", "Art", "Textures", "Generated")
rng = np.random.default_rng(13)
f1 = np.fft.fftfreq(N)[:, None]; f2 = np.fft.fftfreq(N)[None, :]


def noise(sx, sy):
    f = np.fft.fft2(rng.standard_normal((N, N))) * np.exp(-((f2 * sx) ** 2 + (f1 * sy) ** 2) * 6)
    n = np.real(np.fft.ifft2(f))
    return (n - n.mean()) / n.std()


def smooth(a, lo, hi):
    t = np.clip((a - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


# image row 0 = V 1; u,v in 0..1 with v up
u = ((np.arange(N) + .5) / N)[None, :]
v = (1 - (np.arange(N) + .5) / N)[:, None]
course = np.floor(v * 2).astype(int)          # 0 lower, 1 upper
cv = (v * 2) % 1                               # 0 at butt, 1 at the top (hidden under next course)
uu = (u - course * (1 / 6)) % 1                # upper course shifted half a tab
tab = np.floor(uu * 3).astype(int) % 3
tid = course * 3 + tab                         # 0..5, per-tab variation
tu = (uu * 3) % 1                              # 0..1 across a tab

vals = rng.standard_normal((6, 3)); light = rng.random(6)
tint = vals[tid]                               # (N,N,3) per-tab colour offset
base = np.array([62, 64, 68]) / 255
rgb = base * (1 + .06 * tint[..., 0:1]) + np.array([.7, 0, -.7]) * .012 * tint[..., 1:2]  # per-tab brightness + brown/blue drift
weathered = (light[tid] > .8)[..., None] * 1.0
rgb = rgb * (1 + .22 * weathered)
gran = noise(1.5, 1.5); gran2 = noise(6, 6)
rgb = rgb * (1 + .09 * np.clip(gran, -2.5, 2.5)[..., None] + .05 * gran2[..., None])
rgb = rgb * (1 + .08 * noise(80, 80)[..., None])                       # broad weathering
bare = smooth(noise(30, 30), 1.9, 2.4)                                  # missing-granule patches: lighter asphalt-brown
rgb = rgb * (1 - bare[..., None]) + np.array([.30, .28, .26]) * bare[..., None] * (1 + .1 * gran[..., None])

# slots: 6 mm of a 1 m tile, ~6 px wide, from the butt up ~60% of the course
slot_d = np.minimum(tu, 1 - tu) * (1 / 3) * N                           # px from tab edge
slot = (slot_d < 3.2) * smooth(.62 - cv, 0, .08)
# shadow band just above the butt line = under the next course: top ~15% of each course
under = smooth(cv, .83, .88)
butt = smooth(.02 - cv, 0, .02) * 0 + (cv < .015) * .5                  # thin dark lip at the butt line
dark = np.clip(1 - .55 * slot - .5 * under - butt, 0, 1)
rgb = np.clip(rgb * dark[..., None], 0, 1)

mask = np.zeros((N, N, 4))
mask[..., 1] = np.clip(1 - .85 * slot - .55 * under - .3 * (cv < .03), 0, 1)
mask[..., 3] = .15 * (1 - .3 * under) + .03 * bare

os.makedirs(OUT, exist_ok=True)
Image.fromarray((rgb * 255 + .5).astype(np.uint8)).save(os.path.join(OUT, "T_Shingle.png"))
Image.fromarray((mask * 255 + .5).astype(np.uint8), "RGBA").save(os.path.join(OUT, "T_Shingle_Mask.png"))
print("mean albedo %s" % np.round(rgb.mean((0, 1)) * 255).astype(int))
