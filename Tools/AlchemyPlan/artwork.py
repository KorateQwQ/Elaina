"""Original vector drawings and compact material glyphs for the revised notebook."""


def svg(body):
    return '''<svg xmlns="http://www.w3.org/2000/svg" width="50" height="50" viewBox="0 0 50 50"><defs>
    <linearGradient id="gold" x2="0" y2="1"><stop stop-color="#f5cf87"/><stop offset=".5" stop-color="#d89a4b"/><stop offset="1" stop-color="#97502d"/></linearGradient>
    <linearGradient id="purple" x2="0" y2="1"><stop stop-color="#d9b5f1"/><stop offset=".5" stop-color="#a578cd"/><stop offset="1" stop-color="#69518e"/></linearGradient>
    <linearGradient id="blue" x2="0" y2="1"><stop stop-color="#c0eff3"/><stop offset=".5" stop-color="#79bed8"/><stop offset="1" stop-color="#4c739c"/></linearGradient>
    <linearGradient id="shimmer" x2="1" y2="1"><stop stop-color="#f5b9db"/><stop offset=".5" stop-color="#bca0ed"/><stop offset="1" stop-color="#8bd3d9"/></linearGradient>
    <linearGradient id="cream" x2="0" y2="1"><stop stop-color="#fff3d8"/><stop offset=".5" stop-color="#e9d4ac"/><stop offset="1" stop-color="#b3997b"/></linearGradient>
    <linearGradient id="red" x2="0" y2="1"><stop stop-color="#ec9da0"/><stop offset=".5" stop-color="#b8586b"/><stop offset="1" stop-color="#794255"/></linearGradient>
    </defs><g stroke="#3c2b48" stroke-width="1.15" stroke-linejoin="round" stroke-linecap="round">'''+body+'</g></svg>'


