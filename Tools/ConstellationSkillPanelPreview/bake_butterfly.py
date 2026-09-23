"""Match the HTML alpha mask, preserving the supplied straight-alpha sprite sheet."""
from pathlib import Path
from PIL import Image

assets = Path(__file__).resolve().parents[2] / "ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets"
source = Image.open(assets / "butterFly.png").convert("RGBA")
assert source.size == (256, 256), "Expected sixteen 64x64 frames"
alpha = source.getchannel("A")
# tML retains RGB at nonzero alpha: white premultiplied mask avoids rectangular fringes.
Image.merge("RGBA", (alpha, alpha, alpha, alpha)).save(assets / "ButterflyMask.png")
print(assets / "ButterflyMask.png")
