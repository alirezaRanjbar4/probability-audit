# Synthetic example

`predictions.jsonl` contains four invented binary predictions; it does not come from a trained model. Running the [quick start](../README.md) with two bins yields 4 rows, 2 positives, Brier 0.125 and ECE 0. The second bin has three rows, two positives and mean probability 2/3. This shows why binned calibration errors can cancel.

The checkout's `Smoke.ps1` installs a locally packed version into a fresh cache and runs success/invalid examples. [CI](../.github/workflows/ci.yml) performs the same smoke on three platforms. See [troubleshooting](../docs/TROUBLESHOOTING.md).
