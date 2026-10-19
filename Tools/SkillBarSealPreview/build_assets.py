"""Embed the live battle-bar textures into index.html so it also works from file://."""
import base64
import json
import pathlib
import re

here = pathlib.Path(__file__).resolve().parent
root = here.parents[1]
page = here / "index.html"
text = page.read_text(encoding="utf-8")

sources = {
    "rack": root / "ElainaModSkills/ElainaSkillUI/BattleBar/Rack.png",
    "ornaments": root / "ElainaModSkills/ElainaSkillUI/BattleBar/Ornaments.png",
}
for icon in sorted(set(re.findall(r"icon:'([^']+)'", text))):
    sources[icon] = root / "ElainaModSkills/Skills" / f"{icon}.png"

assets = {
    name: "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode("ascii")
    for name, path in sources.items()
}
payload = "/*ASSETS-BEGIN*/" + json.dumps(assets, separators=(",", ":")) + "/*ASSETS-END*/"
text, count = re.subn(r"/\*ASSETS-BEGIN\*/.*?/\*ASSETS-END\*/", lambda _: payload, text, flags=re.S)
if count != 1:
    raise SystemExit("index.html must contain exactly one ASSETS marker")
page.write_text(text, encoding="utf-8", newline="\n")
print(f"Embedded {len(assets)} textures into {page}")
