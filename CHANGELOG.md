# Changelog

## Unreleased (0.2.0)

Compare matched labeled predictions by ID, with separate reliability reports, exact-byte hashes and candidate-minus-reference Brier/ECE differences. Reject mismatched IDs or labels before output. Point estimates do not establish significance or dataset independence.

## 0.1.0 — 2026-11-24

Initial offline binary probability audit: exact-byte input hash, strict JSONL, Brier score, fixed-width reliability bins and ECE; synthetic example, meaningful tests and Windows/Linux/macOS CI. It does not train or fit a calibration mapping. See the [roadmap](ROADMAP.md) for the bounded follow-up.
