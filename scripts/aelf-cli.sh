#!/bin/bash
echo "Sync time info ..."
sudo ntpdate cn.pool.ntp.org

echo "Pull latest aelf/node image ..."
sudo docker pull aelf/node:latest

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
sudo docker run -it -v ~/.local/share/aelf:/app/aelf --name aelf-node-cli aelf/node:latest dotnet AElf.CLI2.dll $@
