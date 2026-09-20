// Import reference data; skill artwork is always loaded directly from the project.
const fs = require('node:fs');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../NewUIExample/elaina-battle-eight-skills.html'), 'utf8').replace(/\r\n/g, '\n');
// Restricted literal reader: never execute scripts from the reference document.
function declaration(name) {
  const start = source.indexOf(`const ${name}=`);
  if (start < 0) throw new Error(`Missing ${name}`);
  let pos = start + name.length + 7;
  function whitespace() {
    while (true) {
      while (/\s/.test(source[pos] || '') && pos < source.length) pos++;
      if (source.slice(pos, pos + 2) === '//') { pos = source.indexOf('\n', pos); if (pos < 0) throw Error('Unterminated comment'); }
      else if (source.slice(pos, pos + 2) === '/*') { const end = source.indexOf('*/', pos + 2); if (end < 0) throw Error('Unterminated comment'); pos = end + 2; }
      else return;
    }
  }
  function value() {
    whitespace(); const c = source[pos++];
    if (c === "'" || c === '"') {
      let result = '';
      while (pos < source.length) { const ch = source[pos++]; if (ch === c) return result;
        if (ch === '\\') { const escaped = source[pos++]; const escapes = {n:'\n',r:'\r',t:'\t','\\':'\\',"'":"'",'"':'"'}; if (!(escaped in escapes)) throw Error('Unsupported escape'); result += escapes[escaped]; }
        else result += ch;
      }
      throw Error('Unterminated string');
    }
    if (c === '[' || c === '{') {
      const result = c === '[' ? [] : Object.create(null), end = c === '[' ? ']' : '}';
      whitespace();
      while (source[pos] !== end) {
        if (c === '[') result.push(value());
        else {
          whitespace(); let key;
          if (source[pos] === "'" || source[pos] === '"') key = value();
          else { const match = /^[A-Za-z_$][\w$]*/.exec(source.slice(pos)); if (!match) throw Error('Invalid key'); key = match[0]; pos += key.length; }
          whitespace(); if (source[pos++] !== ':') throw Error('Expected colon'); result[key] = value();
        }
        whitespace(); if (source[pos] === end) break;
        if (source[pos++] !== ',') throw Error('Expected comma'); whitespace();
      }
      pos++; return result;
    }
    pos--; const token = /^(?:-?(?:\d+(?:\.\d*)?|\.\d+)|true\b|false\b|null\b)/.exec(source.slice(pos));
    if (!token) throw Error(`Nonliteral expression at ${pos}`);
    pos += token[0].length;
    return token[0] === 'null' ? null : token[0] === 'true' ? true : token[0] === 'false' ? false : Number(token[0]);
  }
  return value();
}
const skills = declaration('skills');
const families = declaration('GROWTH_TYPES');
const growth = declaration('SKILL_GROWTH');
const activation = declaration('ACTIVATION_REQUIREMENTS');
const initial = new Set(['origin', 'sense', 'ember']);
const output = path.join(__dirname, 'Assets');
const skillIcons = {
  origin: 'AshenWitchSkill', sense: 'WindBladeSkill', moon: 'MagicMissileSkill',
  echo: 'MultiMissileSkill', ember: 'FireBurstSkill', vortex: 'FireTornadoSkill',
  fireflower: 'FireLaserSkill', ice: 'IceConeSkill', water: 'WaterBallSkill',
  ritual: 'AshenWitchSkill', hiddenA: 'MultiMissileSkill', hiddenB: 'WaterLaserSKill'
};
for (const skill of skills) {
  const icon = skillIcons[skill.id];
  if (!icon || !fs.existsSync(path.join(__dirname, '../../Icon', `${icon}.png`)))
    throw new Error(`Missing project skill icon for ${skill.id}`);
}
const mapped = skills.map(s => ({
  id: s.id, name: s.name, icon: skillIcons[s.id], x: s.x, y: s.y,
  cost: s.cost, prereqs: s.parents, initial: initial.has(s.id), secret: !!s.reveal,
  discovery: s.reveal || [], hint: s.clue || '', desc: s.desc,
  type: s.type, mana: s.mana, cooldown: s.cooldown,
  growth: families[growth[s.id]], toggleable: s.id === 'sense',
  activationRequires: activation[s.id] || [], effect: s.effect || null
}));
const ids = new Set(mapped.map(s => s.id));
if (ids.size !== mapped.length) throw Error('Duplicate skill id');
for (const skill of mapped) {
  if (!skill.growth || !skill.growth.costs.every(c => Number.isInteger(c) && c >= 0)) throw Error(`Invalid growth: ${skill.id}`);
  for (const id of [...skill.prereqs, ...skill.discovery, ...skill.activationRequires]) if (!ids.has(id)) throw Error(`Unknown relation: ${id}`);
}
fs.writeFileSync(path.join(output, 'Skills.json'), JSON.stringify(mapped, null, 2) + '\n');
console.log(`Imported ${mapped.length} skills using existing project skill icons.`);
