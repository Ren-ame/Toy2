# 바닥 텍스처 2종(타일링): 숲 바닥(잔디+흙+자갈), 대리석 포석(틈새 이끼)
import numpy as np, sys
from PIL import Image
N = 1024; OUT = sys.argv[1]; rng = np.random.default_rng(11)
F2 = np.fft.fftfreq(N)[:, None] ** 2 + np.fft.fftfreq(N)[None, :] ** 2; F2[0, 0] = 1
def noise(p):
    s = (rng.normal(size=(N, N)) + 1j * rng.normal(size=(N, N))) / F2 ** (p / 2); s[0, 0] = 0
    n = np.real(np.fft.ifft2(s)); return (n - n.min()) / (n.max() - n.min())
def sstep(a, b, x): t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)
def blur(h, r):   # 주기적 가우시안 블러 (FFT)
    g = np.exp(-F2 * (np.pi * r) ** 2 * 2); g[0, 0] = 1; return np.real(np.fft.ifft2(np.fft.fft2(h) * g))
def save(name, col, h, strength, smooth):
    Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8)).save(f"{OUT}/{name}_BaseColor.png")
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * strength; dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * strength
    n = np.stack([-dx, dy, np.ones_like(h)], -1); n /= np.linalg.norm(n, axis=-1, keepdims=True)
    Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8)).save(f"{OUT}/{name}_Normal.png")
    ms = np.zeros((N, N, 4), np.uint8); ms[..., 3] = (np.clip(smooth, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(ms, "RGBA").save(f"{OUT}/{name}_MetallicSmoothness.png")
y, x = np.mgrid[0:N, 0:N] / N

# ================= 1) 숲 바닥 =================
big, mid, fine, grain = noise(2.4), noise(1.6), noise(0.9), noise(0.2)
rag = noise(1.0)
dirt_mask = sstep(0.55, 0.63, mid * 0.55 + big * 0.25 + rag * 0.2)            # 1 = 흙 (작은 패치 여러 개, 경계 너덜너덜)
blade, clump, dry = noise(0.35), noise(1.3), noise(2.0)
grass = np.array([0.22, 0.27, 0.13]) * (0.78 + 0.32 * blade[..., None]) * (0.85 + 0.3 * clump[..., None])
grass = grass * (1 - 0.45 * sstep(0.55, 0.85, dry)[..., None]) + np.array([0.30, 0.27, 0.15]) * 0.45 * sstep(0.55, 0.85, dry)[..., None]   # 마른 풀
dirt = np.array([0.30, 0.24, 0.18]) * (0.78 + 0.3 * fine[..., None]) * (0.9 + 0.2 * grain[..., None])
col = grass * (1 - dirt_mask[..., None]) + dirt * dirt_mask[..., None]
h = (1 - dirt_mask) * (0.35 + 0.4 * blade) + dirt_mask * (0.12 + 0.2 * fine)
def splat(n, rmin, rmax, aspect, prob, colfn):
    hh = np.zeros((N, N)); cc = np.zeros((N, N, 3)); mm = np.zeros((N, N), bool)
    for _ in range(n):
        cx, cy = rng.random(2)
        if rng.random() > prob(dirt_mask[int(cy * N) % N, int(cx * N) % N]): continue
        r = rng.uniform(rmin, rmax); ang = rng.uniform(0, np.pi); ca, sa = np.cos(ang), np.sin(ang)
        ddx = (x - cx + 0.5) % 1 - 0.5; ddy = (y - cy + 0.5) % 1 - 0.5
        u, v = (ddx * ca + ddy * sa) / r, (-ddx * sa + ddy * ca) / (r * aspect)
        d2 = u * u + v * v; m = d2 < 1; dome = np.sqrt(np.clip(1 - d2, 0, 1))
        upd = m & (dome > hh); hh[upd] = dome[upd]; cc[upd] = colfn(); mm |= upd
    return hh, cc, mm
lh, lc, lm = splat(500, 0.004, 0.008, 0.45, lambda d: 0.25 + 0.5 * d,                       # 낙엽 부스러기
                   lambda: np.array([0.32, 0.20, 0.10]) * rng.uniform(0.6, 1.2))
col = np.where(lm[..., None], col * 0.4 + lc * 0.6, col); h = np.where(lm, h + 0.1 * lh, h)
ph, pc, pm = splat(140, 0.003, 0.009, 0.75, lambda d: 0.15 + 0.85 * d,                      # 자갈
                   lambda: np.array([1.0, 0.97, 0.92]) * rng.uniform(0.30, 0.45))
shadow = np.clip(blur(pm.astype(float), 0.004) * 1.6, 0, 1) * (1 - pm)
col = col * (1 - 0.35 * shadow[..., None])
col = np.where(pm[..., None], pc * (0.7 + 0.3 * ph[..., None]), col)
h = np.where(pm, 0.5 + 0.5 * ph, h)
save("Ground_Forest", col * 1.15, h, 3.0, 0.07 + 0.2 * ph * pm + 0.04 * dirt_mask)

# ================= 2) 대리석 포석 =================
w1, w2 = noise(1.9), noise(1.9)
ux = (x + 0.012 * (w1 - 0.5)) % 1; uy = (y + 0.012 * (w2 - 0.5)) % 1     # 가장자리 살짝 울퉁불퉁
ROWS = 4; row = np.floor(uy * ROWS).astype(int); vy = uy * ROWS - row
edge_d = np.minimum(vy, 1 - vy) / ROWS                                    # 위아래 이음매까지 거리
stone_id = np.zeros((N, N), int); dist_x = np.full((N, N), 1.0)
sid = 0; tints = []
for r in range(ROWS):
    widths = rng.uniform(0.6, 1.4, 4); widths /= widths.sum()
    starts = np.concatenate([[0], np.cumsum(widths)])[:-1] + rng.random()  # 행마다 엇갈리게
    rm = row == r
    for k in range(len(widths)):
        a = starts[k] % 1; rel = (ux - a) % 1
        m = rm & (rel < widths[k])
        stone_id[m] = sid; dist_x[m] = np.minimum(rel[m], widths[k] - rel[m]); tints.append(rng.uniform(0.86, 1.0)); sid += 1
d = np.minimum(edge_d, dist_x)                                            # 이음매까지 거리 (UV)
chip, gapn, mossn, vein_w = noise(0.9), noise(1.4), noise(1.7), noise(1.8)
gap = 0.004 + 0.003 * gapn + 0.012 * sstep(0.62, 0.8, chip) * sstep(0.03, 0.0, d)   # 틈 폭 변화 + 모서리 깨짐
in_stone = sstep(gap, gap + 0.003, d)
bevel = sstep(gap, gap + 0.02, d)
tint = np.array(tints)[stone_id]
fine2 = noise(1.1)
veins = np.abs(np.sin(2 * np.pi * (3 * x + 2 * y) + 9 * (vein_w - 0.5)))
vein = np.clip(1 - veins ** 0.2, 0, 1) * 0.35
marble = np.array([0.80, 0.78, 0.74]) * tint[..., None] * (0.92 + 0.1 * fine2[..., None]) * (1 - vein[..., None] * 0.35) * (1 - 0.07 * big[..., None])
# 균열: 몇몇 돌에 무작위 꺾은선 (마스크 위에 그림, 9칸 복제로 타일링 유지)
from PIL import ImageDraw
cm = Image.new("L", (N, N), 0); dr = ImageDraw.Draw(cm)
for _ in range(14):
    px, py = rng.random(2) * N; pts = [(px, py)]; ang = rng.uniform(0, 2 * np.pi)
    for _ in range(rng.integers(5, 11)):
        ang += rng.normal(0, 0.6); px += np.cos(ang) * rng.uniform(8, 22); py += np.sin(ang) * rng.uniform(8, 22); pts.append((px, py))
    for ox in (-N, 0, N):
        for oy in (-N, 0, N): dr.line([(p[0] + ox, p[1] + oy) for p in pts], fill=255, width=1)
crack = np.asarray(cm, float) / 255 * in_stone
crack = np.clip(crack * 0.7 + blur(crack, 0.0015) * 0.8, 0, 1)
moss_on_stone = (1 - bevel) * sstep(0.62, 0.85, mossn) * 0.7 + sstep(0.78, 0.92, big) * 0.5 * sstep(0.4, 0.7, mossn)
moss = np.array([0.21, 0.25, 0.13]) * (0.7 + 0.5 * blade[..., None])
gapcol = np.array([0.15, 0.12, 0.09]) * (0.8 + 0.4 * fine[..., None])
gapcol = gapcol * (1 - sstep(0.55, 0.8, mossn))[..., None] + moss * sstep(0.55, 0.8, mossn)[..., None]
col = marble * (0.72 + 0.28 * bevel[..., None])
col = col * (1 - 0.8 * moss_on_stone[..., None]) + moss * 0.8 * moss_on_stone[..., None]
col = col * (1 - 0.4 * crack[..., None])
col = col * in_stone[..., None] + gapcol * (1 - in_stone[..., None])
h = 0.2 + 0.6 * bevel * in_stone + 0.05 * fine2 - 0.2 * crack + 0.03 * moss_on_stone
save("Ground_Flagstone", col, h, 4.0, ((0.45 + 0.1 * fine2) * (1 - 0.7 * moss_on_stone) * (1 - crack)) * in_stone + 0.06)
print("ground ok")
