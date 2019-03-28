#!/bin/bash

TAG=$1
NUGET_API_KEY=$2

# TODO: verify version
VERSION=`echo $TAG | cut -b 2-`

# build
for name in `ls -lh | grep ^d | grep AElf |grep -v Test|awk '{print $NF}'`;
do
  dotnet build ${name}/${name}.csproj --configuration Release -P:Version=${VERSION} -P:Authors=AElf -o ../
done

dotnet build AElf.Contracts.TestKit/AElf.Contracts.TestKit.csproj --configuration Release -P:Version=${VERSION} -P:Authors=AElf -o ../


# push
for name  in `ls *.nupkg`;
do
  echo $name
  dotnet nuget push ${name}  -k $NUGET_API_KEY  -s https://api.nuget.org/v3/index.json
done