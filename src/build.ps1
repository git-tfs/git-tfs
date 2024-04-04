<#

.SYNOPSIS
This is a Powershell script to bootstrap a Cake build.

.DESCRIPTION
This Powershell script will download NuGet if missing, restore NuGet tools (including Cake)
and execute your Cake build script with the parameters you provide.

.PARAMETER Script
The build script to execute.
.PARAMETER Target
The build script target to run.
.PARAMETER Configuration
The build configuration to use.
.PARAMETER Verbosity
Specifies the amount of information to be displayed.
.PARAMETER WhatIf
Performs a dry run of the build script.
No tasks will be executed.

.LINK
http://cakebuild.net

Example of use: .\build.ps1 -Target "Package"
#>

[CmdletBinding()]
Param(
    [string]$Script = "build.cake",
    [string]$Target = "Default",
    [string]$Configuration = "Debug",
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity = "Verbose",
    [Alias("DryRun","Noop")]
    [switch]$WhatIf,
    [switch]$SkipToolPackageRestore,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs
)

Write-Host "Preparing to run build script..."

$PSScriptRoot = split-path -parent $MyInvocation.MyCommand.Definition;
$PAKET_EXE = Join-Path $PSScriptRoot "paket.exe"
$TOOLS_DIR = Join-Path $PSScriptRoot "packages/build"
$CAKE_EXE = Join-Path $TOOLS_DIR "Cake/Cake.exe"

# Is this a dry run?
$UseDryRun = "";
if($WhatIf.IsPresent) {
    $UseDryRun = "-dryrun"
}

# Restore tools with Paket?
if(-Not $SkipToolPackageRestore.IsPresent)
{
    Write-Verbose -Message "Restoring tools with Paket..."
    $PaketOutput = Invoke-Expression "&`"$PAKET_EXE`" restore"
    Write-Verbose -Message ($PaketOutput | out-string)

    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }
}

# Make sure that Cake has been installed.
if (!(Test-Path $CAKE_EXE)) {
    Throw "Could not find Cake.exe at $CAKE_EXE"
}

# Start Cake
Write-Host "Running build script..."
Invoke-Expression "& `"$CAKE_EXE`" `"$Script`" --paths_tools=`"$TOOLS_DIR`" --target=`"$Target`" --configuration=`"$Configuration`" --verbosity=`"$Verbosity`" $UseDryRun $ScriptArgs"
exit $LASTEXITCODE