<#
.SYNOPSIS
    Keeps the report-rendering asset tree (Repository\Views) in sync with the canonical
    web-application asset tree (Projects\SealWebServer\wwwroot).

.DESCRIPTION
    The web application serves client assets from wwwroot; the report rendering engine serves
    the SAME assets from Repository\Views (see Report.AttachScriptPath / ClientAssets.cs).
    Both trees therefore hold identical copies of every third-party library (wwwroot\lib) and of
    the shared first-party helpers. This script makes wwwroot the single source of truth and
    refreshes the Repository\Views copies so the two trees cannot drift.

    Scope of the sync (wwwroot -> Repository\Views):
      - lib\*            ALL third-party libraries, EXCEPT jstree (web-UI only, not used by reports)
      - js\common.js, js\helpers.js, js\chartNVD3.js, js\datetime-moment.js   shared first-party scripts
      - css\seal.css                                                          shared first-party style

    Files that exist only in one tree are left untouched (e.g. wwwroot\js\swi-*.js and the jstree
    library are web-UI only; Repository\Views\css\site-colors.css and js\custom.js are report only).
#>
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$wwwroot   = Join-Path $scriptDir "wwwroot"
$views     = Join-Path $scriptDir "..\..\Repository\Views"
$views     = [System.IO.Path]::GetFullPath($views)

Write-Host "Syncing client assets: $wwwroot -> $views"

# 1) Third-party libraries (wwwroot\lib -> Views\lib), excluding the web-UI-only jstree library.
#    robocopy /E = include subfolders; no /MIR so nothing is deleted on the report side.
$jstree = Join-Path $wwwroot "lib\jstree"
robocopy "$wwwroot\lib" "$views\lib" /E /XD "$jstree" /NJH /NJS /NDL /NP /R:2 /W:1 | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed syncing lib (exit $LASTEXITCODE)" }

# 2) Shared first-party helpers (same relative path in both trees).
$shared = @(
    "js\common.js",
    "js\helpers.js",
    "js\chartNVD3.js",
    "js\datetime-moment.js",
    "css\seal.css"
)
foreach ($rel in $shared) {
    $src = Join-Path $wwwroot $rel
    $dst = Join-Path $views   $rel
    if (-not (Test-Path $src)) { Write-Warning "missing source $src"; continue }
    $dstDir = Split-Path $dst -Parent
    if (-not (Test-Path $dstDir)) { New-Item -ItemType Directory -Force -Path $dstDir | Out-Null }
    # Copy only when content differs, to avoid needless writes / git churn.
    $copy = $true
    if (Test-Path $dst) {
        $copy = (Get-FileHash $src).Hash -ne (Get-FileHash $dst).Hash
    }
    if ($copy) { Copy-Item -Force $src $dst; Write-Host "  updated $rel" }
}

Write-Host "Client asset sync complete."
