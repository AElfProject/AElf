#!/usr/bin/env bash
set -ev

TAG=$1
MYGET_API_KEY=$2
VERSION=`echo ${TAG} | cut -b 2-`

src_path=src/
contract_path=contract/
build_output=/tmp/aelf-build

if [[ -d ${build_output} ]]; then
    rm -rf ${build_output}
fi

dotnet restore AElf.sln

for path in ${src_path} ${contract_path} ;
do
    cd ${path}
    echo '---- build '${path}

    for name in `ls -lh | grep ^d | grep AElf | grep -v Tests| awk '{print $NF}'`;
    do
        if [[ -f ${name}/${name}.csproj ]] && [[ 1 -eq $(grep -c "GeneratePackageOnBuild" ${name}/${name}.csproj) ]];then
            echo ${name}/${name}.csproj
            dotnet build /clp:ErrorsOnly ${name}/${name}.csproj --configuration Release -P:Version=${VERSION} -P:Authors=AElf -o ${build_output}

            echo `ls ${build_output}/${name}.${VERSION}.nupkg`
            dotnet nuget push ${build_output}/${name}.${VERSION}.nupkg -k ${MYGET_API_KEY} -s https://www.myget.org/F/aelf-project/api/v3/index.json
        fi
    done
    cd ../
done
