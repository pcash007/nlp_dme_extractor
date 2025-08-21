import os
import sys
import unittest

# Ensure 'src' is on sys.path for package imports
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

try:
    import torch  # noqa: F401
    import spacy  # noqa: F401
    from dme.core import extract_dme_json
    HEAVY_DEPS = True
except Exception:
    HEAVY_DEPS = False


@unittest.skipUnless(HEAVY_DEPS, "Heavy dependencies not installed; skipping integration test.")
class TestCoreIntegration(unittest.TestCase):
    def test_extract_dme_json_basic(self):
        text = (
            "Doctor: Order CPAP (E0601) for 3 months due to OSA.\n"
            "Patient: Okay."
        )
        out = extract_dme_json(text, gate_threshold=0.0)
        self.assertIn("mentions", out)
        self.assertGreaterEqual(len(out["mentions"]), 1)
        first = out["mentions"][0]
        self.assertIn("equipment", first)
        self.assertIn("hcpcs", first.get("parameters", {}))


if __name__ == "__main__":
    unittest.main()
