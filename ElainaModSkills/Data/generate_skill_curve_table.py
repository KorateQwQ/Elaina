from zipfile import ZipFile, ZIP_DEFLATED
from pathlib import Path
from datetime import datetime, timezone
from xml.sax.saxutils import escape

out = Path("SkillTable_Curve.xlsx")

skills = [
    {
        "name": "魔力飞弹",
        "func": "向鼠标方向发射一枚魔力飞弹",
        "cd": "0.5秒",
        "curve": "Linear",
        "s1": 30,
        "s18": 300,
        "current": 30,
        "source": "MagicMissileSkill.cs / MagicMissile.cs",
        "note": "线性成长",
    },
    {
        "name": "火焰射线",
        "func": "生成FireBall弹幕，持续输出",
        "cd": "0.1秒",
        "curve": "CustomBezier",
        "s1": 40,
        "s18": 650,
        "current": 150,
        "source": "FireLaserSkill.cs / FireBall.cs",
        "note": "自定义关键帧，可拖切线",
        "keys": [
            {"bossState": 1, "dps": 40, "inState": 0, "inDps": 0, "outState": 2, "outDps": 5},
            {"bossState": 7, "dps": 120, "inState": -2, "inDps": -5, "outState": 0, "outDps": 0},
            {"bossState": 8, "dps": 220, "inState": 0, "inDps": 0, "outState": 3, "outDps": 80},
            {"bossState": 18, "dps": 650, "inState": -4, "inDps": -100, "outState": 0, "outDps": 0},
        ],
    },
    {
        "name": "水球",
        "func": "水系球体技能，适合持续/范围命中",
        "cd": "未设置",
        "curve": "Linear",
        "s1": 80,
        "s18": 520,
        "current": 80,
        "source": "WaterBallSkill.cs / WaterBall.cs",
        "note": "范围/持续命中技能",
    },
]

boss_names = {
    1: "史莱姆王",
    2: "克眼",
    3: "世吞/克脑",
    4: "蜂王",
    5: "骷髅王",
    6: "鹿角怪",
    7: "肉山",
    8: "史莱姆王后",
    9: "双子魔眼",
    10: "毁灭者",
    11: "机械骷髅王",
    12: "世纪之花",
    13: "石巨人",
    14: "猪鲨",
    15: "光女",
    16: "跳过",
    17: "拜月教徒",
    18: "月总",
}


def clamp01(value):
    return max(0.0, min(1.0, value))


def lerp(start, end, amount):
    return start + (end - start) * amount


def smoothstep(amount):
    amount = clamp01(amount)
    return amount * amount * (3.0 - 2.0 * amount)


def cubic(a, b, c, d, amount):
    amount = clamp01(amount)
    inv = 1.0 - amount
    return inv * inv * inv * a + 3.0 * inv * inv * amount * b + 3.0 * inv * amount * amount * c + amount * amount * amount * d


def calc_custom_dps(skill, state):
    keys = sorted(skill.get("keys", []), key=lambda item: item["bossState"])
    if len(keys) < 2:
        amount = clamp01((state - 1) / 17)
        return round(lerp(skill["s1"], skill["s18"], amount))

    if state <= keys[0]["bossState"]:
        return round(keys[0]["dps"])
    if state >= keys[-1]["bossState"]:
        return round(keys[-1]["dps"])

    for index in range(len(keys) - 1):
        start = keys[index]
        end = keys[index + 1]
        if state < start["bossState"] or state > end["bossState"]:
            continue

        lo = 0.0
        hi = 1.0
        for _ in range(28):
            mid = (lo + hi) / 2.0
            x = cubic(start["bossState"], start["bossState"] + start["outState"], end["bossState"] + end["inState"], end["bossState"], mid)
            if x < state:
                lo = mid
            else:
                hi = mid

        amount = (lo + hi) / 2.0
        dps = cubic(start["dps"], start["dps"] + start["outDps"], end["dps"] + end["inDps"], end["dps"], amount)
        return round(dps)

    return 0


def calc_dps(skill, state):
    if skill["curve"] == "CustomBezier":
        return calc_custom_dps(skill, state)

    amount = clamp01((state - 1) / 17)
    if skill["curve"] == "SmoothStep":
        amount = smoothstep(amount)
    return round(lerp(skill["s1"], skill["s18"], amount))


def col_name(index):
    result = ""
    while index:
        index, rem = divmod(index - 1, 26)
        result = chr(65 + rem) + result
    return result


def inline_cell(ref, style, value):
    return f'<c r="{ref}" s="{style}" t="inlineStr"><is><t>{escape(str(value))}</t></is></c>'


def number_cell(ref, style, value):
    return f'<c r="{ref}" s="{style}"><v>{value}</v></c>'


