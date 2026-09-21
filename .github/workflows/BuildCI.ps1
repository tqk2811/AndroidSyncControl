# CI build script: dotnet publish the WPF app (framework-dependent, win-x64) and zip it.
# NO nuget/native. release.yml handles the GitHub Release create/upload + notes.
# Resolves the repo root from this script's location so it can be invoked from anywhere.
# Output: publish\AndroidSyncControl-<version>-<TargetFramework>.zip
$ErrorActionPreference = 'Stop'
# Quiet the .NET first-run experience/telemetry so freshly installed tools do not
# print a banner to stdout that would contaminate captured command output.
$env:DOTNET_NOLOGO = 'true'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $root

$proj = Join-Path $root 'AndroidSyncControl\AndroidSyncControl.csproj'
if (-not (Test-Path $proj)) { throw "Project not found: $proj" }

# TargetFramework from the csproj -> used in the zip name.
[xml]$csproj = Get-Content $proj
$tfm = @($csproj.Project.PropertyGroup.TargetFramework) | Where-Object { $_ } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($tfm)) { throw "TargetFramework not found in $proj" }

# Version via GitVersion (M.N.<commits-since-tag>). Install the tool if missing.
if (-not (Get-Command dotnet-gitversion -ErrorAction SilentlyContinue)) {
    dotnet tool install -g GitVersion.Tool | Out-Host
    $env:PATH = "$env:PATH;$env:USERPROFILE\.dotnet\tools"
}
# GitVersion stdout can be preceded by log lines on a fresh runner, and the freshly
# installed tool may even exit non-zero on its first call. Relax the error preference
# for the call, capture all output, then slice out the JSON object so parsing is robust.
$eap = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$gvRaw = (dotnet-gitversion /output json 2>&1 | Out-String)
$ErrorActionPreference = $eap
$jsonStart = $gvRaw.IndexOf('{'); $jsonEnd = $gvRaw.LastIndexOf('}')
if ($jsonStart -lt 0 -or $jsonEnd -le $jsonStart) {
    Write-Host "GitVersion raw output:`n$gvRaw"
    throw "dotnet-gitversion produced no JSON output"
}
$gv = $gvRaw.Substring($jsonStart, ($jsonEnd - $jsonStart + 1)) | ConvertFrom-Json
$verMajor = [int]$gv.Major; $verMinor = [int]$gv.Minor; $verBuild = [int]$gv.CommitsSinceVersionSource
$version = "$verMajor.$verMinor.$verBuild"
Write-Host "Version: $version   TargetFramework: $tfm"

# Clean previous publish output.
$publishDir = Join-Path $root 'publish'
Remove-Item -Recurse -Force $publishDir -ErrorAction SilentlyContinue
$appDir = Join-Path $publishDir 'AndroidSyncControl'
New-Item -ItemType Directory -Force -Path $appDir | Out-Null

# Publish framework-dependent, win-x64 (native assets: adb, XAudio2, scrcpy flatten correctly).
# GitVersion.MsBuild (Directory.Build.targets, Release) stamps the same version into the assembly.
dotnet publish $proj -c Release -r win-x64 --self-contained false -o $appDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Ship the license notices next to the binaries (MIT requires the notice to travel with every
# copy; the FFmpeg DLLs are LGPL-2.1 and adb/scrcpy-server are Apache-2.0, whose full texts go
# into licenses and are referenced from THIRD-PARTY-NOTICES.md).
Copy-Item (Join-Path $root 'LICENSE') -Destination $appDir -Force
Copy-Item (Join-Path $root 'THIRD-PARTY-NOTICES.md') -Destination $appDir -Force
Copy-Item (Join-Path $root 'licenses') -Destination $appDir -Recurse -Force

# Zip the published folder (archive keeps a top-level AndroidSyncControl\ folder).
$zipPath = Join-Path $publishDir "AndroidSyncControl-$version-$tfm.zip"
Compress-Archive -Path $appDir -DestinationPath $zipPath -Force
Write-Host "Packed: $zipPath"
