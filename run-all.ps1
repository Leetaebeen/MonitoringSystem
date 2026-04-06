$projects = @(
    @{
        Title = "Monitoring Backend"
        ProjectPath = ".\MonitoringSystem.Backend\MonitoringSystem.Backend.csproj"
        Arguments = "--launch-profile http"
    },
    @{
        Title = "Monitoring Frontend"
        ProjectPath = ".\MonitoringSystem.Frontend\MonitoringSystem.Frontend.csproj"
        Arguments = "--launch-profile http"
    },
    @{
        Title = "Monitoring Simulator"
        ProjectPath = ".\MonitoringSystem.Simulator\MonitoringSystem.Simulator.csproj"
        Arguments = ""
    }
)

Write-Host "Starting Monitoring System projects..." -ForegroundColor Green

foreach ($project in $projects) {
    $command = "Set-Location '$PSScriptRoot'; " +
        "Write-Host '$($project.Title)' -ForegroundColor Cyan; " +
        "dotnet run --project `"$($project.ProjectPath)`" $($project.Arguments)"

    Start-Process powershell.exe -ArgumentList @(
        "-NoExit",
        "-Command",
        $command
    )
}

Write-Host "Requested startup for backend, frontend, and simulator." -ForegroundColor Green