def build_sheet(rows, widths, drawing_id=""):
    xml_rows = []
    for row_index, row in enumerate(rows, 1):
        cells = []
        for col_index, value in enumerate(row, 1):
            ref = f"{col_name(col_index)}{row_index}"
            if row_index == 1:
                cells.append(inline_cell(ref, 1, value))
            elif isinstance(value, (int, float)):
                cells.append(number_cell(ref, 3, value))
            else:
                cells.append(inline_cell(ref, 2, value))
        height = 28 if row_index == 1 else 34
        xml_rows.append(f'<row r="{row_index}" ht="{height}" customHeight="1">' + "".join(cells) + "</row>")

    cols = "".join(
        f'<col min="{index}" max="{index}" width="{width}" customWidth="1"/>'
        for index, width in enumerate(widths, 1)
    )
    end_ref = f"{col_name(len(rows[0]))}{len(rows)}"
    sheet_view = (
        '<sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" '
        'activePane="bottomLeft" state="frozen"/><selection pane="bottomLeft" activeCell="A2" '
        'sqref="A2"/></sheetView></sheetViews>'
    )
    drawing_xml = f'<drawing r:id="{drawing_id}"/>' if drawing_id else ""
    worksheet_namespace = (
        'xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" '
        'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"'
        if drawing_id else
        'xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"'
    )
    return (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        f'<worksheet {worksheet_namespace}>'
        f'<dimension ref="A1:{end_ref}"/>{sheet_view}<sheetFormatPr defaultRowHeight="18"/>{cols}'
        f'<sheetData>{"".join(xml_rows)}</sheetData><autoFilter ref="A1:{end_ref}"/>{drawing_xml}</worksheet>'
    )


def build_chart_xml(skill_count):
    series_xml = []
    colors = ["4472C4", "ED7D31", "70AD47", "A5A5A5", "FFC000"]
    for index in range(skill_count):
        col = col_name(index + 3)
        color = colors[index % len(colors)]
        series_xml.append(
            f'<c:ser><c:idx val="{index}"/><c:order val="{index}"/>'
            f'<c:tx><c:strRef><c:f>曲线预览!${col}$1</c:f></c:strRef></c:tx>'
            f'<c:spPr><a:ln w="28575"><a:solidFill><a:srgbClr val="{color}"/></a:solidFill></a:ln></c:spPr>'
            f'<c:cat><c:numRef><c:f>曲线预览!$A$2:$A$19</c:f></c:numRef></c:cat>'
            f'<c:val><c:numRef><c:f>曲线预览!${col}$2:${col}$19</c:f></c:numRef></c:val>'
            '</c:ser>'
        )
    return (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<c:chartSpace xmlns:c="http://schemas.openxmlformats.org/drawingml/2006/chart" '
        'xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" '
        'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">'
        '<c:chart><c:title><c:tx><c:rich><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>技能 DPS 曲线</a:t></a:r></a:p></c:rich></c:tx></c:title>'
        '<c:plotArea><c:layout/><c:lineChart><c:grouping val="standard"/>'
        f'{"".join(series_xml)}'
        '<c:axId val="123456"/><c:axId val="654321"/></c:lineChart>'
        '<c:catAx><c:axId val="123456"/><c:scaling><c:orientation val="minMax"/></c:scaling><c:delete val="0"/><c:axPos val="b"/><c:numFmt formatCode="General" sourceLinked="1"/><c:majorTickMark val="out"/><c:minorTickMark val="none"/><c:tickLblPos val="nextTo"/><c:crossAx val="654321"/><c:crosses val="autoZero"/><c:auto val="1"/><c:lblAlgn val="ctr"/><c:lblOffset val="100"/></c:catAx>'
        '<c:valAx><c:axId val="654321"/><c:scaling><c:orientation val="minMax"/></c:scaling><c:delete val="0"/><c:axPos val="l"/><c:majorGridlines/><c:numFmt formatCode="General" sourceLinked="1"/><c:majorTickMark val="out"/><c:minorTickMark val="none"/><c:tickLblPos val="nextTo"/><c:crossAx val="123456"/><c:crosses val="autoZero"/><c:crossBetween val="between"/></c:valAx>'
        '</c:plotArea><c:legend><c:legendPos val="r"/><c:layout/></c:legend><c:plotVisOnly val="1"/></c:chart></c:chartSpace>'
    )


chart_xml = build_chart_xml(len(skills))
drawing_xml = (
    '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
    '<xdr:wsDr xmlns:xdr="http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing" '
    'xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" '
    'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">'
    '<xdr:twoCellAnchor><xdr:from><xdr:col>5</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>1</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from>'
    '<xdr:to><xdr:col>16</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>20</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:to>'
    '<xdr:graphicFrame macro=""><xdr:nvGraphicFramePr><xdr:cNvPr id="2" name="技能 DPS 曲线"/><xdr:cNvGraphicFramePr/></xdr:nvGraphicFramePr>'
    '<xdr:xfrm><a:off x="0" y="0"/><a:ext cx="0" cy="0"/></xdr:xfrm><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/chart">'
    '<c:chart xmlns:c="http://schemas.openxmlformats.org/drawingml/2006/chart" r:id="rId1"/></a:graphicData></a:graphic></xdr:graphicFrame><xdr:clientData/></xdr:twoCellAnchor></xdr:wsDr>'
)