ART = {
 'AetherDropper': svg('''<path d="m14 34 5-8 11-11 6 6-11 11-8 5-5 1Z" fill="#d2e8ee"/><path d="m16 34 6-6 10-10 2 2-11 11-6 4Z" fill="url(#shimmer)" stroke="none"/><path d="m24 21 7-7 7 7-7 7Z" fill="#d5b16d"/><path d="m28 16 4-7c2-4 6-5 9-2s2 7-2 9l-6 5Z" fill="url(#purple)"/><path d="m34 9 2-1 2 1" fill="none" stroke="#f1dcff"/><path d="m13 39-2 3a2.5 2.5 0 0 0 5 0Z" fill="url(#shimmer)"/><path d="m27 20 7 7m-5-9 7 7" stroke="#fff0b9" stroke-width=".65"/><path d="m18 27 4-4m15 11 .5 1.7 1.7.5-1.7.5-.5 1.7-.5-1.7-1.7-.5 1.7-.5Z" fill="#e5d9f9" stroke="#9c7dba" stroke-width=".6"/>'''),
 'RevealingDust': svg('''<path d="m16 18-4 9c-5 12 0 18 12 18s17-6 12-18l-4-9Z" fill="url(#gold)"/><path d="m15 17 3-5 11-1 5 6-7 4h-6Z" fill="#d6b67d"/><path d="M15 19q10 3 19 0" fill="none" stroke="#aa759f" stroke-width="2"/><path d="m30 20 8 5m-6-5 2 9" stroke="#d7a3c7"/><path d="M17 32q7-8 14 0-7 8-14 0Z" fill="#f6e8bc"/><circle cx="24" cy="32" r="2.3" fill="#967bb3"/><path d="m18 38 2 2m-4-12 1-3" stroke="#f9dfa3"/><path d="m39 10 .7 2.5 2.5.7-2.5.7-.7 2.5-.7-2.5-2.5-.7 2.5-.7Z" fill="#fae7a5" stroke-width=".6"/><circle cx="8" cy="27" r="1" fill="#eacbe9" stroke="none"/><circle cx="39" cy="37" r="1" fill="#fae7a5" stroke="none"/>'''),
 'BottledRain': svg('''<path d="M19 9h12v8l7 8v15q0 5-5 5H17q-5 0-5-5V25l7-8Z" fill="#85b3cf" fill-opacity=".6"/><path d="M14 36q11-3 22 0v5q0 2-3 2H17q-3 0-3-2Z" fill="#699ed0" stroke="none"/><rect x="18" y="5" width="14" height="5" rx="1" fill="#cabfe0"/><path d="M20 7h9M16 27v8" stroke="#edf8ff" stroke-width="1.4"/><path d="M18 30a3.2 3.2 0 0 1 .5-6 5 5 0 0 1 9-1.8 4 4 0 0 1 5 6.6l-2.5 1.2Z" fill="#e6f2f8" stroke="#6f86ad"/><path d="m21 33-1 3m6-3-1 3m6-3-1 3" stroke="#a1e0f0" stroke-width="1.3"/><path d="M19 15h12" stroke="#b396d1" stroke-width="1.6"/>'''),
 'HoneyCroissant': svg('''<path d="M8 38C2 32 6 23 14 18c7-4 15-4 22 1 7 5 10 12 6 18-4 2-8 0-10-5l-1-3q-7-5-13 0l-1 4c-2 5-6 7-9 5Z" fill="url(#gold)"/><path d="m12 20 6 10m2-14 3 10m6-10-2 10m8-6-5 10" fill="none" stroke="#8f512f" stroke-width="1.3"/><path d="m13 21 5 8m3-11 2 7m5-7-1 7m7-3-4 7" stroke="#f8db9f" stroke-width="1.2"/><path d="M8 33q1-6 5-10" fill="none" stroke="#f9d994"/><path d="M31 18q7 1 9 8l-1 6q-2 1-2-2v-4q-2-4-6-5Z" fill="#ffd674" stroke="#b78027" stroke-width=".7"/><path d="m11 34 .5 1.5 1.5.5-1.5.5-.5 1.5-.5-1.5-1.5-.5 1.5-.5Z" fill="#fff0c3" stroke="none"/>'''),
 'BeefStew': svg('''<path d="M10 28H6q-3 0-3 4t7 3m30-7h4q3 0 3 4t-7 3" fill="none" stroke="#8f779d" stroke-width="2.5"/><path d="M9 27h32l-3 13q-2 6-13 6t-13-6Z" fill="url(#purple)"/><ellipse cx="25" cy="27" rx="16" ry="5" fill="#d4bfdc"/><ellipse cx="25" cy="27" rx="13" ry="3.8" fill="#a86643" stroke="#79513e"/><path d="m15 24 6-1 2 4-6 2Z" fill="#d6ad6d"/><path d="m26 25 6-2 3 4-7 2Z" fill="#98575b"/><path d="m23 28 3-2 3 2-3 2Z" fill="#9ab078" stroke="none"/><path d="m15 34 1 5m7-20c-5-5 4-5 0-10m8 10c-4-4 4-4 0-8" fill="none" stroke="#e2cfeb" stroke-width="1"/><path d="M14 39q11 6 22 0" fill="none" stroke="#dcc29d" stroke-width=".8"/>'''),
 'CaramelBrulee': svg('''<ellipse cx="25" cy="42" rx="20" ry="4" fill="#d9d1e6"/><path d="M9 26h32l-4 13q-1 4-12 4T13 39Z" fill="url(#cream)"/><ellipse cx="25" cy="26" rx="16" ry="7" fill="#efd899"/><ellipse cx="25" cy="25" rx="13.5" ry="5.5" fill="url(#gold)" stroke="#bc8a49"/><path d="m19 22 3 4-2 3m2-3 5-2 4 3 4-2m-8-1 1-3" fill="none" stroke="#91552e" stroke-width=".7"/><path d="M16 32l2 7m6-6 1 7m8-7-1 6" stroke="#bdae97" stroke-width=".75"/><path d="M13 26q1-3 5-4" fill="none" stroke="#ffe9b9" stroke-width="1.1"/><path d="m37 13 .6 2.1 2.1.6-2.1.6-.6 2.1-.6-2.1-2.1-.6 2.1-.6Z" fill="#efdb96" stroke-width=".5"/>'''),
 'MoonDew': svg('''<path d="M19 10h12v8l6 7v16q0 4-5 4H18q-5 0-5-4V25l6-7Z" fill="#b0d7e6" fill-opacity=".5"/><path d="M15 30q10-3 20 0v11q0 2-3 2H18q-3 0-3-2Z" fill="url(#blue)" stroke="none"/><path d="M15 30q10-3 20 0" fill="none" stroke="#e3f7fb"/><rect x="18" y="6" width="14" height="5" rx="1" fill="#b6b1d5"/><path d="M20 8h9M17 25v9" stroke="#f0f9ff"/><path d="M25 33a4.5 4.5 0 1 0 4 7 3.8 3.8 0 0 1-4-7Z" fill="#eaf5fb" stroke="#91b9d6" stroke-width=".6"/><circle cx="31" cy="34" r=".7" fill="#f2fbff" stroke="none"/>'''),
 'WarmResin': svg('''<path d="m10 32 5-10 10-4 11 6 5 13-5 6-22 1-6-6Z" fill="url(#gold)"/><path d="m15 23 3 8-4 11m11-23 4 10 11 7m-22-5 11-2 7 13" fill="none" stroke="#a86b33" stroke-width=".8"/><path d="m13 29 2-4 4-2m12 10 2 4" stroke="#ffdf91" stroke-width="1.4"/><path d="M25 18q-3-10 8-12 1 9-8 12Z" fill="#94aa77"/><path d="m25 18 5-8" stroke="#5e765c" stroke-width=".8"/><path d="m20 10 1 8" stroke="#a27b4c" stroke-width="2"/>'''),
 'Flour': svg('''<path d="m15 11 7-2 14 3-3 6 5 20q1 7-13 7T12 38l5-20Z" fill="url(#cream)"/><path d="m15 11 10 3 11-2M17 18h16" fill="none" stroke="#b59674"/><path d="M16 27h18v12H16Z" fill="#e2bd86" stroke="#b99c76"/><path d="M25 37V26m0 4-4-3m4 6-4-3m4 0 4-3m-4 6 4-3" fill="none" stroke="#89643e" stroke-width="1.1"/><path d="m15 23-1 12" stroke="#fff5dc"/>'''),
 'Sugar': svg('''<path d="m9 27 7-4 8 4v11l-8 5-7-5Z" fill="#ede9f0"/><path d="m24 29 9-5 9 5v10l-9 5-9-5Z" fill="#dad8e8"/><path d="m17 17 8-5 9 5v10l-9 5-8-5Z" fill="#f5eef2"/><path d="m17 17 8 5 9-5m-9 5v10m-16-5 7 4 8-4m-8 4v12m8-14 9 5 9-5m-9 5v10" fill="none" stroke="#b3a7c3" stroke-width=".7"/><path d="m20 17 5-3m3 13 3-2" stroke="#fff"/><circle cx="7" cy="43" r=".7" fill="#ded3eb" stroke="none"/>'''),
 'Egg': svg('''<path d="M25 6c-6 0-15 18-15 25 0 10 7 15 15 15s15-5 15-15C40 24 31 6 25 6Z" fill="url(#cream)"/><path d="M18 17q-5 8-5 14" fill="none" stroke="#fff7e4" stroke-width="2"/><path d="M17 39q9 6 17-2" fill="none" stroke="#bda58b" stroke-width=".8"/>'''),
 'Beef': svg('''<path d="M10 18c9-8 18-8 26-2 9 7 9 18 0 25-8 6-17 3-21 0C4 39 2 26 10 18Z" fill="#e7c6bd"/><path d="M12 20c7-6 15-8 22-2 8 6 8 16 0 21-6 4-12 2-17 0C8 38 6 26 12 20Z" fill="url(#red)"/><path d="M17 21q-4 5-3 11m6-13q-3 9 0 17m8-17q-6 8-1 18m7-13q-6 7-3 11" fill="none" stroke="#f3c8c2" stroke-width="1"/><ellipse cx="32" cy="32" rx="4.6" ry="3.8" fill="#ead8c4" stroke="#b89888"/><ellipse cx="32" cy="32" rx="2" ry="1.4" fill="#c09d8a" stroke="none"/>'''),
 'Potato': svg('''<path d="M13 17c7-4 19 0 23 10 5 12-8 20-18 16C5 39 2 24 13 17Z" fill="url(#gold)"/><path d="M31 12c8-3 15 4 13 12-1 5-5 8-8 8l-4-13Z" fill="#b99056"/><path d="m12 23 2-2m2 15 2 1m9-9 2 1m2 8 1-2m5-17 2 1" stroke="#826045" stroke-width="1.2"/><path d="M10 27q0-4 3-6" fill="none" stroke="#eecb91" stroke-width="1.2"/>'''),
 'ShimmerDroplet': svg('''<path d="M25 5c-3 9-14 19-14 27a14 14 0 0 0 28 0C39 24 28 14 25 5Z" fill="url(#shimmer)"/><path d="M18 23q-4 6-3 10" fill="none" stroke="#fff0fc" stroke-width="1.6"/><path d="m26 28 .9 3.1 3.1.9-3.1.9-.9 3.1-.9-3.1-3.1-.9 3.1-.9Z" fill="#f4ecff" stroke="none"/><circle cx="32" cy="38" r=".8" fill="#ddf7f8" stroke="none"/>''')
}

