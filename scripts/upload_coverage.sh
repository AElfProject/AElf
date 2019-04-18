#!/bin/bash

curl -s https://codecov.io/bash > codecov
chmod +x codecov
./codecov -f "../test/results/coverage.opencover.xml" -t $1
