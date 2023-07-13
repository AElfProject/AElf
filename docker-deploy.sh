#!/bin/bash
set -ev

# publish docker
# AElf node
mkdir build
cp -r src/AElf.Launcher/contracts/* build
cp docker-start-chain.sh build/
dotnet publish AElf.All.sln /clp:ErrorsOnly -c Release -o build

docker build -t aelf/node build/
#docker tag aelf/node:${TAG} aelf/node:latest
#docker login -u="$DOCKER_USERNAME" -p="$DOCKER_PASSWORD"
#docker push aelf/node:${TAG}
#docker push aelf/node:latest
