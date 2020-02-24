## Proposing a side-chain

When a user (usually a developer) feels the need to create a new side-chain on AElf he must call the cross-chain contract and request a side-chain creation. After requested, BPs will either approve this creation or reject it. If the request is approved, the  must then release the proposal and this will. 

A side-chain node is usually very similar to a main-chain node because both are based on AElf software and have common modules. The main difference is the configuration which varies depending on if the node is a side chain or not.

### Creation request

```protobuf

rpc RequestSideChainCreation(SideChainCreationRequest) returns (google.protobuf.Empty) { }

message SideChainCreationRequest {
    int64 indexing_price = 1;
    int64 locked_token_amount = 2;
    bool is_privilege_preserved = 3;
    string side_chain_token_symbol = 4;
    string side_chain_token_name = 5;
    sint64 side_chain_token_total_supply = 6;
    sint32 side_chain_token_decimals = 7;
    bool is_side_chain_token_burnable = 8;
    repeated SideChainTokenInitialIssue side_chain_token_initial_issue_list = 9;
    map<string, sint32> initial_resource_amount = 10; 
    bool is_side_chain_token_profitable = 11;
}

message ProposalCreated{
    option (aelf.is_event) = true;
    aelf.Hash proposal_id = 1;
}
```

The creation request will create a proposal with the Parliament contract. After calling this method, a **ProposalCreated** log will be created in which the **ProposalId** be found. This ID will enable the producers to approve it.

### Approving the proposal (producers)

```protobuf
    rpc Approve (aelf.Hash) returns (google.protobuf.Empty) { }
```

Producers that want to approve the creation have to call the Approve method, with the ID that was logged at creation.

### Releasing 

```protobuf
rpc ReleaseSideChainCreation(ReleaseSideChainCreationInput) returns (google.protobuf.Empty) { }

message ReleaseSideChainCreationInput {
    aelf.Hash proposal_id = 1;
}
```

Once BP's have approved, the request will become releasable by the requestor.

### Procedure

This guides you through configuring and running a side-chain node and a main-chain node. Here's a description of the steps to follow:
- create and edit configurations.
- launch the main-chain node and create, approve and release a side chain creation request (a script is provided later in this tutorial), this will give you a chain ID. 
- launch the side-chain node. 
- verify that the indexing height is increasing.

Note that this tutorial assumes that you know how to clone AElf's source repository, create accounts and build and run multiple nodes. If you not it's recommended to follow previous tutorials. You will also need **nodejs** to run the script and we recommend you install **aelf-command** for interacting with the nodes.

