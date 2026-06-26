#run as Administrator
#
# Installs the Seal Report Scheduler Service for this installation.
# The binary path is resolved from this script's own folder, so it works
# regardless of where Seal Report was installed (not just C:\Program Files).
#
# For a side-by-side installation, pass -Instance to register a uniquely
# named service so it does not collide with another instance, e.g.:
#     .\SealSchedulerServiceInstall.ps1 -Instance "Prod"
param([string]$Instance = "")

$name = "Seal Report Scheduler Service"
if ($Instance -ne "") { $name = "$name - $Instance" }

$bin = Join-Path $PSScriptRoot "SealSchedulerService.exe"

New-Service -Name $name -BinaryPathName "`"$bin`"" -DisplayName $name -StartupType Automatic

#remove the Service
#$name = "Seal Report Scheduler Service"   # add the same -Instance suffix used above
#Stop-Service -Name $name -ErrorAction SilentlyContinue
#sc.exe delete "$name"
