@ECHO OFF
dotnet restore
dotnet test NCalc.Tests -appveyor
dotnet pack NCalc --configuration Release