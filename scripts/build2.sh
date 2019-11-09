#!/usr/bin/env bash

src_path=src/
contract_path=contract/

rm -rf ~/Downloads/aelf

for path in ${src_path} ${contract_path} ;
do
    cd ${path}
    for name in `ls -lh | grep ^d | grep AElf | grep -v Tests | awk '{print $NF}'`;
    do
        if [[ -f ${name}/${name}.csproj ]];then
            echo ${name}/${name}.csproj
            dotnet publish /clp:ErrorsOnly ${name}/${name}.csproj -o ~/Downloads/aelf
        fi
    done
    cd ../
done
