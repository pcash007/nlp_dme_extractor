import os
import sys
import unittest

# Ensure 'src' is on sys.path for package imports
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

from dme.extractor import extract_params


class TestExtractor(unittest.TestCase):
    def test_hcpcs_and_duration_and_mask(self):
        s = "Order CPAP (E0601) for 3 months with full face mask; qty 1."
        out = extract_params(s)
        self.assertIn("hcpcs", out)
        self.assertIn("E0601", out["hcpcs"])  # case-insensitive match
        self.assertIn("duration", out)
        self.assertEqual(out["duration"]["value"], 3)
        self.assertIn("mask_type", out)
        if isinstance(out["mask_type"], str):
            self.assertIn("full face", out["mask_type"].lower())

    def test_reason_and_modifiers(self):
        s = "Provide oxygen concentrator rental due to COPD; delivery and setup included."
        out = extract_params(s)
        self.assertIn("reasons", out)
        self.assertTrue(any("copd" in r.lower() for r in out["reasons"]))
        self.assertIn("modifiers", out)
        self.assertIn("rental", [m.lower() for m in out["modifiers"]])


if __name__ == "__main__":
    unittest.main()
