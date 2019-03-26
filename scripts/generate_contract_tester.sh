#!/usr/bin/env bash
scriptdir=`dirname "$0"`

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
    plugin="${scriptdir}/contract_csharp_plugin_osx"
elif [ $osn == "linux" ]; then
    plugin="${scriptdir}/contract_csharp_plugin_linux"
fi

protoc --proto_path=../protobuf \
--csharp_out=internal_access:./Protobuf/Generated \
--csharp_opt=file_extension=.g.cs \
--contract_opt=tester \
--contract_out=./Protobuf/Generated \
--plugin=protoc-gen-contract="${plugin}" \
$@
