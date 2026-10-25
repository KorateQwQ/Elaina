"""Assemble freshly rendered FNA chest-approach frames. Run run.ps1 first."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[2]
folder = root / ".vissandbox/kuma-preview/chest-approach"
font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 24)
labels = {"travel": "右键派出 · 玩偶飞向宝箱", "gesture": "到达宝箱旁 · 播放开箱动作", "open": "动作完成 · 打开宝箱", "return": "关闭宝箱 · 玩偶返回"}
states = (folder / "states.txt").read_text().splitlines()
frames = []
for path, state in zip(sorted(folder.glob("frame-*.png")), states):
    frame = Image.open(path).convert("RGB")
    draw = ImageDraw.Draw(frame)
    draw.text((26, 22), "FNA 离线预览  |  " + labels[state], font=font, fill=(232, 225, 244))
    frames.append(frame)
frames[0].save(folder / "chest-approach.gif", save_all=True, append_images=frames[1:], duration=67, loop=0)
selected = [0, next(i for i, s in enumerate(states) if s == "gesture") // 2,
            next(i for i, s in enumerate(states) if s == "gesture"),
            next(i for i, s in enumerate(states) if s == "open"),
            next(i for i, s in enumerate(states) if s == "return") + 3, len(frames) - 1]
sheet = Image.new("RGB", (1000, 600), (23, 30, 43))
for cell, index in enumerate(selected):
    sheet.paste(frames[index].resize((500, 200), Image.Resampling.NEAREST), ((cell % 2) * 500, (cell // 2) * 200))
sheet.save(folder / "chest-approach-sheet.png")
print(folder / "chest-approach.gif")
