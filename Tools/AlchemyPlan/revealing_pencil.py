"""Quiet, layered coloured-pencil rendering for the reviewed revealing dust.

Only SVGs tagged data-pencil-art="revealing-dust" use this treatment. The
approved closed lilac diamond and ivory star match the item's pixel emblem.
"""
import copy
import random
import xml.etree.ElementTree as ET

NS = '{http://www.w3.org/2000/svg}'


def stroke(parent, d, color, width, opacity):
    return ET.SubElement(parent, NS + 'path', {
        'd': d, 'fill': 'none', 'stroke': color,
        'stroke-width': str(width), 'opacity': str(opacity),
        'stroke-linecap': 'round', 'stroke-linejoin': 'round'})


def refine(root, original):
    defs = root.find(NS + 'defs')
    drawing = root.find(NS + 'g')
    if defs is None or drawing is None:
        raise ValueError('RevealingDust is missing its drawing or definitions.')
    # Share one full-canvas pencil sample between pigments instead of copying
    # hundreds of little paths into every colour swatch.
    grain = ET.SubElement(defs, NS + 'g', {'id': 'revealing-pencil-grain'})
    rng = random.Random(970)
    passes = {}
    for row in range(-2, 54):
        intercept = row * 1.63
        x = -4 + rng.uniform(-1, 1)
        while x < 54:
            length = rng.uniform(2.2, 7.3)
            y = intercept - x * .67
            mid_x = x + length / 2
            mid_y = y - length * .335
            if -5 < mid_y < 55:
                quiet = ((mid_x - 20.8) / 6.6) ** 2 + ((mid_y - 33) / 7) ** 2
                opacity = rng.choice((.32, .43, .54)) * (.24 if quiet < 1 else 1)
                key = (rng.choice((.18, .23, .28)), round(opacity, 2))
                passes.setdefault(key, []).append(
                    f'M{x:.2f} {y:.2f}l{length*.48:.2f} {-length*.32+rng.uniform(-.15,.15):.2f} {length*.52:.2f} {-length*.35:.2f}')
            x += length + rng.uniform(.35, 1.15)
    for (width, opacity), paths in passes.items():
        stroke(grain, ''.join(paths), '#a183a0', width, opacity)
    flecks = []
    for _ in range(230):
        x, y = rng.uniform(0, 50), rng.uniform(0, 50)
        length = rng.uniform(.18, .65)
        flecks.append(f'M{x:.2f} {y:.2f}l{length:.2f} {-length*.6:.2f}')
    stroke(grain, ''.join(flecks), '#f3e6cf', .15, .34)

    # Fine, broken strokes retain soft pigment and open space around the mark.
    for pattern in defs.findall(NS + 'pattern'):
        if not pattern.get('id', '').startswith('pencil-hatch-'):
            continue
        base = pattern.find(NS + 'rect')
        pigment = base.get('fill', '#cfbb9b')
        pattern.set('width', '50')
        pattern.set('height', '50')
        pattern[:] = []
        ET.SubElement(pattern, NS + 'rect', {'width': '50', 'height': '50', 'fill': pigment})
        ET.SubElement(pattern, NS + 'use', {'href': '#revealing-pencil-grain'})

    # Preserve the notebook's plum pencil contours, with gentler pressure.
    seen_ids = set()
    for node in drawing.iter():
        if node.get('stroke') == '#79558f':
            node.set('stroke', '#917497')
            width = float(node.get('stroke-width', '.4'))
            if width >= .65:
                node.set('stroke-width', str(round(width * .8, 3)))
            if node.get('opacity') == '.4':
                node.set('opacity', '.25')
        ident = node.get('id')
        if ident in seen_ids:
            node.attrib.pop('id', None)
        elif ident:
            seen_ids.add(ident)

    # Preserve the closed purple frame and ivory star from the source emblem.
    source_mark = original.find(".//" + NS + "g[@id='revealing-emblem']")
    if source_mark is None:
        raise ValueError('RevealingDust source is missing its reviewed emblem.')
    found = False
    for parent in drawing.iter():
        for node in list(parent):
            if node.get('id') != 'revealing-emblem':
                continue
            position = list(parent).index(node)
            parent.remove(node)
            parent.insert(position, copy.deepcopy(source_mark))
            found = True
            break
        if found:
            break
    if not found:
        raise ValueError('RevealingDust converted emblem was lost.')

    # A few short strokes follow the rounded cloth; no dense hatching on the mark.
    cloth = ET.Element(NS + 'g', {'id': 'revealing-cloth-pencil'})
    for d in (
        'M12.3 28.7l1.2-1.1m-1.7 3.6 1.5-1.2m-1.8 3.6 1.4-1',
        'M28.1 28.8l1.1-1m-1.1 3.4 1.7-1.4m-1.4 3.6 1.9-1.4',
        'M27.4 36.5l2.5-1.7m-3.4 3.5 2.9-1.9m-4.6 3.5 3.3-1.7',
        'M15.4 40.5l1.4-.8m1.3 1.6 1.9-1.1m1.4 1.3 1.8-1',
    ):
        stroke(cloth, d, '#9b7b95', .26, .42)
    # Insert over the body, below all emblem strokes and the outer dust trail.
    mark_index = list(drawing).index(drawing.find(NS + "g[@id='revealing-emblem']"))
    drawing.insert(mark_index, cloth)
