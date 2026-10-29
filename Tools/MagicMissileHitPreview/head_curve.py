# 复刻 MagicMissileTrail.fx 的 Head 通道，测量不同消融值下爆点剩余的可见亮度（4 倍降采样统计），
# 反解出让可见量匀速下降的消融表（MagicMissileTrailVisuals.DissolveByVisibleLoss），并按 DrawHead 的时间轴自检。
# 用法：python -I head_curve.py <KL 目录>，默认参数即正式设置（Noise/5、缩放 1.8、反相）；对照其他噪声时覆盖：
#   [--noise Effects/Tex/PerlinX.png] [--scale 0.8] [--centered] [--invert | --no-invert] [--weight 0.5]
import argparse, math, random
from PIL import Image
ap = argparse.ArgumentParser()
ap.add_argument('kl'); ap.add_argument('--noise', default='Effects/Tex/Noise/5.png')
ap.add_argument('--scale', type=float, default=1.8); ap.add_argument('--centered', action='store_true')
ap.add_argument('--invert', action=argparse.BooleanOptionalAction, default=True); ap.add_argument('--weight', type=float, default=.5)
args = ap.parse_args()
stamp = Image.open(f'{args.kl}/Effects/Tex/Sparkle/T_Vfx_Stamp_HitSword_22.PNG').convert('RGBA')
noise = Image.open(f'{args.kl}/{args.noise}').convert('L')
N = noise.size[0]; npx = noise.load()
S = 64
def smooth(a, b, x):
    t = min(max((x - a) / (b - a), 0), 1); return t * t * (3 - 2 * t)
edge = .08
ds = [i / 100 for i in range(101)]
curves = []
random.seed(1)
for frame in range(4):
    tile = stamp.crop((frame % 2 * 256, frame // 2 * 256, frame % 2 * 256 + 256, frame // 2 * 256 + 256)).resize((S, S), Image.BOX)
    px = tile.load()
    for _ in range(1 if args.centered else 4):
        off = (0, 0) if args.centered else (random.random(), random.random())
        pts = []
        for y in range(S):
            for x in range(S):
                r, g, b, a = px[x, y]
                lum = (r + g + b) / 765
                if lum < .002: continue
                lx, ly = (x + .5) / S, (y + .5) / S
                radius = min(math.hypot(lx - .5, ly - .5) * 2 / .75, 1)
                u = (lx * args.scale + off[0]) % 1; v = (ly * args.scale + off[1]) % 1
                n = npx[int(u * N) % N, int(v * N) % N] / 255
                if args.invert: n = 1 - n
                pts.append((lum, (1 - radius) * (1 - args.weight) + n * args.weight))
        c = []
        for d in ds:
            cut = d * (1 + edge) - edge
            c.append(sum(l * smooth(cut, cut + edge, f) for l, f in pts))
        curves.append([v / c[0] for v in c])
A = [sum(c[i] for c in curves) / len(curves) for i in range(len(ds))]
def inv(target):
    for i in range(1, len(ds)):
        if A[i] <= target:
            t = (A[i - 1] - target) / max(A[i - 1] - A[i], 1e-9)
            return ds[i - 1] + t * (ds[i] - ds[i - 1])
    return 1.0
# 首项：可见量仍 ≥ 99.5% 的最大消融值；末项：可见量 ≤ 0.2% 的最小消融值
table = [inv(.995)] + [inv(1 - k / 10) for k in range(1, 10)] + [inv(.002)]
print('table', ', '.join(f'{x:.3f}f' for x in table))

def dissolve_for(visible):
    loss = min(max(1 - visible, 0), 1) * 10; i = min(int(loss), 9)
    return table[i] + (table[i + 1] - table[i]) * (loss - i)
def measured(d):
    k = min(d * 100, 99.999); i = int(k)
    return A[i] + (A[i + 1] - A[i]) * (k - i)
s0, h0, h1 = 1.4, 1.9, 1.5
worst = 0
for step in range(11):
    p = step / 10
    size = s0 + (1 - s0) * p; hdr = h0 + (h1 - h0) * p
    target = min(1, (1 - p) * s0 * s0 * h0 / (size * size * hdr))
    total = size * size * hdr * measured(dissolve_for(target)) / (s0 * s0 * h0)
    worst = max(worst, abs(total - (1 - p)))
print(f'max deviation from linear: {worst:.3f}')
