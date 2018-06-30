#!/bin/bash

# Parameter verify
echo "1. Check parameter"
ACCOUNT=$1
if [ ! -n "$ACCOUNT" ]
then
echo "Error: No account parameter provided."
exit
fi

PORT=$2
if [ ! -n "$PORT" ]
then
PORT=1234
echo "Use default port value: 1234"
else
echo "Use input port value as: $PORT"
fi

# Check docker installed or not
echo "2. Check docker command installed or not"
app=`sudo docker -v |grep version |wc -l`
if [ $app -eq 1 ]
then
echo "Docker installed."
else
echo "Error: Docker command not install."
return
fi

# Launch container
echo "3. Start launcher container"
sudo docker exec -it aelf-node-cli dotnet AElf.Launcher.dll -n true -t keyvalue --coinbase 4vu4EbcLL4vwxQV4sUHK/hJA --host 127.0.0.1 -m true --nodeaccount $ACCOUNT --port 6800 --rpc.port $PORT
