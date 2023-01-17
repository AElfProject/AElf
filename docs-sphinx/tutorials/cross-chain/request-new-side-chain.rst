Requesting the creation of a side chain
=======================================

Side chains can be created in the AELF ecosystem to enable scalability.
This part is going to introduce these periods in detail.

Side chain creation api
^^^^^^^^^^^^^^^^^^^^^^^

Anyone can request the side chain creation in the AELF ecosystem. The
proposer/creator of a new side chain will need to request the creation
of the side chain through the cross-chain contract on the main-chain.
The request contains different fields that will determine the type of
side chain that will be created.

This section show the API to use in order to propose the creation of a
side chain. The fields that are in the ``SideChainCreationRequest`` will
determine the type of side chain that is created. For more api details,
you can follow the ``RequestSideChainCreation`` in :doc:`Crosschain contract<../../../reference/smart-contract-api/cross-chain>`.

A new proposal about the side chain creation would be created and the
event ``ProposalCreated`` containing proposal id would be fired. A
parliament organization which is specified since the chain launched is
going to approve this proposal in 24 hours(refer to :doc:`Parliament contract <../../../reference/smart-contract-api/parliament>` 
for detail). Proposer is able to release the side chain creation request
with proposal id once the proposal can be released. Refer ``ReleaseSideChainCreation`` in :doc:`Crosschain contract<../../../reference/smart-contract-api/cross-chain>`.

New side chain would be created and the event ``SideChainCreatedEvent``
containing chain id would be fired.

Side chain node can be launched since it is already created on main
chain. Side chain id from the creation result should be configured
correctly before launching the side chain node. Please make sure cross
chain communication context is correctly set, because side chain node is
going to request main chain node for chain initialization data. For more
details, check :doc:`side chain node running <running-side-chain>` tutorial.

Side chain types
^^^^^^^^^^^^^^^^

Two types of side-chain’s currently exist: **exclusive** or **shared**.
An **exclusive** side-chain is a type of dedicated side-chain (as
opposed to shared) that allows developers to choose the transaction fee
model and set the transaction fee price. The creator has exclusive use
of this side-chain. For example, only creator of this **exclusive** 
side-chain can propose to deploy a new contract.

Pay for Side chain
^^^^^^^^^^^^^^^^^^

Indexing fee
------------

Indexing fee, literally, is paid for the side chain indexing. You can 
specify the indexing fee price and prepayments amount when you request
side chain creation. `Cross chain contract` is going to charge your 
prepayments once the side chain created and pay the miner who indexes 
the side chain block every time. 

Resource fee
------------

Developers of an exclusive side-chain pay the
producers for running it by paying CPU, RAM, DISK, NET resource tokens:
this model is called *charge-by-time*. The amount side chain creator
must share with the producers is set after creation of the chain. The
**exclusive** side-chain is priced according to the time used. The unit
price of the fee is determined through negotiation between the
production node and the developer.

See `Economic whitepaper - 4.3 Sidechain Developer Charging
Model <https://aelf.com/gridcn/aelf_Economic_and_Governance_Whitepaper_v1.2_en.pdf>`__
for more information.

Simple demo for side chain creation request
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

When a user (usually a developer) feels the need to create a new side
chain on AElf he must call the cross-chain contract and request a side
chain creation. After requested, parliament organization members will
either approve this creation or reject it. If the request is approved,
the developer must then release the proposal.

Throughout this tutorial we’ll give step-by-step code snippets that use
the
`aelf-js-sdk <https://github.com/AElfProject/aelf-sdk.js/tree/master>`__
to create a new side chain, the full script will be given at the end of
the tutorial.

This creation of a side chain (logical, on-chain creation) is done in
four steps: 

- the developer must *allow/approve* some tokens to the cross-chain contract of the main chain. 
- the developer calls the cross-chain contract of the main chain, to *request* the creation. 
- the parliament organization members must *approve* this request. 
- finally the developer must *release* the request to finalize the creation.

Keep in mind that this is just the logical on-chain creation of the side
chain. After the side chain is released there’s extra steps needed for
it to be a fully functional blockchain, including the producers running
the side chain’s nodes.


Set-up
------

If you want to test the creation process you will need a producer node
running and the following: 

- you need a key-pair (account) created, this will be your Producer (in this tutorial we also use the producer to create the creation request).
- the node needs to be configured with an API endpoint, account and miner list that correspond to what is in the script.

The following snippet shows constants and initialization code used in
the script:

.. code:: javascript

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

When running the script, the **createSideChain** will be executed and
automatically will run through the full process of creating the side
chain.

Creation of the side chain
--------------------------

Set the Allowance.
~~~~~~~~~~~~~~~~~~

