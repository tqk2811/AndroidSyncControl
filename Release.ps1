# Prepare a release locally: regenerate CHANGELOG.md, stage it and optionally commit and push it.
# Publishing is a separate step: pushing a tag vM.N.P starts the CI release build.
#
# Usage (from repo root):
#   .\Release.ps1                        # regenerate CHANGELOG.md + stage it, then commit yourself
#   .\Release.ps1 -Message "msg"         # also commit
#   .\Release.ps1 -Message "msg" -Push   # also push the branch
#
# Then publish from master:
#   git tag v1.2.3
#   git push origin v1.2.3

param(
    [string]$Message,
    [switch]$Push
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

# 1) Regenerate CHANGELOG.md (full local timeline).
& "$PSScriptRoot\Changelog.ps1"
if ($LASTEXITCODE -ne 0) { throw "Changelog.ps1 failed (exit $LASTEXITCODE)" }

# 2) Stage the changelog.
git add CHANGELOG.md

# 3) Nothing staged at all -> nothing to commit.
$staged = git diff --cached --name-only
if ([string]::IsNullOrWhiteSpace(($staged | Out-String))) {
    Write-Host ""
    Write-Host "Nothing to commit (CHANGELOG.md unchanged, no other staged changes)."
    return
}

# 4) No message -> leave it staged for the user to commit.
if (-not $Message) {
    Write-Host ""
    Write-Host "CHANGELOG.md regenerated and staged. Commit it, then tag the release."
    return
}

# 5) Confirm before the commit/push.
Write-Host ""
Write-Host "About to commit:" -ForegroundColor Yellow
Write-Host "  message : $Message"
Write-Host "  staged  : $($staged -join ', ')"
if ($Push) { Write-Host "  then    : git push" }
$null = Read-Host "Press Enter to proceed (Ctrl+C to abort)"

git commit -m $Message
if ($LASTEXITCODE -ne 0) { throw "git commit failed (exit $LASTEXITCODE)" }

# 6) Push the branch.
if ($Push) {
    git push
    if ($LASTEXITCODE -ne 0) { throw "git push failed (exit $LASTEXITCODE)" }
}
Write-Host "Done. To publish from master: git tag vM.N.P; git push origin vM.N.P"
