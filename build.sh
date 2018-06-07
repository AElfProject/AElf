#!/bin/bash
set -ev

dotnet restore -s "https://nuget.cdn.azure.cn/v3/index.json" -s "https://api.nuget.org/v3/index.json" "AElf.sln"
dotnet test "AElf.Kernel.Tests/AElf.Kernel.Tests.csproj"
dotnet build "AElf.Kernel/AElf.Kernel.csproj"