First the developer must approve some ELF tokens for use by the
cross-chain contract.

.. code:: javascript

   var setAllowance = async function(tokenContract, crossChainContractAddress)
   {
       // set some allowance to the cross-chain contract
       const approvalResult = await tokenContract.Approve({
           symbol:'ELF',
           spender: crossChainContractAddress,
           amount: 20000
           });

       let approveTransactionResult = await pollMining(approvalResult.TransactionId);
   }

Creation request
~~~~~~~~~~~~~~~~

In order to request a side chain creation the developer must call
**RequestSideChainCreation** on the cross-chain contract, this will
create a proposal with the **Parliament** contract. After calling this
method, a **ProposalCreated** log will be created in which the
**ProposalId** be found. This ID will enable the producers to approve
it.

.. code:: protobuf

   rpc RequestSideChainCreation(SideChainCreationRequest) returns (google.protobuf.Empty){}

   message SideChainCreationRequest {
       // The cross chain indexing price.
       int64 indexing_price = 1;
       // Initial locked balance for a new side chain.
       int64 locked_token_amount = 2;
       // Creator privilege boolean flag: True if chain creator privilege preserved, otherwise false.
       bool is_privilege_preserved = 3;
       // Side chain token information.
       SideChainTokenCreationRequest side_chain_token_creation_request = 4;
       // A list of accounts and amounts that will be issued when the chain starts.
       repeated SideChainTokenInitialIssue side_chain_token_initial_issue_list = 5;
       // The initial rent resources.
       map<string, int32> initial_resource_amount = 6;
   }
   
   message SideChainTokenCreationRequest{
       // Token symbol of the side chain to be created
       string side_chain_token_symbol = 1;
       // Token name of the side chain to be created
       string side_chain_token_name = 2;
       // Token total supply of the side chain to be created
       int64 side_chain_token_total_supply = 3;
       // Token decimals of the side chain to be created
       int32 side_chain_token_decimals = 4;
   }
   
   message SideChainTokenInitialIssue{
       // The account that will be issued.
       aelf.Address address = 1;
       // The amount that will be issued.
       int64 amount = 2;
   }

In order for the creation request to succeed, some assertions must pass:

- the Sender can only have one pending request at any time. 
- the locked_token_amount cannot be lower than the indexing price.
- if **is_privilege_preserved** is true, which means it requests **exclusive** side chain, the token initial issue list cannot be empty and all with an **amount** greater than 0. 
- if **is_privilege_preserved** is true, which means it requests **exclusive** side chain, the **initial_resource_amount** must contain all resource tokens of the chain and the value must be greater than 0. 
- the allowance approved to cross chain contract from the proposer (Sender of the transaction) cannot be lower than the **locked_token_amount**.
- no need to provide data about side chain token if **is_privilege_preserved** is false, and side chain token won’t be created even you provide token info.

.. code:: javascript

       const sideChainCreationRequestTx = await crossChainContract.RequestSideChainCreation({
          indexingPrice: 1,
          lockedTokenAmount: '20000',
          isPrivilegePreserved: true,
          sideChainTokenCreationRequest: {
              sideChainTokenDecimals: 8,
              sideChainTokenName: 'SCATokenName',
              sideChainTokenSymbol: 'SCA',
              sideChainTokenTotalSupply: '100000000000000000',
          },
          sideChainTokenInitialIssueList: [
              {
                  address: '28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK',
                  amount: '1000000000000000'
              }
          ],
          initialResourceAmount: { CPU: 2, RAM: 4, DISK: 512, NET: 1024 },
      });

       let sideChainCreationRequestTxResult = await pollMining(sideChainCreationRequestTx.TransactionId);

       // deserialize the log to get the proposal's ID.
       let deserializedLogs = parliamentContract.deserializeLog(sideChainCreationRequestTxResult.Logs, 'ProposalCreated');

The last line will print the proposal ID and this is what will be used
for approving by the producers.

Approval from producers
~~~~~~~~~~~~~~~~~~~~~~~

This is where the parliament organization members approve the proposal:

.. code:: javascript

       var proposalApproveTx = await parliamentContract.Approve(deserializedLogs[0].proposalId);
       await pollMining(proposalApproveTx.TransactionId);

Note: when calling **Approve** it will be the *Sender* of the
transaction that approves. Here the script is set to use the key of one
parliament organization member, see full script at the end.

Release
~~~~~~~

This part of the script releases the proposal:

.. code:: javascript

       var releaseResult = await crossChainContract.ReleaseSideChainCreation({
           proposalId: deserializedLogs[0].proposalId
       });

       let releaseTxResult = await pollMining(releaseResult.TransactionId);

       // Parse the logs to get the chain id.
       let sideChainCreationEvent = crossChainContract.deserializeLog(releaseTxResult.Logs, 'SideChainCreatedEvent');

