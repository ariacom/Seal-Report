# Regenerates the per-file minified Web Interface scripts (wwwroot/js/swi-*.min.js)
# from the TypeScript-compiled swi-*.js, using the uglify-js CLI (npm i -g uglify-js).
#
# These minified files are what the web shell loads outside the Development environment
# (see Views/Home/Main.cshtml). Load order matters, but each file is minified on its own;
# uglify-js preserves top-level names by default so the cross-file globals
# (SWIUtil, SWIGateway, SWIMain, WebApplicationName, ...) are kept intact.
#
# Best-effort: if uglifyjs is not installed the existing (committed) .min.js are left as-is,
# so a build machine without the tool still ships working minified output.

$ErrorActionPreference = 'Stop'
$jsDir = Join-Path $PSScriptRoot 'wwwroot\js'
$files = 'swi-utils', 'swi-gateway', 'swi-main', 'swi-ai'

if (-not (Get-Command uglifyjs -ErrorAction SilentlyContinue)) {
    Write-Warning "uglifyjs not found (npm install -g uglify-js); keeping existing swi-*.min.js"
    exit 0
}

foreach ($f in $files) {
    $src = Join-Path $jsDir "$f.js"
    $out = Join-Path $jsDir "$f.min.js"
    if (Test-Path $src) {
        & uglifyjs $src -c -m -o $out
        Write-Host "minified $f.js -> $f.min.js"
    }
    else {
        Write-Warning "source not found: $src"
    }
}
