"""奥义特写人像帧烘焙。

从 ElainaModSkills/CutIn/png 的原始抠图帧（960x540，直通 alpha，已全部修成闭口）烘焙全部帧：
乘上边缘渐隐遮罩（与 Tools/CutInPreview 的 FADE 参数一致）后转为预乘 RGBA，
写入可打包目录 ElainaModSkills/CutIn/Frames/CutIn_NN.png（NN 为源帧号），并记录清单。
与前一张已烘焙帧逐像素相同的源帧（如 21-28 的定格）不重复输出，清单中记为别名（sameAs），C# 时间轴只引用不重复的帧。

tML 打包 rawimg 时不会再预乘（见 Tools/FixedTextureBake/README.md），所以 PNG 直接存预乘值。

用法：
    python Tools/CutInBake/bake.py           # 烘焙
    python Tools/CutInBake/bake.py --check   # 校验正式 PNG 与清单、与重新烘焙结果一致
"""
import hashlib
import json
import os
import sys

from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SRC_DIR = os.path.join(ROOT, "ElainaModSkills", "CutIn", "png")
OUT_DIR = os.path.join(ROOT, "ElainaModSkills", "CutIn", "Frames")
MANIFEST = os.path.join(os.path.dirname(os.path.abspath(__file__)), "manifest.json")

# 全部源帧。输出文件名中的序号即源帧号，也是 C# 中 ElainaCutIn 时间轴使用的帧号。
FRAMES = list(range(1, 29))

W, H = 960, 540
# (边缘坐标, 内侧坐标)：从边缘的 0 平滑过渡到内侧的 1（源像素）
FADE_LEFT = (0, 230)
FADE_RIGHT = (960, 760)
FADE_TOP = (0, 50)
FADE_BOTTOM = (540, 505)


def smooth(x):
    x = max(0.0, min(1.0, x))
    return x * x * (3 - 2 * x)


def ramp(p, edge, inner):
    return smooth((p - edge) / (inner - edge))


def bake_frame(src_path):
    im = Image.open(src_path).convert("RGBA")
    if im.size != (W, H):
        raise ValueError(f"{src_path} 尺寸为 {im.size}，应为 {(W, H)}")
    fx = [ramp(x + 0.5, *FADE_LEFT) * ramp(x + 0.5, *FADE_RIGHT) for x in range(W)]
    fy = [ramp(y + 0.5, *FADE_TOP) * ramp(y + 0.5, *FADE_BOTTOM) for y in range(H)]
    src = im.load()
    out = Image.new("RGBA", (W, H))
    dst = out.load()
    for y in range(H):
        my = fy[y]
        for x in range(W):
            r, g, b, a = src[x, y]
            na = int(round(a * fx[x] * my))
            if na == 0:
                dst[x, y] = (0, 0, 0, 0)
            else:
                dst[x, y] = (int(round(r * na / 255)), int(round(g * na / 255)), int(round(b * na / 255)), na)
    return out


def rgba_hash(im):
    return hashlib.sha256(im.tobytes()).hexdigest()


def out_path(frame):
    return os.path.join(OUT_DIR, f"CutIn_{frame:02d}.png")


def source_path(frame):
    return os.path.join(SRC_DIR, f"frame-{frame:03d}.png")


def source_hash(frame):
    return rgba_hash(Image.open(source_path(frame)).convert("RGBA"))


def bake():
    os.makedirs(OUT_DIR, exist_ok=True)
    for name in os.listdir(OUT_DIR):
        if name.startswith("CutIn_") and name.endswith(".png"):
            os.remove(os.path.join(OUT_DIR, name))
    entries = []
    baked = {}   # 源帧 RGBA 哈希 -> 已烘焙的帧号
    for frame in FRAMES:
        key = source_hash(frame)
        if key in baked:
            entries.append({"frame": frame, "source": f"frame-{frame:03d}.png", "sameAs": baked[key]})
            print(f"frame-{frame:03d}.png == frame-{baked[key]:03d}.png（不重复输出）")
            continue
        im = bake_frame(source_path(frame))
        im.save(out_path(frame), optimize=True)
        baked[key] = frame
        entries.append({"frame": frame, "source": f"frame-{frame:03d}.png", "size": [W, H],
                        "alpha": "premultiplied", "rgbaSha256": rgba_hash(im)})
        print(f"CutIn_{frame:02d}.png <- frame-{frame:03d}.png")
    with open(MANIFEST, "w", encoding="utf-8") as f:
        json.dump({"fade": {"left": FADE_LEFT, "right": FADE_RIGHT, "top": FADE_TOP, "bottom": FADE_BOTTOM},
                   "frames": entries}, f, ensure_ascii=False, indent=2)
        f.write("\n")


def check():
    with open(MANIFEST, encoding="utf-8") as f:
        manifest = json.load(f)
    failures = 0
    unique = [e for e in manifest["frames"] if "sameAs" not in e]
    expected = {f"CutIn_{e['frame']:02d}.png" for e in unique}
    present = {n for n in os.listdir(OUT_DIR) if n.endswith(".png")}
    for name in sorted(present ^ expected):
        print(f"{'多余' if name in present else '缺少'}文件: {name}")
        failures += 1
    for e in manifest["frames"]:
        if "sameAs" in e:
            ok = source_hash(e["frame"]) == source_hash(e["sameAs"])
            print(("OK   " if ok else "FAIL ") + f"frame-{e['frame']:03d}.png 仍与 frame-{e['sameAs']:03d}.png 相同")
            failures += 0 if ok else 1
            continue
        path = out_path(e["frame"])
        if not os.path.exists(path):
            continue
        im = Image.open(path).convert("RGBA")
        rebaked = bake_frame(source_path(e["frame"]))
        ok = list(im.size) == e["size"] and rgba_hash(im) == e["rgbaSha256"] == rgba_hash(rebaked)
        print(("OK   " if ok else "FAIL ") + os.path.basename(path))
        failures += 0 if ok else 1
    if failures:
        raise SystemExit(f"{failures} 项校验失败")
    print(f"all {len(manifest['frames'])} frames ok ({len(unique)} textures)")


if __name__ == "__main__":
    check() if "--check" in sys.argv else bake()
