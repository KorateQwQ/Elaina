# 当前双子时期冰水循环与极端打法

2026-10-20。只读分析，不修改正式配置或HTML。

## 保存配装

{
  "level": 50,
  "loadout": {
    "stage": 10,
    "name": "神圣法师",
    "armor": "hallowed",
    "slots": 7,
    "items": [
      {
        "id": "elainaHat",
        "prefix": "arcane",
        "vPrefix": "none"
      },
      {
        "id": "sorcerer",
        "prefix": "arcane",
        "vPrefix": "lucky"
      },
      {
        "id": "regen",
        "prefix": "arcane",
        "vPrefix": "lucky"
      },
      {
        "id": "wormScarf",
        "prefix": "arcane",
        "vPrefix": "lucky"
      },
      {
        "id": "ankhShield",
        "prefix": "arcane",
        "vPrefix": "menacing"
      },
      {
        "id": "charmMyths",
        "prefix": "arcane",
        "vPrefix": "lucky"
      },
      {
        "id": "sharkTooth",
        "prefix": "arcane",
        "vPrefix": "menacing"
      }
    ],
    "baseMana": 200,
    "maxLife": 400
  },
  "maxMana": 630,
  "maxLife": 250,
  "gear": {
    "damage": 44,
    "crit": 28,
    "mana": 260,
    "magic": 20,
    "defense": 0,
    "multiplier": 1.7723076923076921,
    "nebulaDamage": 0,
    "nebulaManaRegen": 0,
    "nebulaLifeRegen": 0,
    "armorDefense": 0,
    "manaReduction": 0,
    "moveSpeed": 0
  },
  "skills": {
    "ice": {
      "level": 13,
      "damageLevel": 50,
      "cost": 22.5,
      "interval": 0.75,
      "damage": 194,
      "freezeBuildup": 5.6
    },
    "water": {
      "level": 11,
      "damageLevel": 50,
      "cost": 1.5,
      "interval": 0.2,
      "damage": 116,
      "freezeBuildup": 0
    },
    "ball": {
      "level": 6,
      "damageLevel": 50,
      "cost": 100,
      "cd": 15,
      "damage": 4374,
      "freezeBuildup": 0
    },
    "lock": {
      "level": 4,
      "damageLevel": 50,
      "cost": 250,
      "cd": 60,
      "damage": 20606,
      "freezeBuildup": 0
    }
  }
}

## 全奥秘，月露全程覆盖

| 打法 | 60秒DPS | 120秒DPS | 300秒DPS | 600秒DPS | 300秒锁/球 | 300秒锁间隔 |
|---|---:|---:|---:|---:|---|---:|
| 完整循环·每轮最多1球 | 2283 | 2024 | 1973 | 1965 | 12/12 | 25.2秒 |
| 完整循环·每轮最多2球 | 2362 | 2137 | 1943 | 1937 | 12/16 | 26.8秒 |
| 完整循环·水球可用即尝试 | 2359 | 2137 | 1948 | 1936 | 11/17 | 27.2秒 |
| 冰锥＋水流＋冰凌锁 | 1621 | 1834 | 1701 | 1700 | 11/0 | 27.6秒 |
| 冰锥＋水流＋水球 | 1063 | 1057 | 1059 | 1047 | 0/20 | —秒 |
| 冰锥＋水流 | 763 | 759 | 757 | 758 | 0/0 | —秒 |
| 仅冰锥 | 993 | 956 | 857 | 824 | 0/0 | —秒 |
| 仅水流 | 577 | 577 | 577 | 577 | 0/0 | —秒 |
| 仅水球 | 373 | 373 | 373 | 373 | 0/20 | —秒 |
| 仅冰凌锁 | 0 | 0 | 0 | 0 | 0/0 | —秒 |
| 冰锥＋冰凌锁 | 1420 | 1259 | 1160 | 1096 | 5/0 | 72.0秒 |
| 冰锥＋水球＋冰凌锁 | 1499 | 1296 | 1175 | 1103 | 5/2 | 69.5秒 |
| 冰锥＋水球 | 1267 | 1031 | 889 | 839 | 0/4 | —秒 |
| 水流＋水球 | 895 | 893 | 892 | 892 | 0/20 | —秒 |

## 全增伤，月露全程覆盖

