#!/bin/bash

dotnet restore -s "https://nuget.cdn.azure.cn/v3/index.json" -s "https://api.nuget.org/v3/index.json" "AElf.sln"
dotnet build

if [ $? -ne 0 ] ; then
  echo "Build failed."
  exit 1
fi

for i in *Tests ; do
  limit=$((${#i}+20))

  echo ""
  echo "Executing Tests for $i"
  printf '=%.0s' $(seq 1 $limit)
  echo ""

  dotnet test "$i" --verbosity quiet --no-build
  if [ $? -ne 0 ] ; then
    exit 1
  fi
done
