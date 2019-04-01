#!/bin/bash
set -ev

TAG=$1
NUGET_API_KEY=$2
VERSION=`echo ${TAG} | cut -b 2-`

for name in `ls -lh | grep ^d | grep AElf | grep -v Tests | awk '{print $NF}'`;
do
    if [[ -f ${name}/${name}.csproj ]] && [[ 1 -eq $(grep -c "GeneratePackageOnBuild"  ${name}/${name}.csproj) ]];then
        dotnet build ${name}/${name}.csproj --configuration Release -P:Version=${VERSION} -P:Authors=AElf -o ../
    fi
done

# push
for name in `ls *.nupkg`;
do
    echo ${name}
    dotnet nuget push ${name} -k ${NUGET_API_KEY} -s https://api.nuget.org/v3/index.json
done
