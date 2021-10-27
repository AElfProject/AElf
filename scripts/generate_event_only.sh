#!/usr/bin/env bash
scriptdir=`dirname "$0"`

bash "${scriptdir}/download_binary.sh"

plugin="${scriptdir}/contract_csharp_plugin"

solutiondir=`dirname ${scriptdir}`

protoc --proto_path=${solutiondir}/protobuf \
--csharp_out=internal_access:./Protobuf/Generated \
--csharp_opt=file_extension=.g.cs \
--contract_opt=nocontract \
--contract_out=internal_access:./Protobuf/Generated \
--plugin=protoc-gen-contract=${plugin} \
$@
