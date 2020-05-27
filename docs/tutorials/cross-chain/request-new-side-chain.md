# Requesting the creation of a side-chain

When a user (usually a developer) feels the need to create a new side-chain on AElf he must call the cross-chain contract and request a side-chain creation. After requested, BPs will either approve this creation or reject it. If the request is approved, the developer must then release the proposal.

Throughout this tutorial we'll give step-by-step code snippets that use the [aelf-js-sdk](https://github.com/AElfProject/aelf-sdk.js/tree/master) to create a new side-chain, the full script will be given at the end of the tutorial. 

This creation of a side-chain (logical, on-chain creation) is done in four steps:
- the developer must *allow/approve* some tokens to the cross-chain contract of the main chain.
- the developer calls the cross-chain contract of the main chain, to *request* the creation.
- the BPs must *approve* this request.
- finally the developer must *release* the request to finalize the creation.

Keep in mind that this is just the logical on-chain creation of the side-chain. After the side-chain is released there's extra steps needed for it to be a fully functional blockchain, including the producers running the side-chain's nodes.

Note: for more information about the meaning of the different fields, refer to the document in the [cross-chain section](../../crosschain/setup.md).

## Set-up 

If you want to test the creation process you will need a producer node running and the following:
- you need a key-pair (account) created, this will be your Producer (in this tutorial we also use the producer to create the creation request).
- the node needs to be configured with an API endpoint, account and miner list that correspond to what is in the script.

The following snippet shows constants and initialization code used in the script:

```javascript
const AElf = require('aelf-sdk');
const Wallet = AElf.wallet;

const { sha256 } = AElf.utils;

// set the private key of the block producer.
// REPLACE
const defaultPrivateKey = 'e119487fea0658badc42f089fbaa56de23d8c0e8d999c5f76ac12ad8ae897d76';
const defaultPrivateKeyAddress = 'HEtBQStfqu53cHVC3PxJU6iGP3RGxiNUfQGvAPTjfrF3ZWH3U';

// load the wallet associated with your block producers account.
const wallet = Wallet.getWalletByPrivateKey(defaultPrivateKey);

// API link to the node
// REPLACE
const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:1234'));

// names of the contracts that will be used.
const tokenContractName = 'AElf.ContractNames.Token';
const parliamentContractName = 'AElf.ContractNames.Parliament';
const crossChainContractName = 'AElf.ContractNames.CrossChain';

...

const createSideChain = async () => {

    console.log('Starting side chain creation script\n');

    // check the chain status to make sure the node is running
    const chainStatus = await aelf.chain.getChainStatus({sync: true});
    const genesisContract = await aelf.chain.contractAt(chainStatus.GenesisContractAddress, wallet)
        .catch((err) => {
        console.log(err);
        });

    // get the addresses of the contracts that we'll need to call
    const tokenContractAddress = await genesisContract.GetContractAddressByName.call(sha256(tokenContractName));
    const parliamentContractAddress = await genesisContract.GetContractAddressByName.call(sha256(parliamentContractName));
    const crossChainContractAddress = await genesisContract.GetContractAddressByName.call(sha256(crossChainContractName));

    // build the aelf-sdk contract instance objects
    const parliamentContract = await aelf.chain.contractAt(parliamentContractAddress, wallet);
    const tokenContract = await aelf.chain.contractAt(tokenContractAddress, wallet);
    const crossChainContract = await aelf.chain.contractAt(crossChainContractAddress, wallet);

    ...
}

```

When running the script, the **createSideChain** will be executed and automatically will run through the full process of creating the side-chain.

## Creation of the side chain

### Set the Allowance.

First the developer must approve some ELF tokens for use by the cross-chain contract.

```javascript
var setAllowance = async function(tokenContract, crossChainContractAddress)
{
    console.log('\n>>>> Setting allowance for the cross-chain contract.');

    // set some allowance to the cross-chain contract
    const approvalResult = await tokenContract.Approve({
        symbol:'ELF',
        spender: crossChainContractAddress,
        amount: 20000
        });

    let approveTransactionResult = await pollMining(approvalResult.TransactionId);
}
```

### Creation request

In order to request a side chain creation the developer must call **RequestSideChainCreation** on the cross-chain contract, this will create a proposal with the **Parliament** contract. After calling this method, a **ProposalCreated** log will be created in which the **ProposalId** be found. This ID will enable the producers to approve it.

