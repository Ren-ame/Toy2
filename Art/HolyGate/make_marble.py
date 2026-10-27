# 이음새 없이 반복되는(타일링) 대리석 텍스처 생성: BaseColor, Normal(OpenGL), MetallicSmoothness
import numpy as np, sys
from PIL import Image
N = 1024; OUT = sys.argv[1]; rng = np.random.default_rng(7)
def periodic_noise(scale_pow):
    # 주파수 공간에서 거른 랜덤 노이즈 → 자동으로 가장자리가 이어짐(주기적)
    f = np.fft.fftfreq(N)[:, None] ** 2 + np.fft.fftfreq(N)[None, :] ** 2
    f[0, 0] = 1
    spec = (rng.normal(size=(N, N)) + 1j * rng.normal(size=(N, N))) / f ** (scale_pow / 2)
    spec[0, 0] = 0
    n = np.real(np.fft.ifft2(spec)); return (n - n.min()) / (n.max() - n.min())
turb = periodic_noise(1.8); fine = periodic_noise(1.2); big = periodic_noise(2.6)
y, x = np.mgrid[0:N, 0:N] / N
veins = np.abs(np.sin(2 * np.pi * (2 * x + 1 * y) + 7.0 * (turb - 0.5) * 2))      # 정수 주파수 → 타일링 유지
veins2 = np.abs(np.sin(2 * np.pi * (1 * x - 3 * y) + 9.0 * (big - 0.5) * 2))
v = np.clip(1 - veins ** 0.18, 0, 1) * 0.55 + np.clip(1 - veins2 ** 0.12, 0, 1) * 0.30
base = np.array([0.93, 0.91, 0.87]); vein_col = np.array([0.62, 0.60, 0.58]); warm = np.array([0.95, 0.90, 0.80])
col = base[None, None] * (1 - v[..., None]) + vein_col[None, None] * v[..., None]
col = col * (0.95 + 0.06 * fine[..., None]) * (1 - 0.06 * big[..., None]) + warm * 0.04 * big[..., None]
Image.fromarray((np.clip(col, 0, 1) ** (1 / 1.0) * 255).astype(np.uint8)).save(f"{OUT}/HolyGate_Marble_BaseColor.png")
h = 0.6 * fine + 0.4 * (1 - v)                         # 높이: 결 + 잔 요철
dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 2.0; dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 2.0
nrm = np.stack([-dx, dy, np.ones_like(h)], -1); nrm /= np.linalg.norm(nrm, axis=-1, keepdims=True)   # OpenGL(Y+)
Image.fromarray(((nrm * 0.5 + 0.5) * 255).astype(np.uint8)).save(f"{OUT}/HolyGate_Marble_Normal.png")
smooth = np.clip(0.62 + 0.15 * (fine - 0.5) - 0.12 * v, 0, 1)   # 결 부분은 살짝 거칠게
ms = np.zeros((N, N, 4), np.uint8); ms[..., 3] = (smooth * 255).astype(np.uint8)
Image.fromarray(ms, "RGBA").save(f"{OUT}/HolyGate_Marble_MetallicSmoothness.png")
print("marble ok")
