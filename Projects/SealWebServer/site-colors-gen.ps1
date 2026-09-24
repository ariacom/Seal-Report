# Generates site-colors.css (site colors of the Seal Report Web Report Server) from ONE primary color.
#   Development (writes wwwroot\css\site-colors.css next to this script):
#     .\site-colors-gen.ps1 -Primary "#908A85"
#   Production: the setup installs this script in the Core folder. Target the site that is actually served:
#   a published IIS site keeps its own copy (the publication never overwrites an existing site-colors.css).
#     powershell -ExecutionPolicy Bypass -File "C:\Program Files\Seal Report\Core\site-colors-gen.ps1" -Primary "#908A85" -Out "C:\inetpub\wwwroot\Seal Report\wwwroot\css\site-colors.css"
#   Reload the page afterwards, no restart needed.
param([string]$Primary = "#337ab7", [string]$Out = [System.IO.Path]::Combine($PSScriptRoot, "wwwroot", "css", "site-colors.css"))

function HexToRgb([string]$hex) { $h = $hex.TrimStart('#'); @([Convert]::ToInt32($h.Substring(0,2),16), [Convert]::ToInt32($h.Substring(2,2),16), [Convert]::ToInt32($h.Substring(4,2),16)) }
function RgbToHex($rgb) { "#" + (($rgb | ForEach-Object { "{0:x2}" -f [int][Math]::Round([Math]::Max(0.0, [Math]::Min(255.0, $_))) }) -join "") }
function RgbToHsl($rgb) {
    $r = $rgb[0]/255.0; $g = $rgb[1]/255.0; $b = $rgb[2]/255.0
    $max = [Math]::Max($r, [Math]::Max($g, $b)); $min = [Math]::Min($r, [Math]::Min($g, $b))
    $l = ($max + $min) / 2; $d = $max - $min
    if ($d -eq 0) { return @(0, 0, $l) }
    $s = if ($l -gt 0.5) { $d / (2 - $max - $min) } else { $d / ($max + $min) }
    if ($max -eq $r) { $h = (($g - $b) / $d) % 6 } elseif ($max -eq $g) { $h = ($b - $r) / $d + 2 } else { $h = ($r - $g) / $d + 4 }
    $h = $h * 60; if ($h -lt 0) { $h += 360 }
    @($h, $s, $l)
}
function Hue2Rgb([double]$p, [double]$q, [double]$t) {
    if ($t -lt 0) { $t += 1 }
    if ($t -gt 1) { $t -= 1 }
    if ($t -lt (1/6)) { return $p + ($q - $p) * 6 * $t }
    if ($t -lt 0.5) { return $q }
    if ($t -lt (2/3)) { return $p + ($q - $p) * ((2/3) - $t) * 6 }
    return $p
}
function HslToRgb($hsl) {
    [double]$h = $hsl[0] / 360.0; [double]$s = $hsl[1]; [double]$l = $hsl[2]
    if ($s -eq 0) { $v = $l * 255; return @($v, $v, $v) }
    [double]$q = if ($l -lt 0.5) { $l * (1 + $s) } else { $l + $s - $l * $s }
    [double]$p = 2 * $l - $q
    [double]$r = (Hue2Rgb $p $q ($h + (1/3))) * 255
    [double]$g = (Hue2Rgb $p $q $h) * 255
    [double]$b = (Hue2Rgb $p $q ($h - (1/3))) * 255
    @($r, $g, $b)
}
# Shade: lightness minus N percentage points (Bootstrap darken()) for a normal color,
# plus N points (lighten) when the primary color is dark, so hover/border states stay visible
function Shade([string]$hex, [double]$pct) { $hsl = RgbToHsl (HexToRgb $hex); if ($hsl[2] -gt 0.35) { $hsl[2] = [Math]::Max(0.0, $hsl[2] - $pct / 100) } else { $hsl[2] = [Math]::Min(1.0, $hsl[2] + $pct / 100) }; RgbToHex (HslToRgb $hsl) }
# Bootstrap darken(): lightness minus N percentage points
function Darken([string]$hex, [double]$pct) { $hsl = RgbToHsl (HexToRgb $hex); $hsl[2] = [Math]::Max(0.0, $hsl[2] - $pct / 100); RgbToHex (HslToRgb $hsl) }
# Tint: mix of the color with white (ratio = share of the color)
function Tint([string]$hex, [double]$ratio) { $rgb = HexToRgb $hex; RgbToHex @(($rgb | ForEach-Object { $_ * $ratio + 255 * (1 - $ratio) })) }

