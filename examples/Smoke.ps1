param([string]$ExpectedCommit,[string]$PackageDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$previousCache=$env:NUGET_PACKAGES
Push-Location $root
try {
    if(-not $ExpectedCommit){$ExpectedCommit=git rev-parse HEAD;if($LASTEXITCODE -ne 0){throw 'Cannot determine commit'}}
    if(-not $PackageDirectory){$PackageDirectory=Join-Path $root 'artifacts'}
    $feed=(Resolve-Path -LiteralPath $PackageDirectory).Path
    $package=Join-Path $feed 'ProbabilityAudit.Tool.0.1.0.nupkg'
    $packageHash=(Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
    $run=Join-Path $root ('artifacts/smoke-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $run -Force|Out-Null
    $zip=[IO.Compression.ZipFile]::OpenRead($package)
    try {
        $nuspec=@($zip.Entries|Where-Object {$_.FullName -notmatch '/' -and $_.FullName.EndsWith('.nuspec')})
        if($nuspec.Count -ne 1){throw 'Missing manifest'}
        $settings=[Xml.XmlReaderSettings]::new();$settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit;$settings.XmlResolver=$null;$settings.MaxCharactersInDocument=1048576
        $stream=$nuspec[0].Open();$reader=[Xml.XmlReader]::Create($stream,$settings)
        try {$xml=[Xml.XmlDocument]::new();$xml.XmlResolver=$null;$xml.Load($reader)} finally {$reader.Dispose();$stream.Dispose()}
        $meta=$xml.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
        if($meta.SelectSingleNode('*[local-name()="id"]').InnerText -ne 'ProbabilityAudit.Tool' -or $meta.SelectSingleNode('*[local-name()="version"]').InnerText -ne '0.1.0' -or $meta.SelectSingleNode('*[local-name()="repository"]').GetAttribute('commit') -ne $ExpectedCommit){throw 'Package identity or commit mismatch'}
        $fixture=$zip.GetEntry('examples/predictions.jsonl');if(-not $fixture){throw 'Missing fixture'}
        $input=Join-Path $run 'predictions.jsonl';$stream=$fixture.Open();$output=[IO.File]::Open($input,[IO.FileMode]::CreateNew)
        try {$stream.CopyTo($output)} finally {$stream.Dispose();$output.Dispose()}
        if((Get-FileHash $input -Algorithm SHA256).Hash -ne (Get-FileHash (Join-Path $PSScriptRoot 'predictions.jsonl') -Algorithm SHA256).Hash){throw 'Fixture bytes differ'}
    } finally {$zip.Dispose()}
    $config=Join-Path $run 'NuGet.Config';$escaped=[Security.SecurityElement]::Escape($feed)
    [IO.File]::WriteAllText($config,('<configuration><packageSources><clear/><add key="local" value="'+$escaped+'"/></packageSources></configuration>'))
    $env:NUGET_PACKAGES=Join-Path $run 'packages'
    $toolDirectory=Join-Path $run 'tool'
    dotnet tool install ProbabilityAudit.Tool --version 0.1.0 --tool-path $toolDirectory --configfile $config --no-cache
    if($LASTEXITCODE -ne 0){throw 'Local installation failed'}
    $tool=Join-Path $toolDirectory 'probability-audit'
    $before=(Get-FileHash $input -Algorithm SHA256).Hash.ToLowerInvariant()
    $report=& $tool $input --bins 2|ConvertFrom-Json
    if($LASTEXITCODE -ne 0 -or $report.rows -ne 4 -or $report.positives -ne 2 -or [Math]::Abs($report.brierScore-0.125) -gt 1e-12 -or [Math]::Abs($report.expectedCalibrationError) -gt 1e-12 -or $report.bins[1].count -ne 3 -or $report.inputSha256 -ne $before){throw 'Hand-computed example failed'}
    $invalid=& $tool $input --bins 0 2> (Join-Path $run 'invalid.txt')
    if($LASTEXITCODE -ne 2 -or $invalid){throw 'Invalid bin contract failed'}
    if((Get-FileHash $input -Algorithm SHA256).Hash.ToLowerInvariant() -ne $before -or (Get-FileHash $package -Algorithm SHA256).Hash -ne $packageHash){throw 'Input or package changed'}
    $proof=@{commit=$ExpectedCommit;packageSha256=$packageHash.ToLowerInvariant();inputSha256=$before;rows=4;brier=$report.brierScore;ece=$report.expectedCalibrationError;counts=@($report.bins|ForEach-Object {$_.count});installedFromFreshLocalFeed=$true;exits='0/2';evidenceDirectory=$run}
    $proof|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $run 'verification.json') -Encoding utf8
    Write-Output ('Installed smoke passed; evidence: '+$run)
    $global:LASTEXITCODE=0
} finally {$env:NUGET_PACKAGES=$previousCache;Pop-Location}
