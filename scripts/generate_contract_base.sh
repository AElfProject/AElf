#!/usr/bin/env bash
scriptdir=`dirname "$0"`

bash "${scriptdir}/download_binary.sh"

plugin="${scriptdir}/contract_csharp_plugin"

solutiondir=`dirname ${scriptdir}`

protoc --proto_path=${solutiondir}/protobuf \
--csharp_out="$2":./Protobuf/Generated \
--csharp_opt=file_extension=.g.cs \
--contract_opt="$2",nocontract \
--contract_out=./Protobuf/Generated \
--plugin=protoc-gen-contract=${plugin} \
$1
