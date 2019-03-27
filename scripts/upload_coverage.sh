#!/bin/bash

curl -s https://codecov.io/bash > codecov
chmod +x codecov
./codecov -f "./results/coverage.opencover.xml" -t $1
