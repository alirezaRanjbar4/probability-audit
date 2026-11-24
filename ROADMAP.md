# Bounded first scope

Fixed before initial publication on 2026-11-24:

- [x] Offline binary probability/boolean-label JSONL audit with strict input validation, input SHA-256, Brier, equal-width reliability bins and binned ECE; tests, examples and three-platform CI.
- [ ] Verified v0.1.0 release with actual downloaded package, installed examples and matching final commit/tag/assets.
- [ ] Add a read-only comparison of two prediction files with exactly matching IDs and labels, paired per-bin/Brier reporting and reproducible input hashes; do not claim statistical significance.
- [ ] Verified closing release including that comparison, docs and examples, then public/unarchived maintenance for reproducible bugs and feedback.

Neural/LLM training, calibration fitting, confidence intervals and multiclass/multilabel scoring are outside this fixed scope. See [research](docs/RESEARCH.md).