$P = $Primary.ToLower(); if (-not $P.StartsWith('#')) { $P = "#$P" }
$border = Shade $P 5; $hover = Shade $P 10; $hoverBorder = Shade $P 17; $linkHover = Shade $P 15; $tint = Tint $P 0.41

$lines = @(
"/* Site colors of the Seal Report Web Report Server (banner, folder tree and primary buttons).",
"   Loaded through the 'Web CSS Files' server configuration (Configuration.xml, WebCssFiles)",
"   after the Seal Report stylesheets, so the rules below override the defaults.",
"   Edit the colors to adapt the site: no rebuild or restart needed, reload the page.",
"   The setup and the Web Server publication never overwrite an existing site-colors.css.",
"   Palette derived from the primary color $P (site-colors-gen.ps1 -Primary $P):",
"   border $border | hover $hover | hover border $hoverBorder | link hover $linkHover | selection tint $tint */",
"",
"/* Banner background */",
"#bar_top {",
"    background-color: $P !important;",
"}",
"",
"/* Active tab in the banner (Report / Information / Messages) */",
"#bar_top .nav-link.sr_tab.active {",
"    background-color: $hover !important;",
"}",
"",
"/* Banner texts and icons */",
".navbar-white {",
"    color: white !important;",
"}",
"",
"/* Banner texts and icons on hover */",
".navbar-white:hover {",
"    color: #d9d9d9 !important;",
"}",
"",
"/* Selected folder in the folder tree */",
".swi-tree-node.selected {",
"    background: $tint !important;",
"}",
"",
"/* Primary buttons (reports menu button, toolbar and dialog buttons): normal and disabled state.",
"   The background needs !important to win over the dialog-specific rules of sealweb.css. */",
".btn-primary, .btn-primary:disabled, .btn-primary.disabled {",
"    background-color: $P !important;",
"    border-color: $border;",
"}",
"",
"/* Primary buttons on hover, focus or when pressed */",
".btn-primary:hover, .btn-primary:focus, .btn-primary:active, .btn-primary.active, .show > .btn-primary.dropdown-toggle {",
"    background-color: $hover !important;",
"    border-color: $hoverBorder;",
"}",
"",
"/* Outline primary buttons */",
".btn-outline-primary {",
"    color: $P !important;",
"    border-color: $P;",
"}",
"",
".btn-outline-primary:hover, .btn-outline-primary:focus, .btn-outline-primary:active, .btn-outline-primary.active {",
"    background-color: $P !important;",
"    border-color: $P;",
"    color: white !important;",
"}",
"",
"/* Table pagination: current page */",
".pagination .page-item.active .page-link {",
"    background-color: $P !important;",
"    border-color: $P !important;",
"    color: white;",
"}",
"",
"/* Table pagination: other page links and their hover color */",
".pagination .page-item:not(.disabled):not(.active) .page-link {",
"    color: $P;",
"}",
"",
".pagination .page-item:not(.disabled):not(.active) .page-link:hover {",
"    color: $linkHover;",
"}",
""
)
[System.IO.File]::WriteAllText($Out, ($lines -join "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
"primary=$P border=$border hover=$hover hoverBorder=$hoverBorder linkHover=$linkHover tint=$tint -> $Out"
