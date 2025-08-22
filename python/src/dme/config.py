import json
import os
import re
from typing import Any, Dict

# Load config JSON from package resources path
CONFIG_PATH = os.path.join(os.path.dirname(__file__), "resources", "dme_config.json")

with open(CONFIG_PATH, "r") as f:
    CONFIG: Dict[str, Any] = json.load(f)


def compile_regexes(regex_list):
    compiled = {}
    for spec in regex_list:
        flags = 0
        for flag in spec.get("flags", []):
            if flag.upper() == "IGNORECASE":
                flags |= re.IGNORECASE
        rx = re.compile(spec["pattern"], flags)
        compiled[spec["name"]] = rx
        compiled[spec["name"].lower()] = rx
    return compiled

REGEXES = compile_regexes(CONFIG["regexes"])
MODIFIERS = set(CONFIG.get("modifiers", []))
DME_TERMS = CONFIG.get("dme_terms", [])
