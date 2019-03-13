#!/usr/bin/env bash

protoc --proto_path=./proto \
--csharp_out=./Generated \
--csharp_opt=file_extension=.g.cs \
--contract_out=./Generated test_contract.proto \
--plugin=protoc-gen-contract=./grpc_contract_csharp_plugin
