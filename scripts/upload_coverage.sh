#!/bin/bash

curl -s https://codecov.io/bash > codecov
chmod +x codecov
  
for i in *Tests ; do
  limit=$((${#i}+30))

  echo ""
  echo "Upload Coverage for $i"
  printf '=%.0s' $(seq 1 $limit)
  echo ""

  ./codecov -f "$i/obj/coverage.opencover.xml" -t $1

  if [[ $? -ne 0 ]] ; then
    exit 1
  fi

done
