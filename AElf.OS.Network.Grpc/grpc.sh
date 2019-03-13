#!/usr/bin/env bash
b="64"
if [ "$(uname -m | grep ${b})" != "" ]; then
  osb="x64"
else
  osb="x86"
fi
d="Darwin"
l="Linux"
if [ "$(uname -s | grep ${d})" != "" ]; then
  osn="macosx"
elif [ "$(uname -s | grep ${l})" != "" ]; then
  osn="linux"
else
  osn="windows"
fi

name="${osn}_${osb}"
cp ~/.nuget/packages/grpc.tools/1.19.0/tools/${name}/grpc_csharp_plugin ./
protoc --proto_path=../protobuf \
--csharp_out=./Generated \
--csharp_opt=file_extension=.g.cs \
--grpc_out=./Generated peer_service.proto \
--plugin=protoc-gen-grpc=./grpc_csharp_plugin
