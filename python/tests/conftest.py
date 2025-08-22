import os
import sys

# Ensure 'src' is on sys.path for package imports when running tests
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)
