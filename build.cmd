@ECHO OFF
dotnet restore
dotnet msbuild test/testRunner/testRunner.proj
dotnet pack src/NCalc/NCalc.csproj --configuration Release -o bin/Release