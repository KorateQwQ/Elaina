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
