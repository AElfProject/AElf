#!/bin/bash

dotnet restore -s "https://nuget.cdn.azure.cn/v3/index.json" -s "https://api.nuget.org/v3/index.json" "AElf.sln"
dotnet build

if [ $? -ne 0 ] ; then
  echo "Build failed."
  exit 1
fi

dotnet test --no-build /p:CollectCoverage=true /p:CoverletOutputFormat='json%2copencover' \
 /p:CoverletOutput="../results/coverage" /p:MergeWith="../results/coverage.json" \
 /p:Exclude="[coverlet.*.tests?]*" /p:Exclude="[*tests?]*" /p:Exclude="[*Tests?]*" /p:Exclude="[xunit.*]*"

if [ $? -ne 0 ] ; then
  echo "Test failed."
  exit 1
fi