You will need two folders where you will place both configurations files. You can copy some template configurations from the [**AElf.Launcher project**](https://github.com/AElfProject/AElf/tree/dev/src/AElf.Launcher) in AElf's source code. After the setup you will end up will the following folders:
- Main chain node
  - appsettings.json
  - appsettings.MainChain.MainNet.json
- Side chain node
  - appsettings.json
  - appsettings.SideChain.MainNet.json
- AElf clone
- AElf build (optional, can be in the clone folder)
  - AElf.Launcher.dll (aelf's executable program)
- ProposalScript
  - sideChainProposal.js

### Main chain configuration:

Two configuration files must be placed in the configuration folder of the main-chain, this is also the folder from which you will launch the node:
- appsettings.json
- appsettings.MainChain.MainNet.json

We will set up the main chain node with **AELF** as it's chain id, connecting to Redis' **db1**. The web API port is **1234**. To make the tutorial easier to follow the node's account will be the same as the miner (used below in the miner list). So don't forget to change the **account**, **password** and **initial miner**.

In **appsettings.json** change the following configuration sections:
```json
"ChainId":"AELF",
"ChainType":"MainChain",
"NetType": "MainNet",
"ConnectionStrings": {
        "BlockchainDb": "redis://localhost:6379?db=1",
        "StateDb": "redis://localhost:6379?db=1"
},
"Account": {
    "NodeAccount": "YOUR ACCOUNT",
    "NodeAccountPassword": "YOUR PASSWORD"
},
"Kestrel": {
    "EndPoints": {
        "Http": {
            "Url": "http://*:1234/"
        }
    }
},
"Consensus": {
    "InitialMinerList": ["THE PUB KEY OF THE ACCOUNT CONFIGURED EARLIER"],
    "MiningInterval": 4000,
    "StartTimestamp": 0
},
```

In **appsettings.MainChain.MainNet.json** change the following configuration sections:

```json
{
  "CrossChain": {
      "Grpc": {
          "ListeningPort": 5010
      },
      "MaximalCountForIndexingParentChainBlock" : 32
  }
}
```

### Side chain configuration:

Two configuration files must be placed in the configuration folder of the side-chain, this is also the folder from which you will launch the node:
- appsettings.json
- appsettings.SideChain.MainNet.json

We will set up the side-chain node with **tDVV** (1866392 converted to base58) as it's chain id, connecting to Redis' **db2**. The web API port is **1235**. To make the tutorial easier to follow the node's account will be the same as the miner (used below in the miner list). So don't forget to change the **account**, **password** and **initial miner**. You can use the same account for both nodes in this tutorial.

In **appsettings.json** change the following configuration sections:
```json
"ChainId":"tDVV",
"ChainType":"SideChain",
"NetType": "MainNet",
"ConnectionStrings": {
        "BlockchainDb": "redis://localhost:6379?db=2",
        "StateDb": "redis://localhost:6379?db=2"
},
"Account": {
    "NodeAccount": "YOUR ACCOUNT",
    "NodeAccountPassword": "YOUR PASSWORD"
},
"Kestrel": {
    "EndPoints": {
        "Http": {
            "Url": "http://*:1235/"
        }
    }
},
"Consensus": {
    "InitialMinerList": ["THE PUB KEY OF THE ACCOUNT CONFIGURED EARLIER"],
    "MiningInterval": 4000,
    "StartTimestamp": 0
},

In **appsettings.SideChain.MainNet.json** change the following configuration sections:
```json
{
  "CrossChain": {
    "Grpc": {
      "ParentChainServerPort": 5010,
      "ListeningPort": 5000,
      "ParentChainServerIp": "127.0.0.1"
    },
    "ParentChainId": "AELF",
    "MaximalCountForIndexingParentChainBlock" : 32
  }
}
```

### Launch the main-chain node:

Open a terminal and navigate to the folder where you created the configuration for the main-chain. The next step is to launch the main-chain node:

```bash
dotnet ../AElf.Launcher.dll
```

You can try out a few commands from another terminal to check if everything is fine, for example:

```bash
aelf-command get-blk-height -e http://127.0.0.1:1234
```

### Side chain configuration:

Here you can find the full script for creating the proposal, approving and releasing it. Note that this script is an example and is in **no** way production ready. Don't forget to replace:
- the private key.
- the node's API endpoint.

The last log from this script will print the chain ID of he newly created side-chain.

Create the script file, copy the following content and run it:
**sideChainProposal.js**:
```javascript
const AElf = require('aelf-sdk');
const Wallet = AElf.wallet;

const { sha256 } = AElf.utils;

// set the private key of the block producer
const defaultPrivateKey = 'e119487fea0658badc42f089fbaa56de23d8c0e8d999c5f76ac12ad8ae897d76';
const wallet = Wallet.getWalletByPrivateKey(defaultPrivateKey);

// link to the node
const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:1234'));

if (!aelf.isConnected()) {
  console.log('Could not connect to the node.');
}

const tokenContractName = 'AElf.ContractNames.Token';
const parliamentContractName = 'AElf.ContractNames.Parliament';
const crossChainContractName = 'AElf.ContractNames.CrossChain';

var pollMining = async function(transactionId) {
    console.log('== Waiting for the transaction to be mined ==');
    console.log('transaction id: ' + transactionId);

    for (i = 0; i < 5; i++) {
        const currentResult = await aelf.chain.getTxResult(transactionId);
        console.log('transaction status: ' + currentResult.Status);

        if (currentResult.Status === 'MINED')
            return currentResult;

        await new Promise(resolve => setTimeout(resolve, 2000));
    }
  }

const createSideChain = async () => {

    // get the status of the chain in order to get the genesis contract address
    console.log('Starting side chain creation script');

    const chainStatus = await aelf.chain.getChainStatus({sync: true});
    const genesisContract = await aelf.chain.contractAt(chainStatus.GenesisContractAddress, wallet)
      .catch((err) => {
        console.log(err);
      });

    // get the addresses of the contracts that we'll need to call
    const tokenContractAddress = await genesisContract.GetContractAddressByName.call(sha256(tokenContractName));
    const parliamentContractAddress = await genesisContract.GetContractAddressByName.call(sha256(parliamentContractName));
    const crossChainContractAddress = await genesisContract.GetContractAddressByName.call(sha256(crossChainContractName));

    console.log('token contract address: ' + tokenContractAddress);
    console.log('parliament contract address: ' + parliamentContractAddress);
    console.log('cross chain contract address: ' + crossChainContractAddress);

    // build the aelf-sdk contract object
    const parliamentContract = await aelf.chain.contractAt(parliamentContractAddress, wallet);
    const tokenContract = await aelf.chain.contractAt(tokenContractAddress, wallet);
    const crossChainContract = await aelf.chain.contractAt(crossChainContractAddress, wallet);

    const approveResult = await tokenContract.Approve({
      symbol:'ELF',
      spender: crossChainContractAddress,
      amount: 20000
    });

    const genesisOwnerAddress = await parliamentContract.GetGenesisOwnerAddress.call();
    console.log('genesisOwnerAddress: ' + genesisOwnerAddress);
      
    let approveTokenTransactionResult = await pollMining(approveResult.TransactionId);
    console.log(approveTokenTransactionResult);

    const proposalRequest = crossChainContract.CreateSideChain.packInput({
      indexingPrice: 1,
      lockedTokenAmount: 20000,
      isPrivilegePreserved: false,
      sideChainTokenDecimals: 2,
      sideChainTokenName: 'SCATokenName',
      sideChainTokenSymbol: 'SCA',
      sideChainTokenTotalSupply: 100000,
      isSideChainTokenBurnable: true
    });

    let expiredTime = 3600;
    let time = new Date(); 
    time.setSeconds(new Date().getSeconds() + expiredTime);
    let expired_time = { seconds: Math.floor(time/1000), nanos: (time % 1000) * 1000 };

    var result = await parliamentContract.CreateProposal({
        contractMethodName: 'CreateSideChain',
        organizationAddress: genesisOwnerAddress,
        toAddress: crossChainContractAddress,
        params: proposalRequest,
        expiredTime: expired_time
    });

    console.log(result);

    let createProposalResult = await pollMining(result.TransactionId);

    const proposalId = createProposalResult.ReadableReturnValue;
    console.log('Proposal hash ' + proposalId);

    // approve
    var approvalResult = await parliamentContract.Approve({
        proposalId: JSON.parse(proposalId)
    }).catch((err) => {
        console.log(err);
      });

    console.log(approvalResult)
    let approveTransactionResult = await pollMining(approvalResult.TransactionId);
    console.log(approveTransactionResult);

    // release
    var releaseResult = await parliamentContract.Release(JSON.parse(proposalId));
    console.log('Release txid :' + releaseResult);

    let releaseTransactionResult = await pollMining(releaseResult.TransactionId);
    console.log(releaseTransactionResult);

    // deserialize the creation log (bytes as base64 encoded strings)
    const creationLogEvent = releaseTransactionResult.Logs[1].NonIndexed;
    const creationType = crossChainContract.services[0].lookupType('CreationRequested');
    let deser = creationType.decode(Buffer.from(creationLogEvent, 'base64'));
    const resobj = creationType.toObject(deser, { bytes: String });

    console.log(resobj);
};

createSideChain();
```

### Launch the side-chain node:

Open a terminal and navigate to the folder where you created the configuration for the side-chain.

```bash
dotnet ../AElf.Launcher.dll
```

You can try out a few commands from another terminal to check if everything is fine, for example:

```bash
aelf-command get-blk-height -e http://127.0.0.1:1235
```



