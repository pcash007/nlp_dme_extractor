Test suite for DME Extractor

- Place unit tests in this folder. We use Python's built-in unittest.
- Tests modify sys.path to include the project's `src` folder for imports.
- Heavy dependencies (torch/transformers/spacy/medspacy) are lazily imported where possible.
- The integration test `test_core_integration.py` is skipped automatically if heavy deps are unavailable.

Running tests:
- From project root, run: `python -m unittest discover -s tests -p 'test_*.py'`
