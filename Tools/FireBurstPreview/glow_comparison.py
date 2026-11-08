"""Package the actual FNA frame exports into synchronized normal/slow GIF comparisons."""
from pathlib import Path
import argparse
from PIL import Image, ImageDraw, ImageFont


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parents[2]
                        / ".vissandbox/fire-burst-preview/out")
    parser.add_argument("--font", default="C:/Windows/Fonts/msyh.ttc")
    args = parser.parse_args()
    font = ImageFont.truetype(args.font, 18)
    small = ImageFont.truetype(args.font, 14)
    width, height, gap = 360, 480, 8
    variants = ["texture", "body2", "body3"]
    titles = ["贴图光晕 · 历史对照", "火柱自身增亮 · 峰值 ×2", "火柱自身增亮 · ×3 已采用"]
    subtitle = ["范围已减半，持续 8 帧", "取消光斑，仅火柱 RGB 增亮", "取消光斑，仅火柱 RGB 增亮"]
    canvas_size = (width * 3 + gap * 4, height + 106)

    def compose(background, age, speed):
        frame = Image.new("RGB", canvas_size, (22, 25, 32))
        draw = ImageDraw.Draw(frame)
        for index, variant in enumerate(variants):
            x = gap + index * (width + gap)
            source = args.output / "glow-comparison" / background / variant / f"f{age:02d}.png"
            with Image.open(source) as rendered:
                crop = rendered.convert("RGB").crop((175, 0, 925, 1000))
                frame.paste(crop.resize((width, height), Image.Resampling.LANCZOS), (x, 58))
            draw.text((x + 8, 5), titles[index], font=font, fill=(255, 220, 160))
            draw.text((x + 8, 33), subtitle[index], font=small, fill=(200, 207, 220))
        stage = f"前摇 {age}/30" if age <= 30 else f"爆发第 {age - 30} 帧" if age < 60 else "火柱结束 · 残余粒子"
        label = "白天" if background == "day" else "夜晚"
        draw.text((12, height + 64), f"{label}  |  {speed}  |  {stage}  |  同一随机种子、同一 Bloom 配置", font=small, fill=(225, 230, 240))
        draw.text((12, height + 84), "FNA 离线预览 · 游戏绘制代码 · 色块背景 · 右列 ×3 为正式游戏方案", font=small, fill=(156, 167, 185))
        return frame

    for background in ["day", "night"]:
        for slow in [False, True]:
            ages = list(range(27, 46)) if slow else list(range(1, 79))
            speed = "1/4 倍速" if slow else "正常速度（60 tick/s）"
            frames = [compose(background, age, speed) for age in ages]
            # GIF delays use 10 ms units; alternating delays preserve total playback time.
            durations = [round((i + 1) * (4 if slow else 1) * 100 / 60) * 10
                         - round(i * (4 if slow else 1) * 100 / 60) * 10 for i in range(len(ages))]
            frames.append(frames[-1].copy())
            durations.append(650 if slow else 500)
            name = f"glow-comparison-{background}-{'slow' if slow else 'normal'}.gif"
            frames[0].save(args.output / name, save_all=True, append_images=frames[1:],
                           duration=durations, loop=0, disposal=2, optimize=False)
            print(args.output / name)
        # Representative native-render crops remain available in glow-comparison/.
        sheet = Image.new("RGB", (canvas_size[0], canvas_size[1] * 3))
        for row, age in enumerate([31, 33, 36]):
            sheet.paste(compose(background, age, "静态对照"), (0, row * canvas_size[1]))
        sheet.save(args.output / f"glow-comparison-{background}-sheet.png")


if __name__ == "__main__":
    main()
