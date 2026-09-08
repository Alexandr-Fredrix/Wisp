import pathlib
import sys
import unittest
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "tools"))
from package_source import eligible

class DistributionTests(unittest.TestCase):
    def test_game_binaries_saves_and_secrets_are_rejected(self):
        for name in ["Assembly-CSharp.dll", "src/Game.DLL", "user1.dat",
                     ".env.production", "private.key", "user2.modded.json",
                     "HollowKnightManaged/README.md", "dist/old.zip"]:
            self.assertFalse(eligible(name), name)

    def test_original_sources_and_notices_are_included(self):
        for name in ["src/Wisp/WispMod.cs", "LICENSE", "README.md",
                     ".github/workflows/release.yml", "version.json"]:
            self.assertTrue(eligible(name), name)

if __name__ == "__main__":
    unittest.main()
