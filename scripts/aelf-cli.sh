#!/bin/bash
echo "Check port parameter ..."
PORT=$1
if [ ! -n "$PORT" ]
then
    PORT=6900
    echo "  >> Use default port value: 6900"
else
    echo "  >> Use input port value: $PORT"
fi

echo "Sync time info ..."
sudo ntpdate cn.pool.ntp.org

echo "Pull latest aelf/node image ..."
sudo docker pull aelf/node:dev-v0.1.1

echo "Delete container if exist ..."
process=`sudo docker ps -a | grep aelf-node-cli | grep -v grep | grep -v sudo | wc -l`
if [ $process -eq 1 ]
then
    echo "  >> Stop container ..."
    sudo docker stop aelf-node-cli

    echo "  >> Delete existed container ..."
    sudo docker rm aelf-node-cli
fi

echo "Start cli container ..."
sudo docker run -it -v /home/aelf:/app/aelf --name aelf-node-cli aelf/node:dev-v0.1.1 dotnet AElf.CLI.dll $PORT
