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
cp ~/.nuget/packages/grpc.tools/1.14.1/tools/${name}/grpc_csharp_plugin ./Grpc
protoc --proto_path=../protobuf --csharp_out=./Grpc/Protobuf/Generated --csharp_opt=file_extension=.g.cs --grpc_out=./Grpc/Protobuf/Generated header_info.proto --plugin=protoc-gen-grpc=./Grpc/grpc_csharp_plugin
