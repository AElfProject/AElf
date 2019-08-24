#!/usr/bin/env bash

VERSION_PREFIX=$1
MYGET_API_KEY=$2

# days since 1970-1-1 as build version
BUILD_VERSION=`expr $(date +%s) / 86400`
VERSION=${VERSION_PREFIX}-${BUILD_VERSION}
src_path=src/
contract_path=contract/
for path in $src_path $contract_path ;
do
    cd $path
    for name in `ls -lh | grep ^d | grep AElf | grep -v Tests| awk '{print $NF}'`;
    do
        if [[ -f ${name}/${name}.csproj ]] && [[ 1 -eq $(grep -c "GeneratePackageOnBuild" ${name}/${name}.csproj) ]];then
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
    dotnet nuget push ${name} -k ${MYGET_API_KEY} -s https://www.myget.org/F/aelf-project-dev/api/v3/index.json
    if [ "$?" != 0 ] ; then
        dotnet nuget push ${name} -k ${MYGET_API_KEY} -s https://www.myget.org/F/aelf-project-dev/api/v3/index.json
    else
        echo "successful!!!"
    fi
done
