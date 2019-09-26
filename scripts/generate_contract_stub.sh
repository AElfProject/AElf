#!/usr/bin/env bash
scriptdir=`dirname "$0"`

bash "${scriptdir}/download_binary.sh"

plugin="${scriptdir}/contract_csharp_plugin"

destdir=./Protobuf/Generated

[ -d ${destdir} ] || mkdir -p ${destdir}

solutiondir=`dirname ${scriptdir}`

protoc --proto_path=${solutiondir}/protobuf \
--csharp_out=internal_access:${destdir} \
--csharp_opt=file_extension=.g.cs \
--contract_opt=stub \
--contract_opt=internal_access \
--contract_out=${destdir} \
--plugin=protoc-gen-contract="${plugin}" \
$@