sheet1_rows = [["技能名称", "功能", "冷却", "曲线类型", "State1 DPS", "State18 DPS", "当前DPS设定", "代码来源", "备注"]]
for skill in skills:
    sheet1_rows.append([
        skill["name"],
        skill["func"],
        skill["cd"],
        skill["curve"],
        skill["s1"],
        skill["s18"],
        skill["current"],
        skill["source"],
        skill["note"],
    ])

sheet2_rows = [["State", "Boss阶段"] + [skill["name"] for skill in skills]]
for state in range(1, 19):
    sheet2_rows.append([state, boss_names[state]] + [calc_dps(skill, state) for skill in skills])

now = datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")

content_types = (
    '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
    '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
    '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>'
    '<Default Extension="xml" ContentType="application/xml"/>'
    '<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>'
    '<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>'
    '<Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>'
    '<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>'
    '<Override PartName="/xl/drawings/drawing1.xml" ContentType="application/vnd.openxmlformats-officedocument.drawing+xml"/>'
    '<Override PartName="/xl/charts/chart1.xml" ContentType="application/vnd.openxmlformats-officedocument.drawingml.chart+xml"/>'
    '<Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>'
    '<Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>'
    '</Types>'
)

styles = (
    '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
    '<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">'
    '<fonts count="2"><font><sz val="11"/><name val="Microsoft YaHei"/></font><font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Microsoft YaHei"/></font></fonts>'
    '<fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF4472C4"/><bgColor indexed="64"/></patternFill></fill></fills>'
    '<borders count="2"><border><left/><right/><top/><bottom/><diagonal/></border><border><left style="thin"><color rgb="FFD9E1F2"/></left><right style="thin"><color rgb="FFD9E1F2"/></right><top style="thin"><color rgb="FFD9E1F2"/></top><bottom style="thin"><color rgb="FFD9E1F2"/></bottom><diagonal/></border></borders>'
    '<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>'
    '<cellXfs count="4"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf><xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf><xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyBorder="1" applyAlignment="1"><alignment horizontal="center" vertical="center"/></xf></cellXfs>'
    '<cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles><dxfs count="0"/><tableStyles count="0" defaultTableStyle="TableStyleMedium2" defaultPivotStyle="PivotStyleLight16"/></styleSheet>'
)

files = {
    "[Content_Types].xml": content_types,
    "_rels/.rels": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/><Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/></Relationships>',
    "xl/workbook.xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="技能表" sheetId="1" r:id="rId1"/><sheet name="曲线预览" sheetId="2" r:id="rId2"/></sheets></workbook>',
    "xl/_rels/workbook.xml.rels": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/><Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>',
    "xl/worksheets/sheet1.xml": build_sheet(sheet1_rows, [14, 38, 10, 22, 12, 13, 13, 34, 36]),
    "xl/worksheets/sheet2.xml": build_sheet(sheet2_rows, [9, 16, 14, 14, 14], "rId1"),
    "xl/worksheets/_rels/sheet2.xml.rels": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing" Target="../drawings/drawing1.xml"/></Relationships>',
    "xl/drawings/drawing1.xml": drawing_xml,
    "xl/drawings/_rels/drawing1.xml.rels": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart" Target="../charts/chart1.xml"/></Relationships>',
    "xl/charts/chart1.xml": chart_xml,
    "xl/styles.xml": styles,
    "docProps/core.xml": f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><dc:title>伊蕾娜技能曲线表</dc:title><dc:creator>Python</dc:creator><cp:lastModifiedBy>Python</cp:lastModifiedBy><dcterms:created xsi:type="dcterms:W3CDTF">{now}</dcterms:created><dcterms:modified xsi:type="dcterms:W3CDTF">{now}</dcterms:modified></cp:coreProperties>',
    "docProps/app.xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes"><Application>Python</Application><DocSecurity>0</DocSecurity><ScaleCrop>false</ScaleCrop><HeadingPairs><vt:vector size="2" baseType="variant"><vt:variant><vt:lpstr>Worksheets</vt:lpstr></vt:variant><vt:variant><vt:i4>2</vt:i4></vt:variant></vt:vector></HeadingPairs><TitlesOfParts><vt:vector size="2" baseType="lpstr"><vt:lpstr>技能表</vt:lpstr><vt:lpstr>曲线预览</vt:lpstr></vt:vector></TitlesOfParts><Company></Company><LinksUpToDate>false</LinksUpToDate><SharedDoc>false</SharedDoc><HyperlinksChanged>false</HyperlinksChanged><AppVersion>16.0300</AppVersion></Properties>',
}

with ZipFile(out, "w", ZIP_DEFLATED) as zf:
    for name, content in files.items():
        zf.writestr(name, content)

print(out.resolve())
print(out.stat().st_size)