cli.sh
#!/bin/bash

echo "1. Check port parameter"
PORT=$1
if [ ! -n "$PORT" ]
then
PORT=1234
echo "Use default port value: 1234"
else
echo "Use input port value: $PORT"
fi

echo "2. Check docker command installed or not"
app=`sudo docker -v |grep version |wc -l`
if [ $app -eq 1 ]
then
echo "Docker installed."
else
echo "Error: Docker command not install."
exit
fi

# Pull latest image
echo "3. Pull latest aelf/node image"
sudo docker pull aelf/node:latest

# Check container exist or not
echo "4. Check container exist or not"
process=`sudo docker ps -a |grep aelf-node-cli |grep -v grep |grep -v sudo |wc -l`
if [ $process -eq 1 ]
then
# Stop container
echo "Stop container"
sudo docker stop aelf-node-cli
# Delete container
echo "Delete existed container"
sudo docker rm aelf-node-cli
fi

# Launch container
echo "5. Start cli container"
sudo docker run -it -p 6800:6800 -v /home/aelfdata:/app/aelf --name aelf-node-cli aelf/node:latest dotnet AElf.CLI.dll $PORT
