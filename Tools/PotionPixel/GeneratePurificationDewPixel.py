"""Compatibility entry point for the approved upright Purification Dew.

The former diagonal design has been replaced by the user-approved upright
handheld sprite. Both this command and the batch generators use its source.
"""
import importlib.util
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    'purification_dew_upright', Path(__file__).with_name('GeneratePurificationDewUprightPixel.py'))
_module = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_module)
build_sprite = _module.build_sprite

if __name__ == '__main__':
    _module.main()
