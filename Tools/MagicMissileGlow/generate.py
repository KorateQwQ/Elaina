"""Generate the small, straight-alpha head glow used without post-processing Bloom."""

from pathlib import Path

from PIL import Image


SIZE = 128
OUTPUT = Path(__file__).resolve().parents[2] / "ElainaModSkills/Skills/MagicMissile/SoftGlow.png"


def main():
    image = Image.new("RGBA", (SIZE, SIZE))
    pixels = image.load()
    radius = SIZE / 2 - 1
    for y in range(SIZE):
        for x in range(SIZE):
            dx = (x + 0.5 - SIZE / 2) / radius
            dy = (y + 0.5 - SIZE / 2) / radius
            distance_squared = dx * dx + dy * dy
            alpha = round(200 * max(0, 1 - distance_squared) ** 3)
            pixels[x, y] = (255, 255, 255, alpha) if alpha else (0, 0, 0, 0)
    image.save(OUTPUT)


if __name__ == "__main__":
    main()
