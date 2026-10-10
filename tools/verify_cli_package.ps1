# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

# ローカルパッケージだけを使い、リポジトリ外でインストール済みToolを検証します。
param([switch]$SkipPack)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packages = Join-Path $repository 'artifacts/nuget'
[xml]$props = Get-Content -Raw (Join-Path $repository 'Directory.Build.props')
$version = $props.Project.PropertyGroup.KanjiVariantsCliVersion.InnerText
$packageFile = Join-Path $packages "KanjiVariants.Cli.$version.nupkg"
$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$scratch = Join-Path $tempRoot ('KanjiVariants-tool-check-' + [guid]::NewGuid().ToString('N'))
$results = [Collections.Generic.List[object]]::new()

function Invoke-Captured([string]$File, [string[]]$Arguments, [string]$WorkingDirectory) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $File
    $start.WorkingDirectory = $WorkingDirectory
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    # 検証中の取得物を一時領域へ限定し、通常のNuGetキャッシュを使いません。
    $start.Environment['NUGET_PACKAGES'] = Join-Path $scratch 'cache'
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    try {
        [void]$process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        return [pscustomobject]@{ ExitCode = $process.ExitCode; Stdout = $stdout.GetAwaiter().GetResult(); Stderr = $stderr.GetAwaiter().GetResult() }
    } finally { $process.Dispose() }
}

function Test-Command([string[]]$Arguments, [int]$ExpectedCode, [switch]$Json) {
    $actual = Invoke-Captured $tool $Arguments $scratch
    if ($actual.ExitCode -ne $ExpectedCode) { throw "Unexpected exit code: $($Arguments -join ' '): $($actual.ExitCode); $($actual.Stderr)" }
    if ($ExpectedCode -eq 2) {
        if ($actual.Stdout.Length -ne 0 -or [string]::IsNullOrWhiteSpace($actual.Stderr)) { throw 'Errors must have empty stdout and a diagnostic on stderr.' }
    } else {
        if ($actual.Stderr.Length -ne 0 -or [string]::IsNullOrWhiteSpace($actual.Stdout)) { throw 'Successful diagnosis must use stdout only.' }
        if ($Json) {
            $parsed = $actual.Stdout | ConvertFrom-Json -ErrorAction Stop
            if ($parsed.hasInvalidUtf16) { throw 'Valid UTF-8 input was reported as invalid UTF-16.' }
            $items = if ($Arguments[0] -eq 'check') { @($parsed.unlearned) } else { @($parsed.nonJoyo) }
            if ($Arguments -contains '--unique' -and $ExpectedCode -eq 1 -and -not ($items | Where-Object { $_.count -gt 1 })) {
                throw 'Duplicate grouping did not produce a count greater than one.'
            }
        } elseif ($Arguments -contains '--unique' -and $ExpectedCode -eq 1 -and $actual.Stdout -notmatch 'count=2') {
            throw 'Text output did not group duplicate characters.'
        }
    }
    $results.Add([pscustomobject]@{ Command = $Arguments -join ' '; ExitCode = $actual.ExitCode; Stdout = $actual.Stdout; Stderr = $actual.Stderr })
}

