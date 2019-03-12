#!/usr/bin/env bash

protoc --proto_path=./proto \
--csharp_out=./Generated \
--csharp_opt=file_extension=.g.cs \
token_contract_messages.proto