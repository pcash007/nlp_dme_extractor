#!/usr/bin/env python3
"""CLI entrypoint for DME extractor using shared core.
Usage:
    - No args: uses a built-in demo conversation
    - --conversation/-c "...text...": provide conversation text directly
    - --threshold/-t FLOAT: set DME sentence gate threshold (default: 0.45)
"""
import json
import argparse


DEMO = (
    "Doctor: Hi, I'm Dr. Alice Nguyen. I see you’ve been more short of breath at night.\n"
    "Patient: Yes, I'm John Carter. I wake up gasping.\n"
    "Doctor: Your sleep study confirms OSA. We'll order a CPAP with nasal mask and tubing (E0601) for 3 months, rental initially, for OSA.\n"
    "Patient: Do I also need oxygen?\n"
    "Doctor: Not at rest, your SpO2 is fine. No wheelchair at home.\n"
    "Patient: Understood.\n"
    "Doctor: For daytime fatigue due to OSA, CPAP at 10 cm H2O should help. Arrange delivery and setup.\n"
)


def main():
    parser = argparse.ArgumentParser(prog="dme-extract", add_help=True)
    parser.add_argument(
        "--conversation",
        "-c",
        dest="conversation",
        help="Conversation text; if omitted, a demo conversation is used",
    )
    parser.add_argument(
        "--threshold",
        "-t",
        type=float,
        default=0.45,
        help="DME sentence gate threshold",
    )
    args = parser.parse_args()

    text = args.conversation if args.conversation else DEMO

    # Lazy import to avoid importing heavy deps when only showing --help
    from dme.core import extract_dme_json

    result = extract_dme_json(text, gate_threshold=args.threshold)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
