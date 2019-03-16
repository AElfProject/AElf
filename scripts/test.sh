#!/bin/bash

dotnet test "AElf.sln" --no-build /p:CollectCoverage=true /p:CoverletOutputFormat='json%2copencover' \
  /p:CoverletOutput="../results/coverage" /p:MergeWith="../results/coverage.json" \
  /p:Exclude="[coverlet.*.tests?]*%2c[xunit.*]*%2c[AElf.Database]AElf.Database.RedisProtocol.*%2c[AElf.Contracts.Authorization]*%2c[AElf.Test.Helpers]*%2c[AElf.CLI]AElf.CLI.Commands.*" \
  /p:ExcludeByFile="../AElf.Runtime.CSharp.Core/Metadata/*.cs%2c../AElf.Kernel.SmartContract/Metadata/*.cs"