GLYPHS={
 'drop':'<path d="M25 6C21 15 12 23 12 32a13 13 0 0 0 26 0c0-9-9-17-13-26Z"/><path d="M19 24q-4 6-3 11" fill="none"/>',
 'star':'<path d="m25 7 4 12 13 1-10 8 3 13-10-7-11 7 4-13-10-8 13-1Z"/>',
 'herb':'<path d="M25 44V10m0 24C9 35 9 23 12 21c7 0 13 5 13 13Zm0-10c0-10 5-15 14-16 2 9-3 16-14 16Z"/>',
 'flower':'<path d="M24 44V24m0 14q-12 0-13-9 9-1 13 9Z"/><path d="M23 24q-8 3-8-3-9-5-3-10 0-8 7-6 7-6 10 1 9 0 6 8 5 8-3 9-2 8-9 1Z"/><circle cx="23" cy="15" r="4"/>',
 'mushroom':'<path d="M22 24h7l3 19H18Z"/><path d="M7 26C6 6 43 5 43 26q-18 6-36 0Z"/><circle cx="18" cy="18" r="3"/><circle cx="31" cy="17" r="2.5"/>',
 'block':'<path d="m9 16 15-8 17 8v24L24 46 9 38Z"/><path d="m9 16 15 8 17-8M24 24v22M15 25l5 3m9 5 6-2" fill="none"/>',
 'bone':'<path d="M15 10c-5-6-11 2-6 6-4 6 3 10 6 6l15 16c-3 7 5 10 8 4 7 1 8-7 1-9L23 17c5-5-1-11-8-7Z"/>',
 'chunk':'<path d="m10 17 10-7 17 7 5 15-10 10-18-5-8-11Z"/><path d="m14 20 8 6-5 8m5-8 12-5m-5 7 5 7" fill="none"/>',
 'crystal':'<path d="m23 5 12 14-5 25-16-9-3-18Z"/><path d="m23 5-3 19 10 20m-19-27 9 7 15-5" fill="none"/>',
 'root':'<path d="M24 8v22l-8 14m8-14 9 14m-9-17L12 15m12 8 11-10m-13 22-10 1m16-1 11 3" fill="none"/><path d="m12 15-4-6 9 4m18 0 5-7-10 6"/>',
 'lens':'<ellipse cx="25" cy="26" rx="17" ry="14"/><ellipse cx="25" cy="26" rx="11" ry="9"/><path d="M15 23q2-5 7-6" fill="none"/>',
 'fang':'<path d="M17 8q25 10 14 30l-11 7q8-15 0-22Z"/><path d="m22 18 5 10-3 8" fill="none"/>',
 'feather':'<path d="M11 42C10 20 23 8 41 7 42 24 31 38 11 42Z"/><path d="m8 45 25-28m-16 16-1-9m7 3 1-11m-1 11 10-1" fill="none"/>',
 'dust':'<path d="m15 30 10-10 11 11-7 12-17-2Z"/><path d="m14 13 .8 3 3 .8-3 .8-.8 3-.8-3-3-.8 3-.8Zm23-7 1 3 3 1-3 1-1 3-1-3-3-1 3-1Z"/><circle cx="8" cy="32" r="1.5"/><circle cx="39" cy="39" r="1.5"/>',
 'pane':'<path d="m11 13 25-5 3 30-25 5Z"/><path d="m17 18 12-3m-10 6 11-2" fill="none"/>',
 'bar':'<path d="m9 21 6-9h26l-3 20-29 4Z"/><path d="m9 21 27-3 5-6m-5 6 2 14" fill="none"/>',
 'cloud':'<path d="M12 36a8 8 0 0 1-1-16 10 10 0 0 1 18-5 9 9 0 0 1 12 14q0 8-12 8Z"/>',
 'wood':'<path d="m10 13 28 8-7 22L5 34Z"/><ellipse cx="34" cy="31" rx="8" ry="12"/><ellipse cx="34" cy="31" rx="4" ry="7"/><path d="m11 18 15 5m-17 3 13 3" fill="none"/>',
 'gel':'<path d="M9 30q0-9 8-9c1-14 18-14 21-1q9 4 5 15-4 9-18 9S4 40 9 30Z"/><path d="M15 28q0-4 4-5" fill="none"/>'
}


def glyph(kind,color):
    return '<svg xmlns="http://www.w3.org/2000/svg" width="50" height="50" viewBox="0 0 50 50"><g fill="'+color+'" stroke="#655071" stroke-width="1.1" stroke-linecap="round" stroke-linejoin="round">'+GLYPHS[kind]+'</g></svg>'