This is the last step involved in creating a side chain, after this the
chain id of the new side chain is accessible in the
**SideChainCreatedEvent** event log.

Full script
-----------

This section presents the full script. Remember that in order to run
successfully, a node must be running, configured with one producer. The
configured producer must match the **defaultPrivateKey** and
**defaultPrivateKeyAddress** of the script.

Also, notice that this script by default tries to connect to the node’s
API at the following address http://127.0.0.1:1234, if your node is
listening on a different address you have to modify the address.

If you haven’t already installed it, you need the aelf-sdk:

.. code:: bash

   npm install aelf-sdk

You can simply run the script from anywhere:

.. code:: bash

   node sideChainProposal.js

**sideChainProposal.js**:

.. code:: javascript

   const AElf = require('aelf-sdk');
   const Wallet = AElf.wallet;
   
   const { sha256 } = AElf.utils;
   
   // set the private key of the block producer
   const defaultPrivateKey = 'e119487fea0658badc42f089fbaa56de23d8c0e8d999c5f76ac12ad8ae897d76';
   const defaultPrivateKeyAddress = 'HEtBQStfqu53cHVC3PxJU6iGP3RGxiNUfQGvAPTjfrF3ZWH3U';
   
   const wallet = Wallet.getWalletByPrivateKey(defaultPrivateKey);
   
   // link to the node
   const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:8000'));
   
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
   
       await pollMining(approvalResult.TransactionId);
   }
   
   var checkAllowance = async function(tokenContract, owner, spender)
   {
       console.log('\n>>>> Checking the cross-chain contract\'s allowance');
   
       const checkAllowanceTx = await tokenContract.GetAllowance.call({
           symbol: 'ELF',
           owner: owner,
           spender: spender
       });
   
       console.log(`>> allowance to the cross-chain contract: ${checkAllowanceTx.allowance} ${checkAllowanceTx.symbol}`);
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
   
       // build the aelf-sdk contract object
       const parliamentContract = await aelf.chain.contractAt(parliamentContractAddress, wallet);
       const tokenContract = await aelf.chain.contractAt(tokenContractAddress, wallet);
       const crossChainContract = await aelf.chain.contractAt(crossChainContractAddress, wallet);
   
   
       // 1. set and check the allowance, spender is the cross-chain contract
       await setAllowance(tokenContract, crossChainContractAddress);
       await checkAllowance(tokenContract, defaultPrivateKeyAddress, crossChainContractAddress);
   
       // 2. request the creation of the side chain with the cross=chain contract
       console.log('\n>>>> Requesting the side chain creation.');
       const sideChainCreationRequestTx = await crossChainContract.RequestSideChainCreation({
           indexingPrice: 1,
           lockedTokenAmount: '20000',
           isPrivilegePreserved: true,
           sideChainTokenCreationRequest: {
               sideChainTokenDecimals: 8,
               sideChainTokenName: 'SCATokenName',
               sideChainTokenSymbol: 'SCA',
               sideChainTokenTotalSupply: '100000000000000000',
           },
           sideChainTokenInitialIssueList: [
               {
                   address: '28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK',
                   amount: '1000000000000000'
               }
           ],
           initialResourceAmount: { CPU: 2, RAM: 4, DISK: 512, NET: 1024 },
       });
   
       let sideChainCreationRequestTxResult = await pollMining(sideChainCreationRequestTx.TransactionId);
   
       // deserialize the log to get the proposal's ID.
       let deserializedLogs = parliamentContract.deserializeLog(sideChainCreationRequestTxResult.Logs, 'ProposalCreated');
       console.log(`>> side chain creation request proposal id ${JSON.stringify(deserializedLogs[0].proposalId)}`);
   
       // 3. Approve the proposal
       console.log('\n>>>> Approving the proposal.');
   
       var proposalApproveTx = await parliamentContract.Approve(deserializedLogs[0].proposalId);
       await pollMining(proposalApproveTx.TransactionId);
   
       // 3. Release the side chain
       console.log('\n>>>> Release the side chain.');
   
       var releaseResult = await crossChainContract.ReleaseSideChainCreation({
           proposalId: deserializedLogs[0].proposalId
       });
   
       let releaseTxResult = await pollMining(releaseResult.TransactionId);
   
       // Parse the logs to get the chain id.
       let sideChainCreationEvent = crossChainContract.deserializeLog(releaseTxResult.Logs, 'SideChainCreatedEvent');
       console.log('Chain chain created : ');
       console.log(sideChainCreationEvent);
   };
   
   createSideChain().then(() => {console.log('Done.')});
