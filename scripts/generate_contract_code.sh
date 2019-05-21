#!/usr/bin/env bash
scriptdir=`dirname "$0"`

bash "${scriptdir}/download_binary.sh"

plugin="${scriptdir}/contract_csharp_plugin"

solutiondir=`dirname ${scriptdir}`

protoc --proto_path=${solutiondir}/protobuf \
--csharp_out=./Protobuf/Generated \
--csharp_opt=file_extension=.g.cs \
--contract_out=./Protobuf/Generated \
--plugin=protoc-gen-contract=${plugin} \
$@
