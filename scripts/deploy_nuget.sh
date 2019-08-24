#!/bin/bash
set -ev

TAG=$1
NUGET_API_KEY=$2
VERSION=`echo ${TAG} | cut -b 2-`
src_path=src/
contract_path=contract/
for path in $path1 $path2 ;
do
    cd $path
    for name in `ls -lh | grep ^d | grep AElf | grep -v Tests | awk '{print $NF}'`;
    do
        if [[ -f ${name}/${name}.csproj ]] && [[ 1 -eq $(grep -c "GeneratePackageOnBuild"  ${name}/${name}.csproj) ]];then
            dotnet build /clp:ErrorsOnly ${name}/${name}.csproj --configuration Release -P:Version=${VERSION} -P:Authors=AElf -o ../
        fi
    done
    sleep 10
    cd ../
done
# push
for name in `ls *.nupkg`;
do
    echo ${name}
    dotnet nuget push ${name} -k ${NUGET_API_KEY} -s https://api.nuget.org/v3/index.json
    if [ "$?" != 0 ] ; then
        dotnet nuget push ${name} -k ${NUGET_API_KEY} -s https://api.nuget.org/v3/index.json
    else
        echo "successful!!!"
    fi
done
