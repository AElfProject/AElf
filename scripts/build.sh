#!/bin/bash

dotnet restore -s "https://nuget.cdn.azure.cn/v3/index.json" -s "https://api.nuget.org/v3/index.json" "AElf.sln"
dotnet build "AElf.sln"

if [[ $? -ne 0 ]] ; then
  echo "Build failed."
  exit 1
fi