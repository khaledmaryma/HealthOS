# LIS

## Structure
- LIS.Web (Angular)
- LIS.Api (ASP.NET Core Web API, .NET 9)

## Prerequisites
- Node.js 18+
- .NET SDK 9
- SQL Server (LocalDB or instance)

## Setup
```bash
# Frontend
cd LIS.Web
npm install
npm start

# API
cd ../LIS.Api
# adjust appsettings.json ConnectionStrings:DefaultConnection if needed
dotnet-ef database update
dotnet run
```

API health: GET http://localhost:5000/health (or port shown in console)
Angular dev: http://localhost:4200/
