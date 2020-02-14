#!/bin/bash

cd test/
for i in `ls -lh | grep ^d | grep .Tests$ | awk '{print $NF}'` ;
do
    cd $i/ && dotnet add package coverlet.msbuild
    sleep 1
    cd ..
done
cd ..
