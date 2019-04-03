#!/usr/bin/env bash
scriptdir=`dirname "$0"`

bash "${scriptdir}/download_binary.sh"

plugin="${scriptdir}/contract_csharp_plugin"

protoc --proto_path=../protobuf \
--csharp_out=internal_access:./Protobuf/Generated \
--csharp_opt=file_extension=.g.cs \
--contract_opt=reference \
--contract_out=internal_access:./Protobuf/Generated \
--plugin=protoc-gen-contract="${plugin}" \
$@
