param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'StrataShell.sln'
$appPath = Join-Path $repoRoot 'src\StrataShell.App\bin\Release\net8.0-windows10.0.19041.0\StrataShell.exe'
$probeProject = Join-Path $repoRoot 'tools\StrataShell.QaCapture\StrataShell.QaCapture.csproj'

if (-not $NoBuild) {
    dotnet build $solution -c Release --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Release build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $appPath)) {
    throw "StrataShell build output is missing: $appPath"
}
if (Get-Process StrataShell -ErrorAction SilentlyContinue) {
    throw 'StrataShell is already running; the accessibility probe would be ambiguous.'
}

$results = [System.Collections.Generic.List[object]]::new()

function Test-SurfaceAccessibility {
    param(
        [Parameter(Mandatory)] [string]$Surface,
        [Parameter(Mandatory)] [string]$WindowTitle,
        [Parameter(Mandatory)] [string[]]$Arguments,
        [Parameter(Mandatory)] [string[]]$ExpectedNames,
        [Parameter(Mandatory)] [string[]]$ExpectedFocusableNames,
        [Parameter(Mandatory)] [string]$FocusName,
        [hashtable]$Invocations = @{}
    )

    $launchArguments = @($Arguments) + '--qa-exit-after-ms=10000'
    $process = Start-Process -FilePath $appPath -ArgumentList $launchArguments -PassThru
    try {
        Start-Sleep -Seconds 3
        $jsonLines = & dotnet run --project $probeProject -c Release --no-build -- `
            --inspect-accessibility-title $WindowTitle
        if ($LASTEXITCODE -ne 0) {
            throw "$Surface UI Automation probe failed with exit code $LASTEXITCODE."
        }

        $report = ($jsonLines -join [Environment]::NewLine) | ConvertFrom-Json
        $names = @($report.elements | ForEach-Object Name | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        foreach ($expected in $ExpectedNames) {
            if ($names -notcontains $expected) {
                throw "$Surface is missing the accessible name '$expected'."
            }
        }
        foreach ($expected in $ExpectedFocusableNames) {
            $match = @($report.elements | Where-Object { $_.Name -eq $expected -and $_.IsKeyboardFocusable })
            if ($match.Count -eq 0) {
                throw "$Surface control '$expected' is not exposed as keyboard focusable."
            }
        }

        $noisyNames = @($names | Where-Object {
            $_ -match '^TaskbarShortcutItem \{' -or
            $_ -eq 'ManagedShell.WindowsTasks.ApplicationWindow' -or
            $_ -eq 'ManagedShell.WindowsTray.NotifyIcon'
        })
        if ($noisyNames.Count -gt 0) {
            throw "$Surface exposes implementation-object names through UI Automation."
        }

        $focusJson = & dotnet run --project $probeProject -c Release --no-build -- `
            --focus-accessibility-title $WindowTitle $FocusName
        if ($LASTEXITCODE -ne 0) {
            throw "$Surface could not move real keyboard focus to '$FocusName'."
        }
        $focusReport = ($focusJson -join [Environment]::NewLine) | ConvertFrom-Json
        if (-not $focusReport.focused) {
            throw "$Surface did not retain real keyboard focus on '$FocusName'."
        }

        foreach ($invocation in $Invocations.GetEnumerator()) {
            $invokeJson = & dotnet run --project $probeProject -c Release --no-build -- `
                --invoke-accessibility-title $WindowTitle $invocation.Key $invocation.Value
            if ($LASTEXITCODE -ne 0) {
                throw "$Surface could not invoke '$($invocation.Key)' and find '$($invocation.Value)'."
            }
            $invokeReport = ($invokeJson -join [Environment]::NewLine) | ConvertFrom-Json
            if (-not $invokeReport.popupFound) {
                throw "$Surface invocation '$($invocation.Key)' did not expose '$($invocation.Value)'."
            }
        }

        $results.Add([pscustomobject]@{
            Surface = $Surface
            WindowTitle = $WindowTitle
            ElementCount = $report.elementCount
            NamedElementCount = $names.Count
            NamedFocusableCount = @($report.elements | Where-Object {
                -not [string]::IsNullOrWhiteSpace($_.Name) -and $_.IsKeyboardFocusable
            }).Count
            ExpectedNamesVerified = $ExpectedNames.Count
            FocusVerified = $FocusName
            InvocationsVerified = $Invocations.Count
        })
    }
    finally {
        if (-not $process.HasExited) {
            $process.WaitForExit(12000) | Out-Null
        }
        $process.Refresh()
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            throw "$Surface QA process did not exit within its bounded timeout."
        }
        if ($process.ExitCode -ne 0) {
            throw "$Surface QA process exited with code $($process.ExitCode)."
        }
    }
}

try {
    Test-SurfaceAccessibility -Surface 'Settings overview' -WindowTitle 'StrataShell Settings' `
        -Arguments @('--qa-disable-taskbar', '--settings-tab=overview') `
        -ExpectedNames @('Overview', 'Open preview', 'Hide to tray', 'Save changes') `
        -ExpectedFocusableNames @('Overview', 'Open preview', 'Hide to tray', 'Save changes') `
        -FocusName 'Open preview'

    Test-SurfaceAccessibility -Surface 'Settings panel' -WindowTitle 'StrataShell Settings' `
        -Arguments @('--qa-disable-taskbar', '--settings-tab=panel') `
        -ExpectedNames @('Enable the full-screen panel', 'Toggle on a bare Windows-key press',
            'Panel tile size', 'Panel background opacity', 'Panel motion') `
        -ExpectedFocusableNames @('Enable the full-screen panel', 'Toggle on a bare Windows-key press',
            'Panel tile size', 'Panel background opacity', 'Panel motion') `
        -FocusName 'Panel tile size'

    Test-SurfaceAccessibility -Surface 'Settings taskbar' -WindowTitle 'StrataShell Settings' `
        -Arguments @('--qa-disable-taskbar', '--settings-tab=taskbar') `
        -ExpectedNames @('Enable the custom taskbar', 'Taskbar total height', 'Taskbar window rows',
            'Taskbar icon size', 'Pinned quick-launch shortcuts', 'Remove selected', 'Clear all') `
        -ExpectedFocusableNames @('Enable the custom taskbar', 'Taskbar total height', 'Taskbar window rows',
            'Taskbar icon size', 'Remove selected', 'Clear all') `
        -FocusName 'Taskbar window rows'

    Test-SurfaceAccessibility -Surface 'Settings startup' -WindowTitle 'StrataShell Settings' `
        -Arguments @('--qa-disable-taskbar', '--settings-tab=startup') `
        -ExpectedNames @('Run when I sign in', 'Start quietly in the notification area', 'Settings file path',
            'Open diagnostics', 'Check releases', 'Reset safe defaults') `
        -ExpectedFocusableNames @('Run when I sign in', 'Start quietly in the notification area',
            'Settings file path', 'Open diagnostics', 'Check releases', 'Reset safe defaults') `
        -FocusName 'Settings file path'

    Test-SurfaceAccessibility -Surface 'Full-screen panel' -WindowTitle 'StrataShell Panel' `
        -Arguments @('--qa-disable-taskbar', '--panel-primary') `
        -ExpectedNames @('Open StrataShell settings', 'Close full-screen panel', 'Search applications', 'Applications') `
        -ExpectedFocusableNames @('Open StrataShell settings', 'Close full-screen panel', 'Search applications') `
        -FocusName 'Search applications'

    Test-SurfaceAccessibility -Surface 'Custom taskbar' -WindowTitle 'StrataShell Taskbar' `
        -Arguments @('--qa-enable-taskbar', '--background') `
        -ExpectedNames @('Open full-screen Start panel', 'More quick-launch shortcuts', 'More running windows',
            'More notification icons', 'Open date and time settings') `
        -ExpectedFocusableNames @('Open full-screen Start panel', 'More quick-launch shortcuts', 'More running windows',
            'More notification icons', 'Open date and time settings') `
        -FocusName 'Open full-screen Start panel' `
        -Invocations @{
            'More quick-launch shortcuts' = 'Quick launch'
            'More running windows' = 'Running windows'
            'More notification icons' = 'Notification icons'
        }
}
finally {
    Start-Sleep -Seconds 2
    dotnet run --project $probeProject -c Release --no-build -- --restore-taskbar | Out-Null
}

[pscustomobject]@{
    Passed = $true
    SurfaceCount = $results.Count
    Surfaces = $results
} | ConvertTo-Json -Depth 5
