"""Textured pencil passes for Ashen Facsimile Dust's overlapping emblem only.

The rear copy is offset and muted; the foreground has brighter layered pigment
and a stronger contour. Neither face is an opaque, flat vector-colour patch.
"""
import random
import xml.etree.ElementTree as ET

NS = '{http://www.w3.org/2000/svg}'


def stroke(parent, points, color, width, opacity=1):
    data = 'M' + ' '.join(f'{x:.3f} {y:.3f}' for x, y in points)
    return ET.SubElement(parent, NS + 'path', {
        'd': data, 'fill': 'none', 'stroke': color,
        'stroke-width': str(width), 'opacity': str(opacity),
        'stroke-linecap': 'round', 'stroke-linejoin': 'round',
    })


def path_data(points):
    return 'M' + ' '.join(f'{x:.3f} {y:.3f}' for x, y in points) + 'Z'


def pigment(parent, defs, points, name, *, front):
    shape = path_data(points)
    clip = ET.SubElement(defs, NS + 'clipPath', {
        'id': name, 'clipPathUnits': 'userSpaceOnUse',
    })
    ET.SubElement(clip, NS + 'path', {'d': shape})
    face = ET.SubElement(parent, NS + 'g', {'clip-path': f'url(#{name})'})
    # A translucent wash leaves the underlying paper/cloth visible. Uneven
    # overlapping strokes, rather than a solid white fill, supply the light face.
    ET.SubElement(face, NS + 'path', {
        'd': shape, 'fill': '#e7dfe9' if front else '#a592b2',
        'fill-opacity': '.48' if front else '.44',
    })
    rng = random.Random(418 if front else 207)
    for row in range(33):
        intercept = 26.6 + row * .58
        x = 13.8 + rng.uniform(-.2, .2)
        while x < 27.2:
            length = rng.uniform(1.3, 3.1)
            y = intercept - (x - 14) * .62
            stroke(face, [(x, y), (x + length * .48, y - length * .29 + rng.uniform(-.1, .1)),
                          (x + length, y - length * .62)],
                   rng.choice(('#e5dee8', '#f3edf5', '#cfc0d7') if front
                              else ('#b9a9c5', '#d2c5db', '#9884a6')),
                   rng.uniform(.27, .46), rng.uniform(.6, .91))
            x += length + rng.uniform(.12, .38)
    # Short crossing strokes and tiny paper gaps break up the pigment clusters.
    for _ in range(75):
        x, y = rng.uniform(14.4, 25.7), rng.uniform(27, 38)
        length = rng.uniform(.16, .75)
        stroke(face, [(x, y), (x + length, y + length * .46)],
               '#a280b4' if front else '#856899', rng.uniform(.11, .19),
               rng.uniform(.22, .43))
    for _ in range(60):
        x, y = rng.uniform(14.4, 25.7), rng.uniform(27, 38)
        stroke(face, [(x, y), (x + rng.uniform(.12, .38), y - .12)],
               '#fff3f5' if front else '#ded1e5', .15, .58)


def contour(parent, points, *, front):
    # Individually traced edges have varying pressure and a slight pencil wobble.
    ink = '#7d6091' if front else '#8a6c9e'
    for i, (start, end) in enumerate(zip(points, points[1:] + points[:1])):
        mx, my = (start[0] + end[0]) / 2, (start[1] + end[1]) / 2
        mid = (mx + (.055 if i % 2 else -.055), my + .06)
        stroke(parent, [start, mid, end], ink,
               (.62, .55, .67, .58)[i] if front else (.61, .53, .60, .56)[i],
               .92)
        if i in (0, 3):
            stroke(parent, [(start[0] + .13, start[1] + .15),
                            (mx + .09, my + .15),
                            (end[0] + .1, end[1] + .12)],
                   '#e4d5ed' if front else '#c9b6d4', .2, .66)


def refine(root):
    defs = root.find(NS + 'defs')
    mark = root.find('.//' + NS + "g[@id='facsimile-emblem']")
    if defs is None or mark is None:
        raise ValueError('Facsimile pencil treatment needs the source emblem and defs.')
    mark[:] = []
    mark.set('data-pencil-treatment', 'layered-pigment-and-offset-copy')

    # More exposed upper-left copy, with the lower-right copy visibly overlapping.
    rear = [(19.2, 27.7), (23.4, 30.5), (19.2, 33.3), (15, 30.5)]
    front = [(21, 31.2), (25.2, 34), (21, 36.8), (16.8, 34)]
    pigment(mark, defs, rear, 'facsimile-rear-pencil', front=False)
    contour(mark, rear, front=False)

    # A hand-hatched lower rim reads as the front layer's thickness at 100px.
    rim = [(16.8, 34), (21, 36.8), (25.2, 34),
           (25.2, 34.75), (21, 37.55), (16.8, 34.75)]
    pigment(mark, defs, rim, 'facsimile-rim-pencil', front=False)
    stroke(mark, [(16.8, 34.65), (21, 37.55), (25.2, 34.65)], '#9f87b3', .45, .84)
    pigment(mark, defs, front, 'facsimile-front-pencil', front=True)
    contour(mark, front, front=True)
