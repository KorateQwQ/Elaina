"""Small-size pencil treatment for the reviewed BottledRain artwork.

Applied after the shared pencil conversion so rain retains its pale pigment
instead of being mapped to the same violet as the background hatching.
The SVG original keeps its reviewed appearance; IDs identify pencil-only edits.
"""
import xml.etree.ElementTree as ET

NS = '{http://www.w3.org/2000/svg}'


def refine(root):
    defs = root.find(NS + 'defs')
    drawing = root.find(NS + 'g')
    interior = drawing.find(".//" + NS + "g[@clip-path='url(#inside)']")
    if interior is None:
        raise ValueError('BottledRain pencil artwork is missing its interior clip.')

    # Reserve open space between the cloud and a shallow pool for three drops.
    for parent in drawing.iter():
        for child in list(parent):
            ident = child.get('id')
            if ident in ('rain-strands', 'rain-flecks', 'rain-botanical'):
                parent.remove(child)
            elif ident == 'rain-water':
                child.set('d', 'M9 40c6-1.3 10.5-.8 16 .3 5-1.1 10-.9 17 .5v6H9Z')
            elif ident == 'rain-surface':
                child.set('d', 'M13 40.5c4-.8 8-.5 12 .3 4-.8 8-.6 12 .1')
                child.set('stroke', '#d9eeee')
                child.set('stroke-width', '.7')
                child.set('stroke-opacity', '.9')
            elif ident == 'rain-glass' and child.get('fill', '').startswith('url(#'):
                pattern_id = child.get('fill')[5:-1]
                pattern = defs.find(NS + "pattern[@id='" + pattern_id + "']")
                # Reduce competing marks on the glass, keeping the paper tooth.
                for line in pattern.findall(NS + 'path'):
                    line.set('opacity', '.24')
            elif ident == 'rain-glass':
                # The shared converter duplicates outlines for pencil echoes.
                child.attrib.pop('id', None)

    drops = ET.SubElement(interior, NS + 'g', {'id': 'pencil-rain-drops'})
    for x, y, scale in ((19, 32.5, 1), (24.4, 34.4, 1.06), (30, 32.1, 1)):
        drop = ET.SubElement(drops, NS + 'g', {
            'transform': f'translate({x} {y}) rotate(14) scale({scale})'})
        ET.SubElement(drop, NS + 'path', {
            'd': 'M0 0C-.3 1.1-1.5 2.1-1.5 3.1A1.5 1.5 0 0 0 1.5 3.1C1.5 2.1.3 1.1 0 0Z',
            'fill': '#cff4f3', 'stroke': '#506b97', 'stroke-width': '.55',
            'stroke-linejoin': 'round'})
        ET.SubElement(drop, NS + 'path', {
            'd': 'M-.25 1.7Q-.9 2.6-.85 3.1', 'fill': 'none',
            'stroke': '#fff8f3', 'stroke-width': '.6', 'stroke-linecap': 'round'})
