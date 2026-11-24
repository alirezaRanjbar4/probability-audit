# Contributing

Use small synthetic fixtures and never submit private datasets, predictions or credentials. See the [design](docs/DESIGN.md), [troubleshooting](docs/TROUBLESHOOTING.md) and bounded [roadmap](ROADMAP.md).

With .NET 10 SDK and PowerShell 7, run `dotnet restore`, `dotnet test -c Release --no-restore`, `dotnet format --verify-no-changes --no-restore`, `dotnet pack src/Cli -c Release --no-restore -o artifacts`, then `pwsh -File examples/Smoke.ps1`. Commit intended source and pack first so the smoke's final-commit assertion is meaningful. Do not claim statistical confidence or model quality from a synthetic example.
