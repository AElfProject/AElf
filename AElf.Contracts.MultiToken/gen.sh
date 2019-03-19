#!/usr/bin/env bash

d="Darwin"
l="Linux"
if [ "$(uname -s | grep ${d})" != "" ]; then
  osn="macosx"
elif [ "$(uname -s | grep ${l})" != "" ]; then
  osn="linux"
else
  osn="windows"
fi

if [ $osn == "macosx" ]; then
    chmod a+x contract_csharp_plugin
    protoc --proto_path=./proto \
    --csharp_out=./Generated \
    --csharp_opt=file_extension=.g.cs \
    --contract_out=./Generated token_contract.proto \
    --plugin=protoc-gen-contract=./contract_csharp_plugin
elif [ $osn == "linux" ]; then
    # in ci environment
    protoc --proto_path=./proto \
    --csharp_out=./Generated \
    --csharp_opt=file_extension=.g.cs \
    token_contract.proto 
fi


