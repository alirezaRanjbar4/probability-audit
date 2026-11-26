# Probability Audit

An offline .NET 10 CLI for inspecting **binary classifier probability predictions** against observed labels. It reports fixed-width reliability bins, Brier score and binned expected calibration error (ECE) from a JSONL file. It works with probabilities produced by any classifier; it does not train or calibrate a model.

## Quick start

```sh
dotnet run --project src/Cli -- examples/predictions.jsonl --bins 2
```

The four synthetic rows give Brier `0.125` and ECE `0` for two bins. The zero ECE is a useful warning: predictions of 0.5 and 1 land together in the second bin and their errors cancel in the bin average. Different bins can produce different ECE; low ECE is not a guarantee that individual predictions are reliable. See [examples](examples/README.md) and [design](docs/DESIGN.md).

The current source also compares two matched files (this command is not in the published v0.1.0 package):

```sh
dotnet run --project src/Cli -- compare examples/predictions.jsonl examples/candidate.jsonl --bins 2
```

Both inputs must have the same case-sensitive IDs and labels, in any order. The JSON contains a complete report for each input and Brier/ECE differences as candidate minus reference. No ID or individual prediction is emitted. Each hash covers the exact bytes of its own file. Invalid or mismatched inputs produce exit 2 and no report.

## Input and output

Each UTF-8 JSONL line contains exactly `id` (unique nonblank string), `probability` (finite number between 0 and 1, inclusive) and `label` (JSON boolean). IDs are case-sensitive, at most 256 characters; duplicate/unknown fields and blank lines are invalid. UTF-8 BOM is accepted. For example:

```json
{"id":"case-1","probability":0.8,"label":true}
```

`--bins` accepts an integer 1–100 (default 10). Bin `i` is `[i/B,(i+1)/B)` except the final bin includes 1. All bins are reported in order, including empty bins with null averages. Brier is mean `(p-y)^2`; ECE is the count-weighted mean absolute difference between bin probability mean and observed positive fraction. The JSON report contains no IDs or individual predictions, but its SHA-256 identifies the exact input bytes. Counts can still reveal sensitive information. See [design and limits](docs/DESIGN.md).

Exit `0` emits one JSON report; exit `2` signals invalid arguments, malformed data, resource limit or inaccessible file on stderr with no JSON. The CLI reads but never writes the input. Maximum input 32 MiB, 100,000 rows and 1 Mi UTF-16 characters per line; data is held in memory. Keep inputs stable while reading; hash is not a concurrent filesystem snapshot guarantee.

## Install

Download `ProbabilityAudit.Tool.0.1.0.nupkg` and `SHA256SUMS.txt` from [v0.1.0](https://github.com/alirezaRanjbar4/probability-audit/releases/tag/v0.1.0). Check the downloaded SHA-256, then install with the .NET 10 SDK:

```sh
dotnet tool install ProbabilityAudit.Tool --version 0.1.0 --tool-path ./tool --add-source ./downloaded-package
./tool/probability-audit examples/predictions.jsonl --bins 2
```

The installed executable needs the .NET 10 runtime; the package is distributed as a GitHub Release asset, not NuGet.org. A checksum alongside an asset detects corruption, not authorship or provenance. Windows uses `tool\probability-audit.exe`.

## Interpretation

Use held-out, representative labeled predictions and avoid training-set leakage; the tool cannot prove data independence. Paired point estimates and bin differences do not establish statistical significance, causality, representative sampling or holdout independence. ECE depends on binning and sample size and may hide errors. Brier reflects calibration **and** discrimination and outcome uncertainty, so it is not a calibration-only score. No confidence intervals, multiclass assessment, statistical guarantees, model generation or fitting are provided. [Research](docs/RESEARCH.md) identifies established alternatives.

See [troubleshooting](docs/TROUBLESHOOTING.md), [contributing](CONTRIBUTING.md), [roadmap](ROADMAP.md), [changelog](CHANGELOG.md) and [license](LICENSE).