```protobuf
rpc RequestSideChainCreation(SideChainCreationRequest) returns (google.protobuf.Empty){}

message SideChainCreationRequest {
    int64 indexing_price = 1;
    int64 locked_token_amount = 2;
    bool is_privilege_preserved = 3;
    string side_chain_token_symbol = 4;
    string side_chain_token_name = 5;
    int64 side_chain_token_total_supply = 6;
    int32 side_chain_token_decimals = 7;
    bool is_side_chain_token_burnable = 8;
    bool is_side_chain_token_profitable = 9;
    repeated SideChainTokenInitialIssue side_chain_token_initial_issue_list = 10;
    map<string, int32> initial_resource_amount = 11;
}

message SideChainTokenInitialIssue{
    aelf.Address address = 1;
    int64 amount = 2;
}

message ProposalCreated{
    option (aelf.is_event) = true;
    aelf.Hash proposal_id = 1;
}
```

In order for the creation request to succeed, some assertions must pass:
- the Sender can only have one pending request at any time.
- the locked token amount must be greater than 0 and higher than the indexing price.
- the token initial issue list must contain at least one token and all with an **amount** greater than 0.
- the initial resource amount list must contain all resource tokens of the chain and the value must be greater than 0.
- the cross chain contract must have a larger allowance from the proposer (Sender of the transaction) than the locked token amount: (allowance(Sender to Cross chain contract > locked token amount)).
- no need to provide data about side chain token if **is_privilege_preserved** is false, and side chain token won't be created even you provide token info.


```javascript
    console.log('\n>>>> Requesting the side-chain creation.');
    const sideChainCreationRequestTx = await crossChainContract.RequestSideChainCreation({
        indexingPrice: 1,
        lockedTokenAmount: '20000',
        isPrivilegePreserved: true,
        sideChainTokenDecimals: 8,
        sideChainTokenName: 'SCATokenName',
        sideChainTokenSymbol: 'SCA',
        sideChainTokenTotalSupply: '100000000000000000',
        isSideChainTokenBurnable: true,
        sideChainTokenInitialIssueList: [
            {
                address: '28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK',
                amount: '1000000000000000'
            }
        ],
        initialResourceAmount: { CPU: 2, RAM: 4, DISK: 512, NET: 1024 },
        isSideChainTokenProfitable: true
    });

    let sideChainCreationRequestTxResult = await pollMining(sideChainCreationRequestTx.TransactionId);

    // deserialize the log to get the proposal's ID.
    let deserializedLogs = parliamentContract.deserializeLog(sideChainCreationRequestTxResult.Logs, 'ProposalCreated');
    console.log(`>> side-chain creation request proposal id ${JSON.stringify(deserializedLogs[0].proposalId)}`);
```

The last line will print the proposal ID and this is what will be used for approving by the producers.

### Approval from producers

This is where the BPs approve the proposal:

```javascript
    console.log(`\n>>>> Approving the proposal.`);

    var proposalApproveTx = await parliamentContract.Approve(deserializedLogs[0].proposalId);
    await pollMining(proposalApproveTx.TransactionId);
```

Note: when calling **Approve** it will be the *Sender* of the transaction that approves. Here the script is set to use the key of a BP, see full script at the end.

### Release

This part of the script releases the proposal:

```javascript
    console.log(`\n>>>> Release the side chain.`);

    var releaseResult = await crossChainContract.ReleaseSideChainCreation({
        proposalId: deserializedLogs[0].proposalId
    });

    let releaseTxResult = await pollMining(releaseResult.TransactionId);

    // Parse the logs to get the chain id.
    let sideChainCreationEvent = crossChainContract.deserializeLog(releaseTxResult.Logs, 'SideChainCreatedEvent');
    console.log('Chain chain created : ');
    console.log(sideChainCreationEvent);
```

This is the last step involved in creating a side-chain, after this the chain id of the new side-chain is accessible in the **SideChainCreatedEvent** event log.

## Full script

This section presents the full script. Remember that in order to run successfully, a node must be running, configured with one producer. The configured producer must match the **defaultPrivateKey** and **defaultPrivateKeyAddress** of the script.

Also, notice that this script by default tries to connect to the node's API at the following address http://127.0.0.1:1234, if your node is listening on a different address you have to modify the address.

If you haven't already installed it, you need the aelf-sdk:
```bash
npm install aelf-sdk
```

You can simply run the script from anywhere:
```bash
node sideChainProposal.js
```

