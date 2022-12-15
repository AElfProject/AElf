# Linux and Codespaces

Follow this doc to run aelf multi-nodes in Linux and Codespaces and this will take around 20 minutes to complete.



## Run Multi-Nodes

This tutorial will guide you through how to run three nodes.



### Publish aelf's Code

Create a directory. This tutorial uses a directory on the desktop for reference.

1. Execute this command to create a directory:

```Bash
mkdir ~/Desktop/Code
```

2. Execute this command to change the directory:

```Bash
cd ~/Desktop/Code/AElf
```

3. Execute this command to publish aelf's code (this will take several minutes):

```Bash
sudo dotnet publish AElf.All.sln /p:NoBuild=false --configuration Debug -o ~/Desktop/Out
```



### Configure Three Nodes

1. Execute this command three times to create three accounts: A, B, and C.

```Shell
aelf-command create
```

Please make sure you remember their Public Keys and Addresses.

Create a directory for node configuration. This tutorial uses a directory on the desktop for reference.

2. Execute this command to create a directory:

```Bash
mkdir ~/Desktop/Config
```

3. Execute this command to change the directory:

```Bash
cd ~/Desktop/Config
```

4. Execute this command to create three new directories: `bp1`, `bp2`, and `bp3`  in the "Config" directory and create their respective "keys" directories.

```Bash
mkdir -p ~/Desktop/Config/bp1/keys

mkdir -p ~/Desktop/Config/bp2/keys

mkdir -p ~/Desktop/Config/bp3/keys
```

