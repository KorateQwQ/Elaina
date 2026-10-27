"""Read-only balance audit. Writes only audit-results.json next to this script.

TMOD reader follows the installed tModLoader TmodFile.Read/GetStream format.
The temporary C# probe invokes the packed KL helper and reads regeneration getter IL.
"""
from pathlib import Path
import hashlib
import io
import json
import re
import struct
import subprocess
import sys
import tempfile
import zlib

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent))
import server


def git_text(root, name):
    return subprocess.check_output(["git", "show", "HEAD:" + name], cwd=root).decode("utf-8-sig")


def nodes(source):
    source = re.sub(r"/\*.*?\*/|//[^\r\n]*", "", source, flags=re.S)
    return [float(x.strip().rstrip("fF")) for x in re.search(r"stateDps\s*=\s*\[([^\]]+)\]", source)[1].strip().rstrip(",").split(",")]


def baseline(curve, level):
    state = min(max(level / 5, 0), len(curve) - 1)
    lo = int(state)
    return curve[lo] + (curve[min(lo + 1, len(curve) - 1)] - curve[lo]) * (state - lo)


def read_tmod(path):
    stream = io.BytesIO(path.read_bytes())
    def integer():
        return struct.unpack("<i", stream.read(4))[0]
    def string():
        length = shift = 0
        while True:
            byte = stream.read(1)[0]
            length |= (byte & 127) << shift
            if byte < 128:
                break
            shift += 7
        return stream.read(length).decode("utf-8")
    assert stream.read(4) == b"TMOD"
    loader = string()
    stream.read(20 + 256)
    length = integer()
    assert len(stream.getbuffer()) - stream.tell() == length
    name, version = string(), string()
    entries = [(string(), integer(), integer()) for _ in range(integer())]
    files = {}
    for filename, size, compressed in entries:
        data = stream.read(compressed)
        if filename.endswith(".dll") or filename == "ElainaModSkills/Data/SkillBalance.json":
            value = zlib.decompress(data, -15) if size != compressed else data
            assert len(value) == size
            files[filename] = value
    return {"name": name, "version": version, "loader": loader}, files


