SET scriptdir=%~dp0

call "%scriptdir%download_binary.bat"

protoc --proto_path=../../protobuf ^
--csharp_out=./Protobuf\Generated ^
--csharp_opt=file_extension=.g.cs ^
--contract_out=./Protobuf/Generated ^
--plugin=protoc-gen-contract="%scriptdir%contract_csharp_plugin.exe" ^
%*
