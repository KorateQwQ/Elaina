"""Promote the approved Shimmer item art into the packaged alchemy assets.

Run from any directory: python Tools/PotionPixel/PrepareShimmerItems.py
Then rebuild MysteryPixel and Outlines using Tools/AlchemyPreview (see its README).
The upper-left variant contains two disconnected components; retain both, without scaling.
"""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "ElainaModAlchemy/item/ExampleAssets"
SOURCE = ROOT / "ElainaModAlchemy/item/Untitled 10152026 050309.png"
BOTTLE = ROOT / "Tools/PotionPixel/output/BottledShimmer_Pixel_New.png"


def main():
    with Image.open(SOURCE) as original:
        region = original.convert("RGBA").crop((0, 0, 40, 32))
        dropper = region.crop(region.getbbox())
        if dropper.size != (27, 28):
            raise ValueError("The approved upper-left dropper must be 27x28")
        dropper.save(ASSETS / "AetherDropper_Pixel.png")
    with Image.open(BOTTLE) as bottle:
        if bottle.size != (18, 22):
            raise ValueError("The approved bottled Shimmer must be 18x22")
        bottle.save(ASSETS / "BottledShimmer_Pixel.png")
    print("Promoted AetherDropper (27x28) and BottledShimmer (18x22), without resampling.")


if __name__ == "__main__":
    main()
