$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'StrataShell.sln'
$jsonText = & dotnet list $solution package --vulnerable --include-transitive --format json
if ($LASTEXITCODE -ne 0) {
    throw "NuGet vulnerability audit failed with exit code $LASTEXITCODE."
}

$report = $jsonText | ConvertFrom-Json
$findings = @(
    foreach ($project in @($report.projects)) {
        foreach ($framework in @($project.frameworks)) {
            if ($null -eq $framework) {
                continue
            }

            $packages = @()
            if ($null -ne $framework.topLevelPackages) {
                $packages += @($framework.topLevelPackages)
            }
            if ($null -ne $framework.transitivePackages) {
                $packages += @($framework.transitivePackages)
            }

            foreach ($package in $packages) {
                if ($null -ne $package.vulnerabilities -and @($package.vulnerabilities).Count -gt 0) {
                    [pscustomobject]@{
                        Project = $project.path
                        Framework = $framework.framework
                        Package = $package.id
                        Version = $package.resolvedVersion
                        Vulnerabilities = $package.vulnerabilities
                    }
                }
            }
        }
    }
)

if ($findings.Count -gt 0) {
    $findings | ConvertTo-Json -Depth 8 | Write-Error
    throw "$($findings.Count) vulnerable NuGet package finding(s) detected."
}

Write-Output "NuGet vulnerability audit passed for $(@($report.projects).Count) project(s)."
