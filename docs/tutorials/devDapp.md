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
# If you meet problem, please add the version of the package. The Daily Build version in myget.
# dotnet add package AElf.Sdk.CSharp --version 0.7.0-alpha.1-18003 --source https://www.myget.org/F/aelf-project-dev/api/v3/index.json Writing /var/folders/9f/fclc6h3s51v17s0gdxzpcgt00000gn/T/tmpHSmuWg.tmp

# Follow the steps in the hello world demo of smart contract.

dotnet build test-aelf.csproj

6558d71135bca0ec2c1e7751b4c3bf65a102f872ddcb5e8537fb4b0cf89041fc
```

```js
dotnet publish AElf.sln /p:NoBuild=false --configuration Debug -o /Users/huangzongzhe/workspace/hoopox/AElf-dev/AElfRelease
```