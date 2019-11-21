## Setup cross chain

A side-chain node is usually very similar to a main-chain node because both are based on AElf software and have common modules. The main difference is the configuration which varies depending on if the node is a side chain or not.

### Procedure

This guides you through configuring and running a sidechain node and a mainchain node. Here's a description of the steps to follow:
- create and edit the mainchain's configuration.
- launch the mainchain node and create, approve and release an side chain creation request (a script is provided in this tutorial), this will give you a chain ID. 
- with the chain ID the was issued after the proposal release, modify the side-chains configuration.
- launch the sidechain node. 
- verify that the indexing height is increasing.

Note that this tutorial assumes that you know how to clone AElf's source repository, create accounts and build and run multiple nodes. If you not it's recommended to follow previous tutorials.

You will need two folders where you will place both configurations files.

### Main chain:

Keep ```ParentChainId``` empty as main chain doesn't have parent chain.

```json
"ChainId":"AELF",
"ChainType":"MainChain",
"CrossChain": {
    "Grpc": {
      "LocalServerPort": 5000,
      "LocalServerHost": "127.0.0.1"
    },
    "ParentChainId":"",
}
```

### Side chain:



```json
"ChainId":"tDVV", // 1866392
"ChainType":"SideChain",
"CrossChain":{
    "Grpc": {
      "RemoteParentChainServerPort": 5000,
      "LocalServerHost": "127.0.0.1",
      "LocalServerPort": 5010,
      "RemoteParentChainServerHost": "127.0.0.1"
    },
    "ParentChainId":"AELF",
}
```


Here you can find the full script for creating the proposal, approving and releasing it. Note that this script is an example and is in **no** way production ready. Don't forget to replace:
- the private key.
- the node's API endpoint.

The last log from this script will print the chain ID of he newly created side-chain.

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





