# How to dev a DAPP

## Running a node

See the detail in the section Getting started/Running a node

```sh
git clone https://github.com/AElfProject/AElf.git aelf

cd src

dotnet build AElf.CLI/AElf.CLI.csproj --configuration Release
alias aelf-cli="dotnet AElf.CLI/bin/Release/netcoreapp2.2/AElf.CLI.dll"

export AELF_CLI_DATADIR=~/.local/share/aelf

aelf-cli create

cd AElf.Launcher/
// Change the appsettings.json

dotnet bin/Release/netcoreapp2.2/AElf.Launcher.dll > aelf-logs.logs &
// And you can tail -f aelf-logs.logs

// Check the status of the node
aelf-cli get-blk-height -e http://127.0.0.1:1728
```

## Use CLI

```sh

export AELF_CLI_ENDPOINT=http://localhost:1728
export AELF_CLI_ACCOUNT=6hAAM14FJA5Nn8fVjy32Sf3dkHLeWgeD5vG2HRnpRp5cZ5C

aelf-cli call 4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6
// You will get 'Not found'

```

## Hello world

```sh
dotnet add test-aelf.csproj package AElf.Sdk.CSharp
# If you meet problem, please use add the version of the package.
# dotnet add test-aelf.csproj package AElf.Sdk.CSharp --version 0.7.0-alpha.1
```
