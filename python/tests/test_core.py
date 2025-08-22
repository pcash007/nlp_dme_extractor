import os
import sys
import unittest

# Ensure 'src' is on sys.path for package imports
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)

from dme.core import split_turns


class TestCore(unittest.TestCase):
    def test_split_turns(self):
        text = "Doctor: Hello.\nPatient: Hi there.\nOther: Notes."
        turns = split_turns(text)
        self.assertEqual(len(turns), 3)
        self.assertEqual(turns[0]["speaker"], "Doctor")
        self.assertEqual(turns[1]["speaker"], "Patient")


if __name__ == "__main__":
    unittest.main()
