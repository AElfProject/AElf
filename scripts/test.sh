#!/bin/bash

for i in *Tests ; do
  limit=$((${#i}+20))

  echo ""
  echo "Executing Tests for $i"
  printf '=%.0s' $(seq 1 $limit)
  echo ""

  dotnet test "$i" --no-build /p:CollectCoverage=true /p:CoverletOutputFormat='json%2copencover' \
 /p:CoverletOutput="../results/coverage" /p:MergeWith="../results/coverage.json" \
 /p:Exclude="[coverlet.*.tests?]*" /p:Exclude="[xunit.*]*" /p:Exclude="[*]*ssdb*" /p:Exclude="[*]*redis*"
  if [ $? -ne 0 ] ; then
    exit 1
  fi
done
