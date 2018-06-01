#!/bin/bash
set -ev

TAG=$1
DOCKER_USERNAME=$2
DOCKER_PASSWORD=$3

# AElf node
dotnet publish -c Release AElf.Launcher
docker build -t aelf/node:$TAG AElf.Launcher/bin/Release/netcoreapp2.0/publish/.
docker tag aelf/node:$TAG aelf/node:latest
docker login -u="$DOCKER_USERNAME" -p="$DOCKER_PASSWORD"
docker push aelf/node:$TAG
docker push aelf/node:latest
