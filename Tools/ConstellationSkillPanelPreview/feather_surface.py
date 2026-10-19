"""抹平正式 NotebookSurface.png 里星图柔光的上沿硬边（原地修改，可重复运行）。

reference.py 烘焙底图时，星图柔光被旧星图视口（y=208 起，overflow:hidden）裁住，y=208 处有一道约 3 级亮度的台阶。
星图可见区域上提到 y=150（ConstellationSkillPanel.SkyY）后，星点、立绘都画到了台阶上方，台阶就读成一道分割线。

做法：逐列量出台阶高度 δ(x)（上下各 6 行求均值，再横向平滑），y≥208 起减去 δ·(1−smoothstep)，让柔光在 FEATHER
像素内渐入——只移走台阶，纸面颗粒原样保留。已抹平的底图再跑一次时 δ≈0，不会叠加。reference.py 重新生成底图后会自动调用。
"""
import pathlib

from PIL import Image

repo = pathlib.Path(__file__).resolve().parents[2]
ASSET = repo / "ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets/NotebookSurface.png"
EDGE, FEATHER, X0, X1, K = 208, 80, 26, 753, 12


def feather(path=ASSET):
    im = Image.open(path).convert("RGB")
    px = im.load()
    w = im.width

    def band(x, y0, y1, c):
        return sum(px[x, y][c] for y in range(y0, y1)) / (y1 - y0)

    raw = [[band(x, EDGE, EDGE + 6, c) - band(x, EDGE - 6, EDGE, c) if X0 <= x < X1 else 0 for c in range(3)] for x in range(w)]
    delta = []
    for x in range(w):
        lo, hi = max(0, x - K), min(w, x + K + 1)
        delta.append([sum(raw[i][c] for i in range(lo, hi)) / (hi - lo) for c in range(3)])
    for y in range(EDGE, EDGE + FEATHER):
        t = (y - EDGE) / FEATHER
        k = 1 - t * t * (3 - 2 * t)
        for x in range(X0 - K, X1 + K):
            r, g, b = px[x, y]
            d = delta[x]
            px[x, y] = (round(r - d[0] * k), round(g - d[1] * k), round(b - d[2] * k))
    im.save(path)
    print("feathered:", path.relative_to(repo))


if __name__ == "__main__":
    feather()
