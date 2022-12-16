# Linux and Codespaces

Follow this doc to run an aelf single node in Linux and Codespaces and this will take around 20 minutes to complete.

## Install aelf-command

Execute this command to install aelf-command:

```Bash
npm i aelf-command -g
```

The following output suggests successful installation:

```Bash
+ aelf-command@0.1.44
added 314 packages from 208 contributors in 25.958s
```

## Clone and Build aelf's Code

Create a directory. This tutorial uses a directory on the desktop for reference.

1. Execute this command to create a directory:

```Bash
mkdir ~/Desktop/Code
```

2. Execute this command to change the directory:

```Bash
cd ~/Desktop/Code
```

3. Execute this command to clone aelf's code:

```Bash
git clone https://github.com/AElfProject/AElf.git
```

4. Execute this command to change to aelf's directory:

```Bash
cd AElf
```

5. Execute this command to restore aelf's files:

```Bash
dotnet restore AElf.All.sln
```

6. Execute this command to build aelf's code (this will take several minutes):

```Bash
dotnet build AElf.All.sln
```

The following output suggests successful building:

```Bash
 xx Warning(s)
    0 Error(s)

Time Elapsed 00:15:59.77
```

## Create an aelf Account

Execute this command:

```Shell
aelf-command create
```

An aelf account will be automatically created and you will see info like:

```Bash
AElf [Info]: Your wallet info is :
AElf [Info]: Mnemonic            : mirror among battle muffin cattle plunge tuition buzz hip mad surround recall
AElf [Info]: Private Key         : 4bf625afea60e21aa5afcab5ea682b3dfb614941245698632d72a09ae13*****
AElf [Info]: Public Key          : 04f9bb56a9eca921bd494e677307f0279c98f1d2ed6bdeaa6dd256878272eabd14e91ec61469d2a32ce5e63205930dabdc0b9f13fc80c1f4e31760618d182*****
AElf [Info]: Address             : 21qciGwcaowwBttKMjMk86AW6WajhcodSHytY1vCyZb7p*****
```

You will then be asked whether you want the account data stored as a json file. Enter `y` to confirm and the file will be stored in `/root/.local/share/aelf/keys/`.

Please make sure you remember the account data or the json file's location.

You will be required to set a password (referred to as \* here):

```Bash
Enter a password: ********
Confirm password: ********
```

For the sake of convenience, you are encouraged to keep this Terminal on the account info interface and open another Terminal to continue the following.

## Run a Single Node

A single node runs aelf blockchain on one node. It is usually used to test the execution of contracts only.

1. Execute this command to start a Redis instance (skip this step if redis-server is already started):

```Bash
redis-server
```

2. Open another Terminal and execute this command to change to aelf's directory:

```Bash
cd ~/Desktop/Code/AElf
```

3. Execute this command to change to the `AElf.Launcher` directory:

```Bash
cd src/AElf.Launcher
```

4. Execute this command to modify the `appsettings.json` file (or to manually update it, go to desktop -> Code -> AElf -> src -> AElf.Launcher):

```Bash
vim appsettings.json
```

Find the account data you just created using `aelf-command create`.

```Bash
AElf [Info]: Your wallet info is :
AElf [Info]: Mnemonic            : mirror among battle muffin cattle plunge tuition buzz hip mad surround recall
AElf [Info]: Private Key         : 4bf625afea60e21aa5afcab5ea682b3dfb614941245698632d72a09ae13*****
AElf [Info]: Public Key          : 04f9bb56a9eca921bd494e677307f0279c98f1d2ed6bdeaa6dd256878272eabd14e91ec61469d2a32ce5e63205930dabdc0b9f13fc80c1f4e31760618d182*****
AElf [Info]: Address             : 21qciGwcaowwBttKMjMk86AW6WajhcodSHytY1vCyZb7p*****
```

Fill in the `NodeAccount` and `NodeAccountPassword` under `Account` using the `Address` and `password` you set in `appsettings.json`:

```Bash
 "Account": {
    "NodeAccount": "",
    "NodeAccountPassword": ""
  }
```

It may look like this when you complete it:

```Bash
 "Account": {
    "NodeAccount": "21qciGwcaowwBttKMjMk86AW6WajhcodSHytY1vCyZb7p*****",
    "NodeAccountPassword": "********"
  },
```

Fill in the `InitialMineList` under `Consensus` using Public Key:

```Bash
"Consensus": {
    "InitialMinerList": [],
    "MiningInterval": 4000,
    "StartTimestamp": 0,
    "PeriodSeconds": 604800,
    "MinerIncreaseInterval": 31536000
  }
```

It may look like this when you complete it (make sure the key is bracketed):

```Bash
"Consensus": {
    "InitialMinerList": ["04f9bb56a9eca921bd494e677307f0279c98f1d2ed6bdeaa6dd256878272eabd14e91ec61469d2a32ce5e63205930dabdc0b9f13fc80c1f4e31760618d182*****"],
    "MiningInterval": 4000,
    "StartTimestamp": 0,
    "PeriodSeconds": 604800,
    "MinerIncreaseInterval": 31536000
  }
```

If the IP and port for Redis have been changed, you can modify them under `ConnectionStrings` in `appsettings.json` (skip this step if they are not changed):

```Bash
"ConnectionStrings": {
    "BlockchainDb": "redis://localhost:6379?db=1",
    "StateDb": "redis://localhost:6379?db=1"
}
```

Save the changes and keep them in the `AElf.Launcher` directory.

5. Execute `dotnet run`:

```Bash
sudo dotnet run
```

The following output suggests successful execution:

```Bash
2022-11-29 16:07:44,554 [.NET ThreadPool Worker] INFO  AElf.Kernel.SmartContractExecution.Application.BlockExecutionResultProcessingService - Attach blocks to best chain, best chain hash: "f396756945d9bb883f81827ab36fcb0533d3c66f7062269700e49b74895*****", height: 177
```

If you want to check the node's block height and other block info, you can visit [this page](http://localhost:8000/swagger/index.html) where you can access the API docs and interact with this single node.

To shut the node down, please use control + c on your keyboard.

If you don't want to save the data, you can execute this command to delete all:

```Shell
redis-cli flushall
```

If you are interested in running multi-nodes, please click [here](multi-linux-and-codespaces.md) to learn more.
