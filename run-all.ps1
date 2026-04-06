Write-Host "Monitoring System의 모든 프로젝트를 시작합니다..." -ForegroundColor Green

# cmd.exe의 start 명령어를 사용하여 각각의 프로젝트를 새 창에서 실행하고 제목을 지정합니다.
Start-Process "cmd.exe" -ArgumentList "/c start `"Backend API`" dotnet run --project .\MonitoringSystem.Backend\MonitoringSystem.Backend.csproj --launch-profile https"
Start-Process "cmd.exe" -ArgumentList "/c start `"Simulator`" dotnet run --project .\MonitoringSystem.Simulator\MonitoringSystem.Simulator.csproj"
Start-Process "cmd.exe" -ArgumentList "/c start `"Frontend`" dotnet run --project .\MonitoringSystem.Frontend\MonitoringSystem.Frontend.csproj --launch-profile https"

Write-Host "모든 프로젝트가 시작되었습니다!" -ForegroundColor Green
