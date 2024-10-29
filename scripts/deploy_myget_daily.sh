#!/usr/bin/env bash
set -ev
#VERSION_PREFIX=$1  
MYGET_API_KEY="df3d8103-b5fa-4c61-aa00-c3c57611f958"  
  
# days since 1970-1-1 as build version  
#BUILD_VERSION=`expr $(date +%s) / 86400`  
VERSION="1.10.3.2"  
  
src_path=src/  
contract_path=contract/  
build_output=/tmp/aelf-build  
  
if [[ -d ${build_output} ]]; then  
    rm -rf ${build_output}  
fi  
  
dotnet restore AElf.All.sln  
  
for path in ${src_path} ${contract_path} ;  
do  
    cd ${path}  
    echo '---- build '${path}  
  
    for name in `ls -lh | grep ^d | grep AElf | grep -v Tests| awk '{print $NF}'`;  
    do  
        if [[ -f ${name}/${name}.csproj ]] && [[ 1 -eq $(grep -c "GeneratePackageOnBuild" ${name}/${name}.csproj) ]];then  
            echo ${name}/${name}.csproj  
            dotnet build /clp:ErrorsOnly ${name}/${name}.csproj --configuration Release -P:Version=${VERSION} -P:Authors=AElf -o ${build_output}  
        fi  
    done   
     cd ../  
done  
  
for path in ${src_path} ${contract_path} ;  
do  
    cd ${path}  
    echo '---- build '${path}  
  
    for name in `ls -lh | grep ^d | grep AElf | grep -v Tests| awk '{print $NF}'`;  
    do  
        if [[ -f ${name}/${name}.csproj ]] && [[ 1 -eq $(grep -c "GeneratePackageOnBuild" ${name}/${name}.csproj) ]];then  
            echo ${name}/${name}.csproj  
#            dotnet build /clp:ErrorsOnly ${name}/${name}.csproj --configuration Release -P:Version=${VERSION} -P:Authors=AElf -o ${build_output}  
  
            echo "push to myget"  
            echo `ls ${build_output}/${name}.${VERSION}.nupkg`  
            dotnet nuget push ${build_output}/${name}.${VERSION}.nupkg -k ${MYGET_API_KEY} -s https://www.myget.org/F/aelf-project-dev/api/v3/index.json  
        fi  
    done    
    cd ../  
done  
  
echo "all don"