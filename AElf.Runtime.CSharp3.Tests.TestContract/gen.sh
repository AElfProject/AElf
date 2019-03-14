#!/usr/bin/env bash

dir=`dirname "$0"`

chmod a+x ${dir}/grpc_contract_csharp_plugin

protoc --proto_path=./proto \
--csharp_out=./Generated \
--csharp_opt=file_extension=.g.cs \
--contract_out=./Generated test_contract.proto \
--plugin=protoc-gen-contract=${dir}/grpc_contract_csharp_plugin
