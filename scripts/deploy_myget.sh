#!/usr/bin/env bash
set -ev

TAG=$1
MYGET_API_KEY=$2
VERSION=`echo ${TAG} | cut -b 2-`

src_path=src/
contract_path=contract/
for path in ${src_path} ${contract_path} ;
do
    cd ${path}
    for name in `ls -lh | grep ^d | grep AElf | grep -v Tests | awk '{print $NF}'`;
    do
        if [[ -f ${name}/${name}.csproj ]] && [[ 1 -eq $(grep -c "GeneratePackageOnBuild"  ${name}/${name}.csproj) ]];then
            dotnet build /clp:ErrorsOnly ${name}/${name}.csproj --configuration Release -P:Version=${VERSION} -P:Authors=AElf -o ../
        fi
    done
    for name in `ls *.nupkg`;
    do
        echo ${name}
        dotnet nuget push ${name} -k ${MYGET_API_KEY} -s https://www.myget.org/F/aelf-project/api/v3/index.json
    done
    cd ../
done
