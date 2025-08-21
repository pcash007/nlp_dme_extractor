from __future__ import annotations
import warnings
import spacy
from spacy.pipeline import EntityRuler
import medspacy

_NLP = None


def get_nlp(dme_terms: list[str]):
    global _NLP
    if _NLP is not None:
        return _NLP
    for model in ("en_core_web_sm", "en_core_web_md", "en_core_web_lg"):
        try:
            _NLP = spacy.load(model, disable=["lemmatizer"])
            break
        except Exception:
            continue
    if _NLP is None:
        warnings.warn("Falling back to blank English pipeline")
        _NLP = spacy.blank("en")
    if "sentencizer" not in _NLP.pipe_names:
        _NLP.add_pipe("sentencizer")
    ruler = _NLP.add_pipe("entity_ruler", config={"overwrite_ents": True})
    patterns = [{"label": "DME", "pattern": term, "id": term.lower()} for term in dme_terms]
    ruler.add_patterns(patterns)
    if "medspacy_context" not in _NLP.pipe_names:
        _NLP.add_pipe("medspacy_context")
    return _NLP
