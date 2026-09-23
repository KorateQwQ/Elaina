"""Shared coloured-pencil treatment used by the notebook artwork."""
import colorsys
import copy
from urllib.parse import quote, unquote
import xml.etree.ElementTree as ET

NS = 'http://www.w3.org/2000/svg'
ET.register_namespace('', NS)
def tag(name): return '{' + NS + '}' + name
def element(name, attrs=None): return ET.Element(tag(name), attrs or {})

def pencil_color(color):
    if not color.startswith('#') or len(color) not in (4, 7): return color
    if len(color) == 4: color = '#' + ''.join(c * 2 for c in color[1:])
    rgb = [int(color[i:i+2], 16) / 255 for i in (1, 3, 5)]
    hue, light, sat = colorsys.rgb_to_hls(*rgb)
    if light > .91: return '#e8daef'
    if light < .16: return '#745586'
    if sat < .15:
        hue, sat = .758, .22
    else:
        sat = min(.51, max(.24, sat * .61))
    rgb = colorsys.hls_to_rgb(hue, .51 + light * .25, sat)
    return '#' + ''.join(f'{round(c * 255):02x}' for c in rgb)

def convert(uri, profile='soft'):
    if profile not in ('soft', 'hatched'):
        raise ValueError('Unknown pencil profile: ' + profile)
    hatched = profile == 'hatched'
    root = ET.fromstring(unquote(uri.split(',', 1)[1]))
    defs = root.find(tag('defs'))
    if defs is None:
        defs = element('defs')
        root.insert(0, defs)
    gradients = {}
    glow_ids = set()
    for grad in list(defs):
        kind = grad.tag.split('}')[-1]
        if kind not in ('linearGradient', 'radialGradient'): continue
        if kind == 'radialGradient': glow_ids.add(grad.get('id'))
        stops = grad.findall(tag('stop'))
        gradients[grad.get('id')] = pencil_color(stops[len(stops)//2].get('stop-color', '#c6a4df'))
    patterns = {}
    def pigment(color):
        if color not in patterns:
            ident = 'pencil-hatch-' + str(len(patterns))
            patterns[color] = ident
            step = '3.6' if hatched else '3.15'
            pattern = element('pattern', {'id': ident, 'width': step, 'height': step, 'patternUnits': 'userSpaceOnUse'})
            pattern.append(element('rect', {'width': step, 'height': step, 'fill': color}))
            pattern.append(element('path', {
                'd': 'M-.5 3.55 1.1 2.06 2.2 1.24 3.6-.5M3.1 4.1 4.1 3.1' if hatched else 'M-.5 3.1 3.15-.45M2.7 3.6 3.6 2.7',
                'fill': 'none', 'stroke': '#73518d' if hatched else '#785390',
                'stroke-width': '.58' if hatched else '.25', 'opacity': '.76' if hatched else '.42'}))
            pattern.append(element('path', {
                'd': 'M.05 3.6 1.5 2.3 2.5 1.35 3.6.2' if hatched else 'M.2 3.2 3.2.15',
                'fill': 'none', 'stroke': '#efdcf7', 'stroke-width': '.18' if hatched else '.16', 'opacity': '.4'}))
            defs.append(pattern)
        return 'url(#' + patterns[color] + ')'
    filter_xml = '''<filter xmlns="http://www.w3.org/2000/svg" id="pencil-tooth" x="-8%" y="-8%" width="116%" height="116%" color-interpolation-filters="sRGB">
      <feTurbulence type="fractalNoise" baseFrequency="1.35" numOctaves="3" seed="17" result="grain"/>
      <feDisplacementMap in="SourceGraphic" in2="grain" scale=".24" xChannelSelector="R" yChannelSelector="G" result="rough"/>
      <feColorMatrix in="grain" type="luminanceToAlpha"/>
      <feComponentTransfer><feFuncA type="linear" slope=".35" intercept=".65"/></feComponentTransfer>
      <feComposite in="rough" operator="in"/>
    </filter>'''
    defs.append(ET.fromstring(filter_xml))

    def walk(parent):
        for child in list(parent):
            name = child.tag.split('}')[-1]
            fill = child.get('fill', '')
            if fill.startswith('url(#') and fill[5:-1] in glow_ids:
                parent.remove(child)
                continue
            if name == 'ellipse' and float(child.get('ry', 99)) < 5 and float(child.get('cy', 0)) > 40:
                parent.remove(child)
                continue
            if name == 'g':
                walk(child)
            if fill.startswith('url(#') and fill[5:-1] in gradients:
                child.set('fill', pigment(gradients[fill[5:-1]]))
            elif fill.startswith('#'):
                color = pencil_color(fill)
                child.set('fill', color if color in ('#e8daef', '#745586') else pigment(color))
                if fill.lower() == '#e6e0ff': child.set('fill-opacity', '.55')
                if color == '#e8daef' and name == 'path' and not child.get('stroke'):
                    child.set('stroke', '#8b669f')
                    child.set('stroke-width', '.3')
            stroke = child.get('stroke', '')
            if stroke and stroke != 'none':
                bright = stroke.lower() in ('#fff', '#ffffff', '#ffd8ea', '#f0cf9c')
                child.set('stroke', '#e6d4ef' if bright else '#79558f')
                width = float(child.get('stroke-width', 1))
                child.set('stroke-width', str(round(max(.28, width * (.64 if bright else .6)), 3)))
                child.set('stroke-linecap', 'round')
                child.set('stroke-linejoin', 'round')
                if not bright and name != 'g' and width >= .8:
                    echo = copy.deepcopy(child)
                    echo.set('fill', 'none')
                    echo.set('stroke-width', '.28')
                    echo.set('opacity', '.4')
                    echo.set('transform', (echo.get('transform', '') + ' translate(.22 -.16)').strip())
                    parent.insert(list(parent).index(child) + 1, echo)
    # Definitions are not drawing: retain clips and apply hatching only to visible shapes.
    strokes = element('g', {'filter': 'url(#pencil-tooth)', 'stroke-linecap': 'round', 'stroke-linejoin': 'round'})
    for child in list(root):
        if child is not defs:
            root.remove(child)
            strokes.append(child)
    walk(strokes)
    root.append(strokes)
    return 'data:image/svg+xml,' + quote(ET.tostring(root, encoding='unicode'), safe="~!*'()-._")

