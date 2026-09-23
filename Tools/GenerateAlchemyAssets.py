from pathlib import Path


OUT = Path("ElainaModAlchemy/item/Assets")

potions = [
    ("ManaElixir", "#44c9ee", '<path d="M21 30l1.5 3.5L26 35l-3.5 1.5L21 40l-1.5-3.5L16 35l3.5-1.5z" fill="#e9fbff"/><circle cx="29" cy="39" r="1.2" fill="#fff"/>'),
    ("Painkiller", "#e87d94", '<path d="M16 36q5-8 10 0t10 0" fill="none" stroke="#fff0cb" stroke-width="2"/><path d="M25 30v12" stroke="#fff0cb" stroke-width="1.5"/>'),
    ("BloodthirstPotion", "#d74356", '<path d="M25 29c-2 4-6 7-6 11a6 6 0 0 0 12 0c0-4-4-7-6-11z" fill="#ffb1a3"/><path d="M25 35v8" stroke="#fff" stroke-width="1.2"/>'),
    ("StarPowerPotion", "#674bd3", '<path d="M25 28l2.2 5.5 5.8.4-4.5 3.7 1.5 5.6-5-3.1-5 3.1 1.5-5.6-4.5-3.7 5.8-.4z" fill="#fff3aa"/><circle cx="33" cy="31" r="1" fill="#fff"/>'),
    ("ConcentrationPotion", "#53bd9a", '<circle cx="25" cy="37" r="6" fill="none" stroke="#fff6d7" stroke-width="1.5"/><circle cx="25" cy="37" r="2" fill="#fff6d7"/><path d="M25 27v4m-10 6h4m12 0h4" stroke="#fff6d7" stroke-width="1.3"/>'),
    ("ResonancePotion", "#bc6cae", '<path d="M17 35h4l3-5 4 14 3-9h5" fill="none" stroke="#fff1df" stroke-width="1.8" stroke-linejoin="round"/><circle cx="17" cy="35" r="1" fill="#fff"/>'),
    ("FeatherPotion", "#62aee0", '<path d="M18 41q1-14 15-15-1 14-15 15zm1-1 10-10m-6 6 1-5m1 1 4-1" fill="#f6f2df" stroke="#557084" stroke-width="1" stroke-linejoin="round"/>'),
    ("IsolationPotion", "#b5d46b", '<path d="M18 37h14m-12-4h10m-8-4h6" stroke="#fbffe1" stroke-width="2" stroke-linecap="round"/><path d="m25 28 1.2 2.5 2.8.4-2 2 .5 2.8-2.5-1.3-2.5 1.3.5-2.8-2-2 2.8-.4z" fill="#fff9b5"/>'),
    ("RaincloudBottle", "#64b8c9", '<path d="M19 35a4 4 0 0 1 2-7 6 6 0 0 1 11 1 4 4 0 0 1 1 8H20" fill="#ecfbff"/><path d="M22 39l-1 3m7-3-1 3m7-3-1 3" stroke="#d5f7ff" stroke-width="1.6"/>'),
]

