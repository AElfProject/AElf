#!/bin/bash

for i in *Tests ; do
  limit=$((${#i}+30))

  echo ""
  echo "Executing Tests for $i"
  printf '=%.0s' $(seq 1 $limit)
  echo ""

  dotnet test "$i" --no-build /p:CollectCoverage=true /p:CoverletOutputFormat='json%2copencover' \
  /p:CoverletOutput="../results/coverage" /p:MergeWith="../results/coverage.json" \
  /p:Exclude="[coverlet.*.tests?]*%2c[xunit.*]*%2c[AElf.Kernel.Consensus.Scheduler.*]*%2c[AElf.Database]AElf.Database.RedisProtocol.*%2c[AElf.Contracts.Authorization]*%2c[AElf.Test.Helpers]*%2c[AElf.CLI]AElf.CLI.Commands.*%2c[*]*Exception%2c[*.Tests]*" \
  /p:ExcludeByFile="../AElf.Runtime.CSharp.Core/Metadata/*.cs%2c../AElf.Kernel.SmartContract/Metadata/*.cs%2c../AElf.Database/RedisDatabase.cs"
  
  if [[ $? -ne 0 ]] ; then
    exit 1
  fi

done
