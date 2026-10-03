"""Compatibility entry point for applying only the triangle Star Power UI art.

The shared approved notebook exporter owns SVG, pencil, premultiplied UI PNG
and silhouette generation. This command does not change item pixel art.
"""
import importlib.util
from pathlib import Path

if __name__ == '__main__':
    spec = importlib.util.spec_from_file_location('notebook_potions', Path(__file__).with_name('apply_notebook_potions.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.apply(['StarPowerPotion'])
