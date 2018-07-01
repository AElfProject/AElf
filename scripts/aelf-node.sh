#!/bin/bash
echo "Check parameter ..."
ACCOUNT=$1
if [ ! -n "$ACCOUNT" ]
then
    echo "Error: No account parameter provided."
    exit
fi

PORT=$2
if [ ! -n "$PORT" ]
then
    PORT=6900
    echo "  >> Use default port value: 6900"
else
    echo "  >> Use input port value as: $PORT"
fi

echo "Pull latest aelf/node image ..."
sudo docker pull aelf/node:dev-v0.1.1

echo "Check docker environment ..."
app=`sudo docker -v |grep version |wc -l`
if [ $app -eq 1 ]
then
    echo "  >> Docker installed."
else
    echo "  >> Error: Docker command not install."
    return
fi

echo "Delete container if exist ..."
process=`sudo docker ps -a | grep aelf-node-launcher | grep -v grep | grep -v sudo | wc -l`
if [ $process -eq 1 ]
then
    echo "  >> Stop container"
    sudo docker stop aelf-node-launcher
    echo "  >> Delete existed container"
    sudo docker rm aelf-node-launcher
fi

echo "Start launcher container ..."
sudo docker run -it -p 6800:6800 -v /home/aelf:/app/aelf -v /home/aelf/ChainInfo.json:/app/ChainInfo.json --name aelf-node-launcher aelf/node:dev-v0.1.1 dotnet AElf.Launcher.dll -t keyvalue --host 0.0.0.0 --b 172.31.13.96:6800 172.31.15.149:6800 172.31.7.19:6800 -m true --nodeaccount $ACCOUNT --port 6800 --rpc.port $PORT
