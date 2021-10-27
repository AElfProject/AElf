#!/bin/bash

cd test/

for i in *Tests ; do
  echo ""
  echo "### Executing Tests for $i:"

  time dotnet test "$i" --no-build /p:CollectCoverage=true /p:CoverletOutputFormat='json%2copencover' \
  /p:CoverletOutput="../results/coverage" /p:MergeWith="../results/coverage.json" \
  /p:Exclude="[coverlet.*.tests?]*%2c[xunit.*]*%2c[AElf.Kernel.Consensus.Scheduler.*]*%2c[AElf.Database]AElf.Database.RedisProtocol.*%2c[AElf.Contracts.Authorization]*%2c[AElf.Test.Helpers]*%2c[*]*Exception%2c[*.Tests]*%2c[AElf.Contracts.TestContract.BasicFunctionWithParallel]*%2c[AElf.Contracts.GenesisUpdate]*" \
  /p:ExcludeByFile="../../src/AElf.Runtime.CSharp.Core/Metadata/*.cs%2c../../src/AElf.Kernel.SmartContract/Metadata/*.cs%2c../../src/AElf.Database/RedisDatabase.cs%2c../../test/*.TestBase/*.cs" \
  > /tmp/test.log

  if [[ $? -ne 0 ]] ; then
    echo "Test Run Failed."
    cat /tmp/test.log
    exit 1
  fi

  echo "Test Run Successful."

done
