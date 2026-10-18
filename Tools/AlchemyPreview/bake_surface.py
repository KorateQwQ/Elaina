"""Alchemy paper: the skill notebook's light, without the skill chart's clipped layers.

The skill surface is built from clipped panel layers, which leave faint rectangular steps.
Here it is averaged into a 10 px grid, Gaussian-blurred (sigma 3 cells ≈ 30 px) and
B-spline upsampled, so both notebooks share the same light pools (catalog and detail column)
but the alchemy page has no seams. A deterministic sub-level dither prevents 8-bit banding.

1100x800 opaque RGB PNG; one cached draw at runtime, ~3.36 MiB as RGBA8.
Pillow is the only dependency. Does not modify the skill notebook surface.
Rerun bake_redesign.py afterwards: its edge fades are cut from this surface.
"""
from pathlib import Path
import math
from PIL import Image

repo = Path(__file__).resolve().parents[2]
width, height, cell, sigma = 1100, 800, 10, 3
skill = Image.open(repo / "ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets/NotebookSurface.png").convert("RGB")
gw, gh = width // cell, height // cell
# Exact float cell means (Image.resize would round each cell to 8 bits).
src = skill.load()
grid = [[[0.0] * 3 for _ in range(gw)] for _ in range(gh)]
for y in range(height):
    row = grid[y // cell]
    for x in range(width):
        acc, p = row[x // cell], src[x, y]
        acc[0] += p[0]; acc[1] += p[1]; acc[2] += p[2]
for row in grid:
    for acc in row:
        for c in range(3): acc[c] /= cell * cell

radius = sigma * 3
kernel = [math.exp(-(i * i) / (2 * sigma * sigma)) for i in range(-radius, radius + 1)]
total = sum(kernel)
kernel = [k / total for k in kernel]
def blur(get, n):
    return [[sum(kernel[k] * get(min(n - 1, max(0, i + k - radius)))[c] for k in range(len(kernel))) for c in range(3)] for i in range(n)]
grid = [blur(lambda i, r=row: r[i], gw) for row in grid]
columns = [blur(lambda j, x=x: grid[j][x], gh) for x in range(gw)]
grid = [[columns[x][y] for x in range(gw)] for y in range(gh)]

def spline(t):
    # Uniform cubic B-spline weights: C2 continuous, no gradient kinks at cell borders.
    return ((1 - t) ** 3 / 6, (3 * t ** 3 - 6 * t * t + 4) / 6, (-3 * t ** 3 + 3 * t * t + 3 * t + 1) / 6, t ** 3 / 6)
surface = Image.new("RGB", (width, height))
pixels = surface.load()
for y in range(height):
    fy = (y + .5) / cell - .5
    gy = math.floor(fy)
    wy = spline(fy - gy)
    rows = [grid[min(gh - 1, max(0, gy + k - 1))] for k in range(4)]
    for x in range(width):
        fx = (x + .5) / cell - .5
        gx = math.floor(fx)
        wx = spline(fx - gx)
        cols = [min(gw - 1, max(0, gx + k - 1)) for k in range(4)]
        dither = ((x * 73856093 ^ y * 19349663) % 1024) / 1024 - .5
        value = []
        for c in range(3):
            v = 0.0
            for j in range(4):
                r = rows[j]
                v += wy[j] * (wx[0] * r[cols[0]][c] + wx[1] * r[cols[1]][c] + wx[2] * r[cols[2]][c] + wx[3] * r[cols[3]][c])
            value.append(round(max(0, min(255, v + dither))))
        pixels[x, y] = tuple(value)
target = repo / "ElainaModAlchemy/UI/Assets/NotebookSurface.png"
surface.save(target)
print(target)
