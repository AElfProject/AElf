#!/bin/bash

dotnet restore -s "https://nuget.cdn.azure.cn/v3/index.json" -s "https://api.nuget.org/v3/index.json" -v quiet "AElf.sln"
dotnet build src/AElf.Contracts.Deployer/AElf.Contracts.Deployer.csproj
mkdir scripts/patcher
cp src/AElf.Contracts.Deployer/bin/Debug/netcoreapp3.0/*.* scripts/patcher
dotnet build /p:GeneratePackageOnBuild=false "AElf.sln"

if [[ $? -ne 0 ]] ; then
  echo "Build failed."
  exit 1
fi
