#!/bin/bash
set -e
#cd `dirname $0`/..
for each_script in $(find . -name 'gen*_script')
do
    cd $(dirname $each_script)
    bash ./$(basename $each_script)
    cd -
done
