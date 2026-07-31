@echo off
REM Publish and register the offsite agent as a Windows Service (run as Administrator).
set ROOT=%~dp0..
set OUT=C:\Timekeeping\Agent

dotnet publish "%ROOT%\src\Timekeeping.Agent\Timekeeping.Agent.csproj" -c Release -r win-x86 --self-contained false -o "%OUT%"
if errorlevel 1 exit /b 1

sc.exe query PVAOTimekeepingAgent >nul 2>&1
if %errorlevel%==0 (
  sc.exe stop PVAOTimekeepingAgent
  sc.exe delete PVAOTimekeepingAgent
)

sc.exe create PVAOTimekeepingAgent binPath= "%OUT%\Timekeeping.Agent.exe" start= auto DisplayName= "PVAO Timekeeping Agent"
sc.exe description PVAOTimekeepingAgent "Polls ZKTeco biometrics and syncs attendance to HQ over VPN"
sc.exe start PVAOTimekeepingAgent
echo Installed. Edit "%OUT%\appsettings.json" then: sc.exe restart PVAOTimekeepingAgent
