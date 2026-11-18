#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs every backend test project with code coverage and enforces the constitution's thresholds.

.DESCRIPTION
    Quality gate (constitution, "Development Workflow and Quality Gates"):
      - Domain + Application line coverage  >= 90%
      - Overall backend line coverage       >= 80%
    Layers without coverable lines yet are reported as "n/a" and do not fail the gate.
    The HTML report is written to artifacts/coverage/report/index.html.
#>
[CmdletBinding()]
param(
    [double] $CoreThreshold = 90,
    [double] $OverallThreshold = 80,
    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts/coverage'
$results = Join-Path $artifacts 'results'
$report = Join-Path $artifacts 'report'

if (Test-Path $artifacts) { Remove-Item $artifacts -Recurse -Force }

Push-Location $root
try {
    dotnet tool restore | Out-Null

    dotnet test --solution TaskLane.slnx --configuration $Configuration `
        --results-directory $results `
        --coverage --coverage-output-format cobertura
    if ($LASTEXITCODE -ne 0) { throw "Tests failed (exit code $LASTEXITCODE)." }

    dotnet reportgenerator `
        "-reports:$results/**/*.cobertura.xml" `
        "-targetdir:$report" `
        '-reporttypes:Html;JsonSummary;MarkdownSummaryGithub' `
        '-assemblyfilters:+TaskLane.Domain;+TaskLane.Application;+TaskLane.Infrastructure;+TaskLane.Api' `
        '-classfilters:-TaskLane.Infrastructure.Persistence.Migrations.*;-Microsoft.*;-System.*'
    if ($LASTEXITCODE -ne 0) { throw "ReportGenerator failed (exit code $LASTEXITCODE)." }
}
finally {
    Pop-Location
}

$summary = Get-Content (Join-Path $report 'Summary.json') -Raw | ConvertFrom-Json
$assemblies = @($summary.coverage.assemblies)

function Get-LineCoverage([object[]] $items) {
    $coverable = ($items | Measure-Object -Property coverablelines -Sum).Sum
    $covered = ($items | Measure-Object -Property coveredlines -Sum).Sum
    if (-not $coverable) { return $null }
    return [math]::Round(100 * $covered / $coverable, 2)
}

$core = Get-LineCoverage @($assemblies | Where-Object { $_.name -in 'TaskLane.Domain', 'TaskLane.Application' })
$overall = Get-LineCoverage $assemblies

$failures = @()
foreach ($gate in @(
        @{ Name = 'Domain + Application'; Value = $core; Threshold = $CoreThreshold },
        @{ Name = 'Overall backend'; Value = $overall; Threshold = $OverallThreshold })) {
    $display = if ($null -eq $gate.Value) { 'n/a (no coverable lines)' } else { "$($gate.Value)%" }
    Write-Host ("{0,-22} {1,-26} (threshold {2}%)" -f $gate.Name, $display, $gate.Threshold)
    if ($null -ne $gate.Value -and $gate.Value -lt $gate.Threshold) { $failures += $gate.Name }
}

Write-Host "Report: $(Join-Path $report 'index.html')"
if ($failures) { throw "Coverage below threshold: $($failures -join ', ')." }