icons = {
    "BroomWax": ("#d5a953", '<path d="M13 35 36 13" stroke="#70492f" stroke-width="3" stroke-linecap="round"/><path d="M11 34q-3 5 4 7l5-6-5-5z" fill="#d99c58" stroke="#70492f" stroke-width="1.2"/><path d="m32 16 4 4m-7-1 4 4" stroke="#f2d690" stroke-width="1.5"/>'),
    "ReturnInk": ("#5865bf", '<path d="M20 13h10v6l5 5v16H15V24l5-5z" fill="#e8d5a8" stroke="#453548" stroke-width="1.5"/><path d="M18 32q6-8 12 0t-2 6q-5 1-5-3 0-2 2-2" fill="none" stroke="#5553a4" stroke-width="2"/><path d="M20 13h10v6H20z" fill="#c68f61" stroke="#453548" stroke-width="1.2"/>'),
    "HerbalistDew": ("#7bbb6e", '<path d="M25 42V19m0 13q-12 0-12-11 11-1 12 11zm0-7q0-11 12-13 1 11-12 13z" fill="#68aa63" stroke="#315841" stroke-width="1.5"/><circle cx="25" cy="15" r="4" fill="#d4f29a" stroke="#668142" stroke-width="1"/><path d="M22 15h6m-3-3v6" stroke="#fffbd1" stroke-width="1"/>'),
    "SilentIncense": ("#a780ac", '<path d="M17 31h16l-2 12H19z" fill="#af7757" stroke="#523c43" stroke-width="1.5"/><path d="M25 31V17m0 8q-7-5-3-10 5 2 3 10zm1-4q1-7 7-7 0 6-7 7z" fill="none" stroke="#ead8f0" stroke-width="2" stroke-linecap="round"/><path d="M20 35h10" stroke="#edbb83" stroke-width="1"/>'),
    "StoredManaCandy": ("#f09ab7", '<path d="m17 19 5 3h6l5-3 4 6-5 5v5l3 5-7 3-3-5h-3l-3 5-7-3 3-5v-5l-5-5z" fill="#ee83af" stroke="#713d65" stroke-width="1.5"/><path d="m25 24 1.5 4 4 .5-3 2.5 1 4-3.5-2.2-3.5 2.2 1-4-3-2.5 4-.5z" fill="#fff0b0"/>'),
    "TracePowder": ("#ddbd72", '<path d="M14 33 33 14l5 5-19 19-5 1z" fill="#d7c8a8" stroke="#544557" stroke-width="1.4"/><path d="m29 18 5 5m-9-1 5 5m-9-1 5 5" stroke="#766d77" stroke-width="1"/><circle cx="15" cy="17" r="2" fill="#ffe795"/><circle cx="35" cy="35" r="2" fill="#ffe795"/><path d="m35 12 1 2 2 1-2 1-1 2-1-2-2-1 2-1z" fill="#fff6c7"/>'),
    "MoonlightDecoy": ("#b6a1de", '<path d="M25 12c-9 5-12 17-6 26 4 6 13 6 18 2-9 1-15-7-13-16 1-5 5-9 10-10-3-3-6-4-9-2z" fill="#efe2ff" stroke="#534365" stroke-width="1.4"/><path d="m23 29 2-3 2 3 4 1-4 2-2 4-2-4-4-2z" fill="#f5d888"/><circle cx="34" cy="20" r="1" fill="#fff"/>'),
    "ShimmerExtractor": ("#72c9cb", '<path d="M18 11h14v5l-3 4v12l5 5v4H15v-4l5-5V20l-2-4z" fill="#d9f0eb" fill-opacity=".85" stroke="#334d5b" stroke-width="1.6"/><path d="M20 29q5-3 10 0v8H20z" fill="#d5a8e8"/><path d="M19 15h12M20 38h12" stroke="#718e91" stroke-width="1.5"/><path d="m25 29 1 2 2 .4-2 1-1 2-.8-2-2-.8 2-.6z" fill="#fff3c1"/>'),
    "AshenFacsimileDust": ("#aaa6ad", '<path d="M11 34 29 14l9 8-18 20-9-3z" fill="#b9b4b6" stroke="#504753" stroke-width="1.4"/><path d="m17 33 17-14m-13 17 17-14" stroke="#ede2d4" stroke-width="1"/><circle cx="13" cy="13" r="2" fill="#f2d998"/><circle cx="37" cy="37" r="2" fill="#dcabd2"/><path d="m30 11 1 3 3 1-3 1-1 3-1-3-3-1 3-1z" fill="#fff2cc"/>'),
    "MoonDew": ("#85bcec", '<path d="M25 12c-4 7-12 16-12 22a12 12 0 0 0 24 0c0-6-8-15-12-22z" fill="#a8dcf3" stroke="#37566b" stroke-width="1.5"/><path d="M18 34a7 7 0 0 0 10 7" fill="none" stroke="#effcff" stroke-width="2"/><path d="m28 21 1.5 3 3.5.5-2.5 2.5.5 3.5-3-1.8-3 1.8.5-3.5-2.5-2.5 3.5-.5z" fill="#fff3ba"/>'),
    "WarmFragrantResin": ("#dbad66", '<path d="M25 11c-2 8-10 13-10 22a10 10 0 0 0 20 0c0-9-8-14-10-22z" fill="#e4b25c" stroke="#65472f" stroke-width="1.5"/><path d="M20 34q1 7 8 7" fill="none" stroke="#fff0bd" stroke-width="2"/><circle cx="24" cy="28" r="2" fill="#f7d88c"/>'),
    "EchoPollen": ("#ce9cce", '<path d="M25 39V25m0 7q-11 0-11-9 9-1 11 9zm0-5q1-9 10-10 1 8-10 10z" fill="#72a96c" stroke="#425744" stroke-width="1.3"/><circle cx="25" cy="18" r="5" fill="#e7acd9" stroke="#704d72" stroke-width="1.3"/><circle cx="25" cy="18" r="2" fill="#fff1b7"/><path d="M15 16q-4 2 0 4m20-4q4 2 0 4" fill="none" stroke="#e7c8ef" stroke-width="1.2"/>'),
    "ShimmerSediment": ("#bf8cda", '<path d="m12 36 7-18 7 10 6-15 7 23z" fill="#b9a8ca" stroke="#50435e" stroke-width="1.5" stroke-linejoin="round"/><path d="m19 18 2 10-9 8m14-8 6-15 0 23m-6 0 14-10" fill="none" stroke="#f3d8ff" stroke-width="1.2"/><circle cx="18" cy="34" r="1" fill="#fff5bc"/>'),
}


