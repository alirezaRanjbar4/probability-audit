# Troubleshooting

For exit 2 check stderr, UTF-8 JSONL syntax and required `id`, `probability`, `label` fields. IDs must be unique and nonblank, labels must be boolean, and probabilities finite in `[0,1]`. Extra fields are rejected. `--bins` is a whole number between 1 and 100. No report is emitted on input failure.

An empty bin has null means, not zero. Probability 1 belongs to the last bin; a boundary such as 0.5 with two bins belongs to the second. If ECE appears unexpectedly small, inspect counts and compare bin choices: within-bin errors can cancel. Brier and ECE measure different things and neither establishes model accuracy or sample representativeness. See [design](DESIGN.md).

Large inputs may exceed the explicit byte/row/line limits. Supply a smaller representative evaluation set; concatenating reports or averaging their ECE values does not in general reproduce the ECE of the combined dataset. For installation, confirm the downloaded version, SHA-256 and .NET 10 runtime; the tool is a GitHub Release asset, not a NuGet.org publication. Submit only synthetic data in issues; see [contributing](../CONTRIBUTING.md).
