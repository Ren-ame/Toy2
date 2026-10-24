# Terrain 레이어용 텍스처 4종 (큰 얼룩 없이 균일하게 → 섞는 건 Terrain 스플랫맵이 담당)
import numpy as np, sys
from PIL import Image
N = 1024; OUT = sys.argv[1]; rng = np.random.default_rng(21)
F2 = np.fft.fftfreq(N)[:, None] ** 2 + np.fft.fftfreq(N)[None, :] ** 2; F2[0, 0] = 1
def noise(p):
    s = (rng.normal(size=(N, N)) + 1j * rng.normal(size=(N, N))) / F2 ** (p / 2); s[0, 0] = 0
    n = np.real(np.fft.ifft2(s)); return (n - n.min()) / (n.max() - n.min())
def sstep(a, b, x): t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)
def blur(h, r): g = np.exp(-F2 * (np.pi * r) ** 2 * 2); return np.real(np.fft.ifft2(np.fft.fft2(h) * g))
y, x = np.mgrid[0:N, 0:N] / N
def save(name, col, h, strength):
    Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8)).save(f"{OUT}/Terrain_{name}_BaseColor.png")
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength; dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    n = np.stack([-dx, dy, np.ones_like(h)], -1); n /= np.linalg.norm(n, axis=-1, keepdims=True)
    Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8)).save(f"{OUT}/Terrain_{name}_Normal.png")
def splat(n, rmin, rmax, aspect):
    hh = np.zeros((N, N)); ids = np.full((N, N), -1)
    for k in range(n):
        cx, cy = rng.random(2); r = rng.uniform(rmin, rmax); ang = rng.uniform(0, np.pi); ca, sa = np.cos(ang), np.sin(ang)
        ddx = (x - cx + 0.5) % 1 - 0.5; ddy = (y - cy + 0.5) % 1 - 0.5
        u, v = (ddx * ca + ddy * sa) / r, (-ddx * sa + ddy * ca) / (r * aspect)
        d2 = u * u + v * v; dome = np.sqrt(np.clip(1 - d2, 0, 1)); upd = (d2 < 1) & (dome > hh)
        hh[upd] = dome[upd]; ids[upd] = k
    return hh, ids
mid, fine, blade, clump = noise(1.4), noise(0.9), noise(0.3), noise(1.1)

# 잔디
g = np.array([0.24, 0.31, 0.14]) * (0.75 + 0.35 * blade[..., None]) * (0.88 + 0.22 * clump[..., None]) * (0.93 + 0.1 * mid[..., None])
save("Grass", g * 1.1, 0.5 * blade + 0.3 * clump, 3.0)
# 마른 풀
d = np.array([0.40, 0.36, 0.20]) * (0.72 + 0.38 * blade[..., None]) * (0.88 + 0.22 * clump[..., None])
d = d * (1 - 0.25 * sstep(0.5, 0.8, mid)[..., None]) + np.array([0.24, 0.29, 0.14]) * 0.25 * sstep(0.5, 0.8, mid)[..., None]
save("DryGrass", d * 1.1, 0.5 * blade + 0.3 * clump, 3.0)
# 흙 + 자갈
grain = noise(0.2)
soil = np.array([0.31, 0.25, 0.19]) * (0.8 + 0.25 * fine[..., None]) * (0.9 + 0.2 * grain[..., None]) * (0.92 + 0.12 * mid[..., None])
ph, pid = splat(110, 0.004, 0.011, 0.75)
pm = ph > 0; tone = rng.uniform(0.30, 0.48, 200)[np.maximum(pid, 0)]
shadow = np.clip(blur(pm.astype(float), 0.004) * 1.6, 0, 1) * (1 - pm)
soil = soil * (1 - 0.35 * shadow[..., None])
soil = np.where(pm[..., None], (tone * (0.7 + 0.3 * ph))[..., None] * np.array([1.0, 0.97, 0.92]), soil)
save("Dirt", soil * 1.1, np.where(pm, 0.5 + 0.5 * ph, 0.15 + 0.2 * fine), 3.0)
# 낙엽 더미 (숲 가장자리용)
base = np.array([0.20, 0.15, 0.10]) * (0.8 + 0.3 * fine[..., None])
lh, lid = splat(2600, 0.006, 0.013, 0.5)
lm = lh > 0
pal = np.array([[0.45, 0.25, 0.10], [0.38, 0.28, 0.12], [0.30, 0.18, 0.09], [0.50, 0.38, 0.16], [0.26, 0.24, 0.12]])
lc = pal[np.maximum(lid, 0) % 5] * rng.uniform(0.7, 1.1, 4000)[np.maximum(lid, 0)][..., None]
leaf = np.where(lm[..., None], lc * (0.75 + 0.25 * lh[..., None]), base)
leaf *= (1 - 0.3 * np.clip(blur(lm.astype(float), 0.003) - lm, 0, 1))[..., None]
save("Leaves", leaf * 1.1, np.where(lm, 0.4 + 0.3 * lh + 0.3 * (lid % 7) / 7, 0.1), 2.5)
print("ok")