**sideChainProposal.js**:
```javascript
const AElf = require('aelf-sdk');
const Wallet = AElf.wallet;

const { sha256 } = AElf.utils;

// set the private key of the block producer
const defaultPrivateKey = 'e119487fea0658badc42f089fbaa56de23d8c0e8d999c5f76ac12ad8ae897d76';
const defaultPrivateKeyAddress = 'HEtBQStfqu53cHVC3PxJU6iGP3RGxiNUfQGvAPTjfrF3ZWH3U';
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
  console.log(`>> Waiting for ${transactionId} the transaction to be mined.`);

  for (i = 0; i < 10; i++) {
      const currentResult = await aelf.chain.getTxResult(transactionId);
      // console.log('transaction status: ' + currentResult.Status);

      if (currentResult.Status === 'MINED')
          return currentResult;

      await new Promise(resolve => setTimeout(resolve, 2000))
      .catch(function () {
          console.log("Promise Rejected");
     });;
  }
}

var setAllowance = async function(tokenContract, crossChainContractAddress)
{
    console.log('\n>>>> Setting allowance for the cross-chain contract.');

    // set some allowance to the cross-chain contract
    const approvalResult = await tokenContract.Approve({
        symbol:'ELF',
        spender: crossChainContractAddress,
        amount: 20000
        });

    let approveTransactionResult = await pollMining(approvalResult.TransactionId);
    //console.log(approveTransactionResult);
}

var checkAllowance = async function(tokenContract, owner, spender)
{
    console.log('\n>>>> Checking the cross-chain contract\'s allowance');

    const checkAllowanceTx = await tokenContract.GetAllowance({
        symbol: 'ELF',
        owner: owner,
        spender: spender
    });

    let checkAllowanceTxResult = await pollMining(checkAllowanceTx.TransactionId);
    let txReturn = JSON.parse(checkAllowanceTxResult.ReadableReturnValue);

    console.log(`>> allowance to the cross-chain contract: ${txReturn.allowance} ${txReturn.symbol}`);
} 

const createSideChain = async () => {

    // get the status of the chain in order to get the genesis contract address
    console.log('Starting side chain creation script\n');

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

    console.log();

    // 1. set and check the allowance, spender is the cross-chain contract
    await setAllowance(tokenContract, crossChainContractAddress);
    await checkAllowance(tokenContract, defaultPrivateKeyAddress, crossChainContractAddress);

    // 2. request the creation of the side-chain with the cross=chain contract
    console.log('\n>>>> Requesting the side-chain creation.');
    const sideChainCreationRequestTx = await crossChainContract.RequestSideChainCreation({
        indexingPrice: 1,
        lockedTokenAmount: '20000',
        isPrivilegePreserved: true,
        sideChainTokenDecimals: 8,
        sideChainTokenName: 'SCATokenName',
        sideChainTokenSymbol: 'SCA',
        sideChainTokenTotalSupply: '100000000000000000',
        isSideChainTokenBurnable: true,
        sideChainTokenInitialIssueList: [
            {
                address: '28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK',
                amount: '1000000000000000'
            }
        ],
        initialResourceAmount: { CPU: 2, RAM: 4, DISK: 512, NET: 1024 },
        isSideChainTokenProfitable: true
    });

    let sideChainCreationRequestTxResult = await pollMining(sideChainCreationRequestTx.TransactionId);

    // deserialize the log to get the proposal's ID.
    let deserializedLogs = parliamentContract.deserializeLog(sideChainCreationRequestTxResult.Logs, 'ProposalCreated');
    console.log(`>> side-chain creation request proposal id ${JSON.stringify(deserializedLogs[0].proposalId)}`);

    // 3. Approve the proposal 
    console.log(`\n>>>> Approving the proposal.`);

    var proposalApproveTx = await parliamentContract.Approve(deserializedLogs[0].proposalId);
    await pollMining(proposalApproveTx.TransactionId);

    // 3. Release the side chain
    console.log(`\n>>>> Release the side chain.`);

    var releaseResult = await crossChainContract.ReleaseSideChainCreation({
        proposalId: deserializedLogs[0].proposalId
    });

    let releaseTxResult = await pollMining(releaseResult.TransactionId);

    // Parse the logs to get the chain id.
    let sideChainCreationEvent = crossChainContract.deserializeLog(releaseTxResult.Logs, 'SideChainCreatedEvent');
    console.log('Chain chain created : ');
    console.log(sideChainCreationEvent);
};

createSideChain();
```