def potion_svg(color, motif):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="50" height="50" viewBox="0 0 50 50">
<defs><linearGradient id="glass" x2="0" y2="1"><stop stop-color="#fff" stop-opacity=".8"/><stop offset="1" stop-color="#b6d5dc" stop-opacity=".45"/></linearGradient><linearGradient id="liquid" x2="0" y2="1"><stop stop-color="{color}"/><stop offset="1" stop-color="{color}" stop-opacity=".72"/></linearGradient><clipPath id="b"><path d="M20 14h10v7q0 2 5 7a14 14 0 1 1-20 0q5-5 5-7z"/></clipPath></defs>
<path d="M20 14h10v7q0 2 5 7a14 14 0 1 1-20 0q5-5 5-7z" fill="url(#glass)" stroke="#42384f" stroke-width="1.5"/>
<g clip-path="url(#b)"><path d="M12 32q6-3 13 0t13 0v17H12z" fill="url(#liquid)"/><path d="M12 32q6-3 13 0t13 0" fill="none" stroke="#fff4df" stroke-opacity=".8" stroke-width="1.2"/>{motif}</g>
<path d="M18 30q1-5 4-7" fill="none" stroke="#fff" stroke-width="1.6" stroke-linecap="round"/><path d="M20 14h10" stroke="#fff" stroke-opacity=".8" stroke-width="1"/><rect x="19" y="10" width="12" height="5" rx="1.5" fill="#b77b55" stroke="#533c37" stroke-width="1.2"/><path d="M25 17h1" stroke="#f497c0" stroke-width="3"/><path d="M20 13C15 8 12 12 15 15l9 2-4-4m7 0c5-5 8-1 5 2l-8 2 4-4" fill="#f497c0" stroke="#87445e" stroke-width=".8" stroke-linejoin="round"/><path d="M24 15q1-2 2 0t-2 0" fill="#ffe3ef"/></svg>'''


def icon_svg(color, art):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="50" height="50" viewBox="0 0 50 50"><defs><radialGradient id="bg"><stop stop-color="{color}" stop-opacity=".24"/><stop offset="1" stop-color="{color}" stop-opacity="0"/></radialGradient></defs><circle cx="25" cy="27" r="21" fill="url(#bg)"/>{art}</svg>'''


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, color, motif in potions:
        (OUT / f"{name}.svg").write_text(potion_svg(color, motif), encoding="utf-8")
    for name, (color, art) in icons.items():
        (OUT / f"{name}.svg").write_text(icon_svg(color, art), encoding="utf-8")
    print(f"Wrote {len(potions) + len(icons)} SVG icons to {OUT}")


if __name__ == "__main__":
    main()
