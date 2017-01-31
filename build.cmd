@ECHO OFF
dotnet restore
dotnet test NCalc.NetCore.Tests -appveyor
dotnet pack NCalc.NetCore --configuration Release