def main():
    old = json.loads(git_text(ROOT, "ElainaModSkills/Data/SkillBalance.json"))["configuration"]
    current_bytes = server.CONFIG_PATH.read_bytes()
    current = json.loads(current_bytes.decode("utf-8-sig"))["configuration"]
    old_curve = nodes(git_text(ROOT.parent / "KL", "Utils/KLDpsHelper.cs"))
    curve = server.read_dps_curve()["values"]
    magic_source = (ROOT / "ElainaAttribute/ElainaMagicAttributes.cs").read_text(encoding="utf-8-sig")
    fixed = float(re.search(r"CombatRecoveryPerSecond\s*=>\s*([\d.]+)f", magic_source)[1])
    percent = float(re.search(r"CombatMaxMagicPointRecoveryPercentPerSecond\s*=>\s*([\d.]+)f", magic_source)[1])
    html = server.HTML_PATH.read_text(encoding="utf-8-sig")
    page_fixed = float(re.search(r"recovery:([\d.]+)\+maxMana", html)[1])
    rows = {}
    for name, group, levels in [("WindSlashSkill", "basicAttacks", [16, 20, 25, 30, 35]), ("WaterBallSkill", "skills", [26, 30, 35])]:
        before = next(r for r in old[group] if r["id"] == name)
        after = next(r for r in current[group] if r["id"] == name)
        def total(config, row, b):
            if group == "basicAttacks":
                return b * row["ratio"] * row["interval"]
            return b * (config["occupyRatio"] * max(1 / 60, row["cast"]) + config["rate"] / 100 / 20 * row["cost"] * row["cd"] * row.get("cdRatio", 1) * row["k"])
        rows[name] = {"before": before, "after": after, "nodes": []}
        for level in levels:
            before_total, after_total = total(old, before, baseline(old_curve, level)), total(current, after, baseline(curve, level))
            rows[name]["nodes"].append({"characterLevel": level, "oldBaseline": baseline(old_curve, level), "newBaseline": baseline(curve, level), "oldTotal": before_total, "newTotal": after_total, "oldIntegerHit": int(before_total / before["shots"]), "newIntegerHit": int(after_total / after["shots"]), "changePercent": (after_total / before_total - 1) * 100})
    mods = ROOT.parent.parent / "Mods"
    kl_info, kl_files = read_tmod(mods / "KL.tmod")
    elaina_info, elaina_files = read_tmod(mods / (ROOT.name + ".tmod"))
    packed = json.loads(elaina_files["ElainaModSkills/Data/SkillBalance.json"].decode("utf-8-sig"))["configuration"]
    with tempfile.TemporaryDirectory(prefix="elaina-balance-audit-") as temp:
        temp = Path(temp)
        (temp / "PackedKL.dll").write_bytes(kl_files["KL.dll"])
        (temp / "PackedElaina.dll").write_bytes(elaina_files[ROOT.name + ".dll"])
        (temp / "Probe.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>')
        (temp / "Program.cs").write_text('''
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
var kl = Assembly.Load(File.ReadAllBytes(args[0]));
var method = kl.GetType("KL.Utils.KLDpsHelper", true)!.GetMethod("GetLevelDps")!;
var curve = Enumerable.Range(0,20).Select(i => (float)method.Invoke(null,new object[]{i*5})!).ToArray();
using var stream=File.OpenRead(args[1]); using var pe=new PEReader(stream); var reader=pe.GetMetadataReader();
var getters=new Dictionary<string,float>(); bool debugPoll=false;
foreach(var handle in reader.TypeDefinitions){
 var type=reader.GetTypeDefinition(handle); string name=reader.GetString(type.Name);
 foreach(var mh in type.GetMethods()){
  var m=reader.GetMethodDefinition(mh); string mn=reader.GetString(m.Name);
  if(name=="SkillBalanceSystem" && mn=="Poll")debugPoll=true;
  if(name!="ElainaMagicAttributes" || !new[]{"get_CombatRecoveryPerSecond","get_CombatMaxMagicPointRecoveryPercentPerSecond"}.Contains(mn))continue;
  var il=pe.GetMethodBody(m.RelativeVirtualAddress).GetILBytes()!;
  if(il.Length!=6 || il[0]!=0x22 || il[5]!=0x2A)throw new Exception("Unexpected getter IL: "+mn);
  getters[mn]=BitConverter.ToSingle(il,1);
 }
}
Console.WriteLine("AUDIT="+JsonSerializer.Serialize(new{curve,getters,debugPoll}));
''', encoding="utf-8")
        output = subprocess.check_output(["dotnet", "run", "--project", str(temp / "Probe.csproj"), "-c", "Release", "--", str(temp / "PackedKL.dll"), str(temp / "PackedElaina.dll")], text=True, encoding="utf-8")
        probe = json.loads(next(line[6:] for line in output.splitlines() if line.startswith("AUDIT=")))
    wind_per_second = rows["WindSlashSkill"]["after"]["cost"] / rows["WindSlashSkill"]["after"]["interval"]
    result = {"comparison": "source vs Git HEAD; installed TMOD inspected without modifying it", "sourceConfigSha256": hashlib.sha256(current_bytes).hexdigest(), "oldCurve": old_curve, "sourceCurve": curve, "changes": rows,
              "recovery": {"sourceFixed": fixed, "sourceFraction": percent, "pageFixed": page_fixed, "pagePercent": current["combat"]["regenPercent"], "windManaPerSecond": wind_per_second, "sustainThresholdMaxMana": (wind_per_second - fixed) / percent},
              "installed": {"KL": kl_info, "Elaina": elaina_info, "probe": probe, "curveMatchesSource": probe["curve"] == curve, "wind": next(r for r in packed["basicAttacks"] if r["id"] == "WindSlashSkill"), "waterBall": next(r for r in packed["skills"] if r["id"] == "WaterBallSkill"), "skillConfigBytesMatchSource": elaina_files["ElainaModSkills/Data/SkillBalance.json"] == current_bytes}}
    (HERE / "audit-results.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"sourceCurve": curve[:7], "installedCurve": probe["curve"][:7], "installedWindRatio": result["installed"]["wind"]["ratio"], "installedWindCost": result["installed"]["wind"]["cost"], "installedBallK": result["installed"]["waterBall"]["k"], "packedGetters": probe["getters"], "debugHotReadPresent": probe["debugPoll"], "recovery": result["recovery"]}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
