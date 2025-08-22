import os
import sys
import unittest

# Ensure 'src' is on sys.path for package imports
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

from dme.config import CONFIG, REGEXES, MODIFIERS, DME_TERMS


class TestConfig(unittest.TestCase):
    def test_config_loaded(self):
        self.assertIsInstance(CONFIG, dict)
        self.assertIn("regexes", CONFIG)
        self.assertGreater(len(CONFIG["regexes"]), 0)

    def test_regexes_compiled_and_named(self):
        # Names should be accessible case-insensitively
        self.assertIn("hcpcs", REGEXES)
        rx = REGEXES.get("hcpcs")
        self.assertIsNotNone(rx)
        self.assertIsNotNone(rx.search("Code E0601 applies"))

    def test_modifiers_and_terms(self):
        self.assertIn("rental", MODIFIERS)
        self.assertIn("CPAP", DME_TERMS)


if __name__ == "__main__":
    unittest.main()