try {
    [void](New-Item -ItemType Directory -Path $scratch)
    if (-not $SkipPack) {
        # packは開発環境のキャッシュを使い、インストール検証だけを隔離します。
        Push-Location $repository
        try {
            & dotnet pack KanjiVariants.Cli/KanjiVariants.Cli.csproj -c Release -o $packages -m:1 -p:UseSharedCompilation=false
            if ($LASTEXITCODE -ne 0) { throw 'dotnet pack failed.' }
        } finally { Pop-Location }
    }
    $zip = [IO.Compression.ZipFile]::OpenRead($packageFile)
    try {
        $entries = @($zip.Entries.FullName)
        foreach ($name in @('DotnetToolSettings.xml', 'KanjiVariants.Cli.dll', 'KanjiVariants.dll', 'KanjiVariants.Cli.deps.json', 'KanjiVariants.Cli.runtimeconfig.json')) {
            if (@($entries | Where-Object { ($_ -split '/')[-1] -eq $name }).Count -ne 1) { throw "Missing or duplicate runtime file: $name" }
        }
        if ($entries | Where-Object { $_ -match '(?i)(Tests|TestData|(^|/)(obj|bin)/|\.(cs|pdb|xlsx|csv|user|suo)$)' }) { throw 'Unexpected development files in package.' }
        $settingsEntry = $zip.Entries | Where-Object { $_.FullName.EndsWith('/DotnetToolSettings.xml') }
        $reader = [IO.StreamReader]::new($settingsEntry.Open())
        try { [xml]$settings = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($settings.DotNetCliTool.Commands.Command.Name -ne 'kanjivariants' -or $settings.DotNetCliTool.Commands.Command.EntryPoint -ne 'KanjiVariants.Cli.dll') { throw 'Invalid tool command settings.' }
        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') }
        $reader = [IO.StreamReader]::new($nuspecEntry.Open())
        try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($nuspec.package.metadata.id -ne 'KanjiVariants.Cli' -or $nuspec.package.metadata.version -ne $version) { throw 'Invalid package identity.' }
        if ($nuspec.package.metadata.packageTypes.packageType.name -ne 'DotnetTool') { throw 'Not a DotnetTool package.' }
    } finally { $zip.Dispose() }

    $config = Join-Path $scratch 'NuGet.Config'
    # clearで他のフィードを排除します。パッケージソースは今回の出力ディレクトリだけです。
    $escapedSource = [Security.SecurityElement]::Escape($packages)
    [IO.File]::WriteAllText($config, "<configuration><packageSources><clear/><add key=`"local`" value=`"$escapedSource`"/></packageSources></configuration>")
    $toolPath = Join-Path $scratch 'tool'
    $install = Invoke-Captured 'dotnet' @('tool', 'install', 'KanjiVariants.Cli', '--tool-path', $toolPath, '--configfile', $config, '--version', $version) $scratch
    if ($install.ExitCode -ne 0) { throw "Tool install failed: $($install.Stdout) $($install.Stderr)" }
    $tool = Join-Path $toolPath $(if ($IsWindows) { 'kanjivariants.exe' } else { 'kanjivariants' })
    $installedLibrary = @(Get-ChildItem -LiteralPath $toolPath -Recurse -Filter KanjiVariants.dll)
    if ($installedLibrary.Count -ne 1) { throw 'Installed library DLL is missing or duplicated.' }
    # 同梱DLLのハッシュを比較し、外部のProjectReference先を読んでいないことを確認します。
    $zip = [IO.Compression.ZipFile]::OpenRead($packageFile)
    try {
        $libraryEntry = $zip.Entries | Where-Object { $_.FullName.EndsWith('/KanjiVariants.dll') }
        $stream = $libraryEntry.Open()
        try { $sha = [Security.Cryptography.SHA256]::Create(); $hash = [Convert]::ToHexString($sha.ComputeHash($stream)) } finally { $stream.Dispose(); $sha.Dispose() }
    } finally { $zip.Dispose() }
    if ((Get-FileHash -LiteralPath $installedLibrary[0].FullName -Algorithm SHA256).Hash -ne $hash) { throw 'Installed DLL differs from packaged DLL.' }

    [IO.File]::WriteAllText((Join-Path $scratch 'sample.txt'), '議議催髙髙')
    [IO.File]::WriteAllText((Join-Path $scratch 'sample.srt'), "1`r`n00:00:01,000 --> 00:00:02,000`r`n議議催髙髙`r`n")
    [IO.File]::WriteAllText((Join-Path $scratch 'good.txt'), '学校へ行く')
    [IO.File]::WriteAllText((Join-Path $scratch 'good.srt'), "1`n00:00:01,000 --> 00:00:02,000`n学校へ行く`n")
    [IO.File]::WriteAllText((Join-Path $scratch 'bad.srt'), "1`ninvalid time`n学校")
    [IO.File]::WriteAllBytes((Join-Path $scratch 'bad.txt'), [byte[]]@(0xC3, 0x28))
    foreach ($file in @('sample.txt', 'sample.srt')) {
        foreach ($command in @('check', 'joyo')) {
            $baseArguments = @($command, $file)
            if ($command -eq 'check') { $baseArguments += @('--grade', '3') }
            Test-Command $baseArguments 1
            Test-Command ($baseArguments + @('--unique')) 1
            Test-Command ($baseArguments + @('--json')) 1 -Json
            Test-Command ($baseArguments + @('--unique', '--json')) 1 -Json
        }
    }
    foreach ($file in @('good.txt', 'good.srt')) {
        Test-Command @('check', $file, '--grade', '3', '--json') 0 -Json
        Test-Command @('joyo', $file, '--json') 0 -Json
    }
    Test-Command @('check', 'sample.txt', '--json') 2
    Test-Command @('joyo', 'missing.txt', '--json') 2
    Test-Command @('joyo', 'sample.txt', '--unknown', '--json') 2
    Test-Command @('joyo', 'bad.srt', '--json') 2
    Test-Command @('joyo', 'bad.txt', '--json') 2
    $report = [pscustomobject]@{
        Package = $packageFile; Version = $version; PackageBytes = (Get-Item -LiteralPath $packageFile).Length
        Entries = $entries; InstalledLibrarySha256 = $hash; LocalSourcesOnly = $true
        RepositoryExternalWorkingDirectory = $scratch; Commands = $results
    }
    $reportPath = Join-Path $repository 'artifacts/cli-package-verification.json'
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
    Write-Output "PASS: $($results.Count) installed-tool commands. Report: $reportPath"
} finally {
    # 再帰削除の直前に、一時領域直下のこの実行専用ディレクトリであることを確認します。
    $resolvedScratch = [IO.Path]::GetFullPath($scratch)
    if ([IO.Path]::GetDirectoryName($resolvedScratch).TrimEnd([IO.Path]::DirectorySeparatorChar) -ne $tempRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) -or
        [IO.Path]::GetFileName($resolvedScratch) -notmatch '^KanjiVariants-tool-check-[0-9a-f]{32}$') { throw 'Unsafe cleanup path.' }
    if (Test-Path -LiteralPath $resolvedScratch) { Remove-Item -LiteralPath $resolvedScratch -Recurse -Force }
}
