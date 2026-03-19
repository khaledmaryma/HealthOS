@echo off
cd /d C:\d\LHH_Backup\LIS.Api
echo Starting LIS API on http://localhost:5050...
echo Press Ctrl+C to stop
dotnet run --urls "http://localhost:5050"

