"""Local editor validation; only temporary files are written."""
import copy
import json
from pathlib import Path
import tempfile
import unittest
import threading
from urllib.request import Request, urlopen
import server


class BasicDesignTests(unittest.TestCase):
    def setUp(self):
        self.payload = json.loads(server.CONFIG_PATH.read_text(encoding="utf-8-sig"))
        self.c = self.payload["configuration"]
        self.c["basicAttacks"] = [
            dict(id="MagicMissileSkill", name="魔法飞弹", ratio=.8, cost=3, interval=.3, shots=1, unlockStageCap=5, unlockLevel=1, levelsPerPhase=2),
            dict(id="IceConeBasic1", name="冰锥普攻", ratio=.8, cost=10, interval=.5, shots=3, unlockStageCap=10, unlockLevel=6, levelsPerPhase=2),
        ]
        self.c["selectedBasicAttackId"] = "IceConeBasic1"

    def test_atomic_round_trip_preserves_basic_drafts_and_missile(self):
        original = copy.deepcopy(self.c["combat"])
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "balance.json"
            server.atomic_save(path, self.payload)
            restored = json.loads(path.read_text(encoding="utf-8"))["configuration"]
        self.assertEqual(restored["basicAttacks"], self.c["basicAttacks"])
        self.assertEqual(restored["selectedBasicAttackId"], "IceConeBasic1")
        self.assertEqual(restored["combat"], original)

    def test_legacy_payload_rejected(self):
        self.c["version"] = 1
        with self.assertRaises(ValueError):
            server.validate_payload(self.payload)

    def test_growth_modes_round_trip_and_legacy_defaults(self):
        for category, interval in [("basicAttacks", 10), ("skills", 7)]:
            row = self.c[category][0]
            row["growthMode"] = "interval"
            row["levelInterval"] = interval
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "balance.json"
            server.atomic_save(path, self.payload)
            restored = json.loads(path.read_text(encoding="utf-8"))["configuration"]
        self.assertEqual(restored["basicAttacks"][0]["levelInterval"], 10)
        self.assertEqual(restored["skills"][0]["growthMode"], "interval")
        for row in self.c["basicAttacks"] + self.c["skills"]:
            row.pop("growthMode", None)
            row.pop("levelInterval", None)
        server.validate_payload(self.payload)

    def test_growth_fields_reject_invalid_values_for_both_categories(self):
        for category in ["basicAttacks", "skills"]:
            for field, values in [("growthMode", ["unknown", "", None, 1, True, {}]),
                                  ("levelInterval", [0, -1, 1.5, 1001, "7", None, True])]:
                for value in values:
                    payload = copy.deepcopy(self.payload)
                    payload["configuration"][category][0][field] = value
                    with self.subTest(category=category, field=field, value=value):
                        with self.assertRaises(ValueError):
                            server.validate_payload(payload)

    def test_reject_invalid_shots(self):
        for value in [0, 1.5, 101, True, "3"]:
            with self.subTest(value=value):
                self.c["basicAttacks"][1]["shots"] = value
                with self.assertRaises(ValueError):
                    server.validate_payload(self.payload)

    def test_reject_unknown_or_invalid_selection(self):
        for value in ["missing", [], None]:
            self.c["selectedBasicAttackId"] = value
            with self.assertRaises(ValueError):
                server.validate_payload(self.payload)

    def test_reject_cross_category_duplicate(self):
        self.c["skills"][0]["id"] = "IceConeBasic1"
        with self.assertRaises(ValueError):
            server.validate_payload(self.payload)

    def test_regen_percent_defaults_and_validates(self):
        self.c["combat"].pop("regenPercent", None)
        server.validate_payload(self.payload)
        self.assertEqual(self.c["combat"]["regenPercent"], 2)
        self.c["combat"]["regenPercent"] = 1.5
        server.validate_payload(self.payload)
        self.assertEqual(self.c["combat"]["regenPercent"], 1.5)
        for value in [-.1, 100.1, "2"]:
            self.c["combat"]["regenPercent"] = value
            with self.subTest(value=value):
                with self.assertRaises(ValueError):
                    server.validate_payload(self.payload)

    def test_cd_ratio_defaults_and_validates(self):
        self.c["skills"][0].pop("cdRatio", None)
        server.validate_payload(self.payload)
        self.assertEqual(self.c["skills"][0]["cdRatio"], 1)
        self.c["skills"][0]["cdRatio"] = .5
        server.validate_payload(self.payload)
        self.assertEqual(self.c["skills"][0]["cdRatio"], .5)
        for value in [-.1, 1.1, "0.5"]:
            self.c["skills"][0]["cdRatio"] = value
            with self.subTest(value=value):
                with self.assertRaises(ValueError):
                    server.validate_payload(self.payload)

    def test_conversion_rate_accepts_up_to_two_hundred(self):
        for value in [0, 3.5, 40, 100, 130, 200]:
            self.c["skills"][0]["k"] = value
            server.validate_payload(self.payload)
        for value in [-.1, 200.1]:
            self.c["skills"][0]["k"] = value
            with self.subTest(value=value):
                with self.assertRaises(ValueError):
                    server.validate_payload(self.payload)

    def test_ratio_curve_round_trips_and_validates(self):
        curve = [[1, 1], [10, .9], [20, .8], [35, .7], [50, .6]]
        self.c["basicAttacks"][0]["ratioCurve"] = curve
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "balance.json"
            server.atomic_save(path, self.payload)
            restored = json.loads(path.read_text(encoding="utf-8"))["configuration"]
        self.assertEqual(restored["basicAttacks"][0]["ratioCurve"], curve)
        for value in [[], None]:
            self.c["basicAttacks"][0]["ratioCurve"] = value
            server.validate_payload(self.payload)
        bad = [[[10, 1], [5, .5]], [[1, 1], [1, .5]], [[0, 1]], [[1001, 1]], [[1.5, 1]], [[1, 5.1]], [[1, -1]],
               [[1]], [[1, 1, 1]], [["1", 1]], [[1, True]], {"1": 1}, "1:1", [[i, 1] for i in range(1, 18)]]
        for value in bad:
            self.c["basicAttacks"][0]["ratioCurve"] = value
            with self.subTest(value=value):
                with self.assertRaises(ValueError):
                    server.validate_payload(self.payload)

    def test_occupy_ratio_defaults_and_validates(self):
        self.c.pop("occupyRatio", None)
        server.validate_payload(self.payload)
        self.assertEqual(self.c["occupyRatio"], 0)
        for value in [0, 1, 5]:
            self.c["occupyRatio"] = value
            server.validate_payload(self.payload)
        for value in [-.1, 5.1, "1", None]:
            self.c["occupyRatio"] = value
            with self.subTest(value=value):
                with self.assertRaises(ValueError):
                    server.validate_payload(self.payload)

    def test_loadouts_round_trip_and_are_optional(self):
        self.assertEqual(len(self.c["loadouts"]), server.LOADOUT_STAGES)
        self.c["loadouts"][2]["maxLife"] = 405
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "balance.json"
            server.atomic_save(path, self.payload)
            restored = json.loads(path.read_text(encoding="utf-8"))["configuration"]
        self.assertEqual(restored["loadouts"], self.c["loadouts"])
        self.c.pop("loadouts")
        self.c.pop("stage", None)
        server.validate_payload(self.payload)

    def test_reject_invalid_loadouts(self):
        def edit(path, value):
            def apply(c):
                target = c["loadouts"]
                for key in path[:-1]:
                    target = target[key]
                target[path[-1]] = value
            return apply
        cases = [
            lambda c: c["loadouts"].pop(),
            lambda c: c.update(loadouts={}),
            lambda c: c.update(stage=20),
            lambda c: c.update(stage=1.5),
            edit([3, "stage"], 5),
            edit([0, "stage"], True),
            edit([0, "name"], " "),
            edit([0, "name"], "x" * 65),
            edit([0, "armor"], "../x"),
            edit([0, "slots"], 8),
            edit([0, "items"], []),
            edit([0, "items", 0, "id"], 3),
            edit([0, "items", 0, "prefix"], "a b"),
            edit([0, "baseMana"], 30),
            edit([0, "baseMana"], 220),
            edit([0, "maxLife"], 390),
            edit([0, "maxLife"], 403),
            edit([0, "maxLife"], 520),
        ]
        for index, mutate in enumerate(cases):
            payload = copy.deepcopy(self.payload)
            mutate(payload["configuration"])
            with self.subTest(case=index):
                with self.assertRaises(ValueError):
                    server.validate_payload(payload)

    def test_missile_has_no_special_case(self):
        self.c["basicAttacks"][0]["shots"] = 3
        server.validate_payload(self.payload)

    def test_http_v2_read_save_round_trip(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "SkillBalance.json"
            server.atomic_save(path, self.payload)
            http = server.make_server(0, path)
            worker = threading.Thread(target=http.serve_forever, daemon=True)
            worker.start()
            base = f"http://127.0.0.1:{http.server_port}"
            try:
                with urlopen(base + "/api/info") as response:
                    self.assertEqual(json.load(response)["schema"], "elaina-mage-balance-v2")
                self.c["basicAttacks"][1]["ratio"] = .6
                request = Request(base + "/api/skill-balance", data=json.dumps(self.payload).encode(),
                                  headers={"Content-Type": "application/json", "Origin": base}, method="PUT")
                with urlopen(request) as response:
                    self.assertTrue(json.load(response)["saved"])
                with urlopen(base + "/api/skill-balance") as response:
                    self.assertEqual(json.load(response)["data"]["configuration"]["basicAttacks"][1]["ratio"], .6)
            finally:
                http.shutdown()
                http.server_close()
                worker.join()


if __name__ == "__main__":
    unittest.main()
