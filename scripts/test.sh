#!/bin/bash

for i in *Tests ; do
  limit=$((${#i}+20))

  echo ""
  echo "Executing Tests for $i"
  printf '=%.0s' $(seq 1 $limit)
  echo ""

  dotnet test "$i" --no-build /p:CollectCoverage=true /p:CoverletOutputFormat='json%2copencover' \
 /p:CoverletOutput="../results/coverage" /p:MergeWith="../results/coverage.json" \
 /p:Exclude="[coverlet.*.tests?]*%2c[xunit.*]*%2c[AElf.Database.RedisProtocol.*]*%2c[AElf.Contracts.Authorization.*]*%2c[AElf.Test.Helpers.*]"
  if [ $? -ne 0 ] ; then
    exit 1
  fi
done