5. Copy account A, B, and C from `/root/.local/share/aelf/keys/` to `bp1/keys`, `bp2/keys`, and `bp3/keys` respectively (If you can't find `.local`, you can use cmd + shift + g in Finder to designate the directories).

6. Execute this command to create `appsettings.json` files and `appsettings.MainChain.MainNet.json` files in directories `bp1`, `bp2`, and `bp3`:

```Bash
cd ~/Desktop/Config/bp1;touch appsettings.json;touch appsettings.MainChain.MainNet.json

cd ~/Desktop/Config/bp2;touch appsettings.json;touch appsettings.MainChain.MainNet.json

cd ~/Desktop/Config/bp3;touch appsettings.json;touch appsettings.MainChain.MainNet.json
```

​	Copy the following templates to each file:

​	For `appsettings.json`:

```JSON
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "AllowedHosts": "*",
  "CorsOrigins": "*",
  "ConnectionStrings": {
    "BlockchainDb": "redis://localhost:6379?db=1",
    "StateDb": "redis://localhost:6379?db=1"
  },
  "ChainId": "AELF",
  "IsMainChain" : true,
  "NetType": "MainNet",
  "Account": {
    "NodeAccount": "21qciGwcaowwBttKMjMk86AW6WajhcodSHytY1vCyZb7p*****",
    "NodeAccountPassword": "********"
  },
  "Network": {
    "BootNodes": [],
    "ListeningPort": 7001,
    "NetAllowed": "",
    "NetWhitelist": []
  },
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "http://*:8001/"
      }
    }
  },
  "Runner": {
    "BlackList": [],
    "WhiteList": []
  },
  "DeployServiceUrl": "",
  "Consensus": {
    "InitialMinerList" : [
      "04884d9563b3b67a589e2b9b47794fcfb3e15fa494053088dd0dc8a909dd72bfd24c43b0e2303d631683acaed34acf87526dd489e3805211cba710d956718*****",
      "045670526219d73154847b1e9367be9af293601793c9f7e34a96336650c9c1104a4aac9aaee960af00e775dcd88048698629891b0617ab605e646ae78961c*****",
      "046a5913eae5fee3da9ee33604119f025a0ad45575dfed1257eff5da2c24e629845b1e1a131c5da8751971d545cc5c03826b3eb2b7109b5141679a1927338*****"
    ],
    "MiningInterval" : 4000,
    "StartTimestamp": 0,
    "PeriodSeconds": 120
  },
  "BackgroundJobWorker":{
    "JobPollPeriod": 1
  }
}
```

For `appsettings.MainChain.MainNet.json`:

```JSON
{
    "ChainId": "AELF",
    "TokenInitial": {
        "Symbol": "ELF",
        "Name": "elf token",
        "TotalSupply": 1000000000,
        "Decimals": 2,
        "IsBurnable": true,
        "DividendPoolRatio": 0.2
    },
    "ElectionInitial": {
        "LockForElection": 100000,
        "TimeEachTerm": 2,
        "BaseTimeUnit": 2,
        "MinimumLockTime": 1,
        "MaximumLockTime": 2000
    }
}
```

7. Modify the `appsettings.json` files in directory `bp1`, `bp2`, and `bp3` as instructed:

   1. Change the numbers following `db=` in `BlockchainDb` and `StateDb` under `ConnectionStrings`:

      1. `bp1`: redis://localhost:6379?db=1

      2. `bp2`: redis://localhost:6379?db=2

      3. `bp3`: redis://localhost:6379?db=3

   2. Replace `NodeAccount` and `NodeAccountPassword` under `Account` with `Address` and `password` in account A, B, and C.
   3. Fill in all three `InitialMineList` under `Consensus` using account A, B, and C's `Public Key`, keys separated with`,`:

1. ```JSON
   "Consensus": {
       "InitialMinerList" : [
         "04884d9563b3b67a589e2b9b47794fcfb3e15fa494053088dd0dc8a909dd72bfd24c43b0e2303d631683acaed34acf87526dd489e3805211cba710d956718*****",
         "045670526219d73154847b1e9367be9af293601793c9f7e34a96336650c9c1104a4aac9aaee960af00e775dcd88048698629891b0617ab605e646ae78961c*****",
         "046a5913eae5fee3da9ee33604119f025a0ad45575dfed1257eff5da2c24e629845b1e1a131c5da8751971d545cc5c03826b3eb2b7109b5141679a1927338*****"
       ],
   ```

		4. In `bp1`, `BootNodes` is blank and `ListeningPort` is 7001. In `bp2`, `BootNodes` is `127.0.0.1:7001` (make sure to bracket it), and `ListeningPort` is 7002. In `bp3`, `BootNodes` are `127.0.0.1:7001` and `127.0.0.1:7002` (make sure to bracket them and separate them with `,`) and `ListeningPort` is 7003.
		4. Change the port numbers in `Kestrel-EndPoints-Http-Url` to 8001, 8002, and 8003 respectively (to ensure there is no conflict of ports).

8. Execute this command to start a Redis instance:

1. ```Bash
   redis-server
   ```


### Run Three Nodes

In this tutorial, code is published in `~/Desktop/Out` and the three nodes are configured in `~/Desktop/Config`.

Use `redis-server` to start a Redis instance.

We recommend you open three new Terminals to monitor the nodes’ operation.

Execute this command to launch node 1:

```Bash
cd ~/Desktop/Config/bp1;dotnet ~/Desktop/Out/AElf.Launcher.dll
```

Execute this command to launch node 2:

```Bash
cd ~/Desktop/Config/bp2;dotnet ~/Desktop/Out/AElf.Launcher.dll
```

Execute this command to launch node 3:

```Bash
cd ~/Desktop/Config/bp3;dotnet ~/Desktop/Out/AElf.Launcher.dll
```

The three nodes run successfully if all Terminals show the following output: 

```Bash
2022-11-30 20:51:04,163 [.NET ThreadPool Worker] INFO  AElf.Kernel.Miner.Application.MiningService - Generated block: { id: "12f519e1601dd9f755a186b1370fd12696a8c080ea04465dadc*********2463", height: 25 }, previous: 5308de83c3585dbb4a097a9187a3b2f9b8584db4889d428484ca3e4df09e2860, executed transactions: 2, not executed transactions 0
```

To shut the nodes down, please use control + c on your keyboard.

If you don't want to save the data, you can execute this command to delete all:

```Shell
redis-cli flushall
```