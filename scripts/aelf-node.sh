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
sudo docker pull aelf/node:latest

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
sudo docker run -it -p 6800:6800 -v /Users/peng/.local/share/aelf:/app/aelf --name aelf-node-launcher aelf/node:latest dotnet AElf.Launcher.dll --db.type inmemory --node.account $ACCOUNT --mine.enable true --node.port 6800 --rpc.host 0.0.0.0 --rpc.port $PORT
