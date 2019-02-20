#!/bin/bash

dotnet tool install --global coverlet.console
dotnet restore -s "https://nuget.cdn.azure.cn/v3/index.json" -s "https://api.nuget.org/v3/index.json" "AElf.sln"
dotnet build

if [ $? -ne 0 ] ; then
  echo "Build failed."
  exit 1
fi

dotnet test --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput='./result.xml'

if [ $? -ne 0 ] ; then
  echo "Test failed."
  exit 1
fi