| 打法 | 60秒DPS | 120秒DPS | 300秒DPS | 600秒DPS | 300秒锁/球 | 300秒锁间隔 |
|---|---:|---:|---:|---:|---|---:|
| 完整循环·每轮最多1球 | 2159 | 2155 | 1967 | 1947 | 9/9 | 32.7秒 |
| 完整循环·每轮最多2球 | 2261 | 2183 | 1983 | 1954 | 9/10 | 33.5秒 |
| 完整循环·水球可用即尝试 | 2254 | 2181 | 1982 | 1954 | 9/10 | 33.3秒 |
| 冰锥＋水流＋冰凌锁 | 1981 | 2242 | 2080 | 2077 | 11/0 | 27.6秒 |
| 冰锥＋水流＋水球 | 1300 | 1293 | 1296 | 1281 | 0/20 | —秒 |
| 冰锥＋水流 | 934 | 930 | 928 | 928 | 0/0 | —秒 |
| 仅冰锥 | 1107 | 956 | 859 | 828 | 0/0 | —秒 |
| 仅水流 | 708 | 708 | 708 | 708 | 0/0 | —秒 |
| 仅水球 | 456 | 456 | 456 | 456 | 0/20 | —秒 |
| 仅冰凌锁 | 0 | 0 | 0 | 0 | 0/0 | —秒 |
| 冰锥＋冰凌锁 | 1477 | 1323 | 1130 | 1159 | 4/0 | 75.3秒 |
| 冰锥＋水球＋冰凌锁 | 1530 | 1344 | 1144 | 1165 | 4/1 | 75.4秒 |
| 冰锥＋水球 | 1199 | 1001 | 877 | 837 | 0/2 | —秒 |
| 水流＋水球 | 1095 | 1093 | 1092 | 1092 | 0/20 | —秒 |

## 全奥秘300秒：伤害分配、月露影响

| 打法 | 冰锥% | 水流% | 水球% | 冰凌锁% | 无药DPS | 有药DPS | 有药结束蓝 |
|---|---:|---:|---:|---:|---:|---:|---:|
| 完整循环·每轮最多1球 | 9.8 | 25.3 | 11.4 | 53.5 | 1486 | 1973 | 253 |
| 完整循环·每轮最多2球 | 9.2 | 25.0 | 15.4 | 50.4 | 1503 | 1943 | 17 |
| 完整循环·水球可用即尝试 | 9.2 | 24.9 | 16.3 | 49.7 | 1510 | 1948 | 191 |
| 冰锥＋水流＋冰凌锁 | 10.5 | 32.6 | 0.0 | 56.8 | 1590 | 1701 | 613 |
| 仅冰锥 | 100.0 | 0.0 | 0.0 | 0.0 | 651 | 857 | 15 |
| 冰锥＋冰凌锁 | 62.1 | 0.0 | 0.0 | 37.9 | 881 | 1160 | 2 |
| 水流＋水球 | 0.0 | 58.2 | 41.8 | 0.0 | 892 | 892 | 630 |

## 有限生命转魔力（无受伤无治疗，300秒）

| 前缀 | 打法 | DPS | 生命消耗 | 剩余生命 | 首次转化 |
|---|---|---:|---:|---:|---:|
| 奥秘 | 完整循环·每轮最多1球 | 1973 | 0 | 250 | —秒 |
| 奥秘 | 完整循环·每轮最多2球 | 2077 | 219 | 31 | 103.2秒 |
| 奥秘 | 仅冰锥 | 916 | 249 | 1 | 99.8秒 |
| 奥秘 | 冰锥＋冰凌锁 | 1217 | 249 | 1 | 62.3秒 |
| 增伤 | 完整循环·每轮最多1球 | 2118 | 249 | 1 | 55.2秒 |
| 增伤 | 完整循环·每轮最多2球 | 2131 | 249 | 1 | 28.8秒 |
| 增伤 | 仅冰锥 | 913 | 249 | 1 | 45.8秒 |
| 增伤 | 冰锥＋冰凌锁 | 1185 | 249 | 1 | 23.3秒 |

## 水球测试CD1秒（120秒，全奥秘有月露）

- 仅水球：1586DPS，34球/0锁。
- 水流＋水球：1669DPS，29球/0锁。
- 完整循环·水球可用即尝试：1899DPS，14球/3锁。

## 前缀及循环策略扫描

- 120秒所测最高：2奥秘，完整循环·每轮最多1球，2287DPS，5锁/4球。
- 300秒所测最高：0奥秘，冰锥＋水流＋冰凌锁，2080DPS，11锁/0球。

## 75%普通攻击/水球命中率敏感性

16个固定种子，冰凌锁仍全中：完整每轮最多1球平均1567DPS，范围1524～1691。只代表独立丢失命中的模型，未模拟首领移动/连续空窗。

- Current stage10saved equipment at character50, all four skills at stage cap. Stage10is the earlier comparison convention, not a claim that a player fighting Twins already owns Hallowed armor.
- Primary tables have full opening mana, maintained120second Moon Dew granting1% each second, no instant elixir charges and no other potions. No-potion alternatives included.
- Single persistent zero-defense freezable target, all hits and expected standard crit. No Twins AI, movement, armor procs, damage taken, shields or healing. These numbers are comparable skill DPS, not actual boss DPS.
- Formal water-ball15second cooldown in primary rows; current icon callback1second override is an isolated stress case, preserving actual per-cast damage instead of repricing it at1second.
- Ice/lock45frame action; ball82frame minimum action with70frame fixed hit; water20frame exit. Freeze lasts4seconds, then60second cooldown; lock60seconds plus one freeze permission.
- Primary results disable life conversion to isolate sustainable mana. Additional cases consume a finite250HP pool at actual1%life/1%mana step, rounded as runtime, no healing or hits.
- One/two balls per cycle mean maximum limits; a ready affordable lock is never delayed only to fulfill the quota. No opening ball for quota policies.
- Long-window results remain affected by finite opening mana and action timing. No global optimization; prefix sweep is over listed policies only.
