aelf-sdk.js - AELF JavaScript API
=================================

Introduction
------------

aelf-sdk.js for aelf is like web.js for ethereum.

aelf-sdk.js is a collection of libraries which allow you to interact
with a local or remote aelf node, using a HTTP connection.

The following documentation will guide you through installing and
running aelf-sdk.js, as well as providing a API reference documentation
with examples.

If you need more information you can check out the repo :
`aelf-sdk.js <https://github.com/AElfProject/aelf-sdk.js>`__

Adding aelf-sdk.js
------------------

First you need to get aelf-sdk.js into your project. This can be done
using the following methods:

npm: ``npm install aelf-sdk``

pure js: ``link dist/aelf.umd.js``

After that you need to create a aelf instance and set a provider.

.. code:: javascript

   // in brower use: <script src="https://unpkg.com/aelf-sdk@lastest/dist/aelf.umd.js"></script>
   // in node.js use: const AElf = require('aelf-sdk');
   const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:8000'));

Examples
--------

You can also see full examples in ``./examples``;

Create instance
~~~~~~~~~~~~~~~

Create a new instance of AElf, connect to an AELF chain node.

.. code:: javascript

   import AElf from 'aelf-sdk';

   // create a new instance of AElf
   const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:1235'));

Create or load a wallet
~~~~~~~~~~~~~~~~~~~~~~~

Create or load a wallet with ``AElf.wallet``

.. code:: javascript

   // create a new wallet
   const newWallet = AElf.wallet.createNewWallet();
   // load a wallet by private key
   const priviteKeyWallet = AElf.wallet.getWalletByPrivateKey('xxxxxxx');
   // load a wallet by mnemonic
   const mnemonicWallet = AElf.wallet.getWalletByMnemonic('set kite ...');

Get a system contract address
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Get a system contract address, take ``AElf.ContractNames.Token`` as an
example

.. code:: javascript

   const tokenContractName = 'AElf.ContractNames.Token';
   let tokenContractAddress;
   (async () => {
     // get chain status
     const chainStatus = await aelf.chain.getChainStatus();
     // get genesis contract address
     const GenesisContractAddress = chainStatus.GenesisContractAddress;
     // get genesis contract instance
     const zeroContract = await aelf.chain.contractAt(GenesisContractAddress, newWallet);
     // Get contract address by the read only method `GetContractAddressByName` of genesis contract
     tokenContractAddress = await zeroContract.GetContractAddressByName.call(AElf.utils.sha256(tokenContractName));
   })()

Get a contract instance
~~~~~~~~~~~~~~~~~~~~~~~

Get a contract instance by contract address

.. code:: javascript

   const wallet = AElf.wallet.createNewWallet();
   let tokenContract;
   // Use token contract for examples to demonstrate how to get a contract instance in different ways
   // in async function
   (async () => {
     tokenContract = await aelf.chain.contractAt(tokenContractAddress, wallet)
   })();

   // promise way
   aelf.chain.contractAt(tokenContractAddress, wallet)
     .then(result => {
       tokenContract = result;
     });

   // callback way
   aelf.chain.contractAt(tokenContractAddress, wallet, (error, result) => {if (error) throw error; tokenContract = result;});

Use contract instance
~~~~~~~~~~~~~~~~~~~~~

How to use contract instance

::

   A contract instance consists of several contract methods and methods can be called in two ways: read-only and send transaction.

.. code:: javascript

   (async () => {
     // get the balance of an address, this would not send a transaction,
     // or store any data on the chain, or required any transaction fee, only get the balance
     // with `.call` method, `aelf-sdk` will only call read-only method
     const result = await tokenContract.GetBalance.call({
       symbol: "ELF",
       owner: "7s4XoUHfPuqoZAwnTV7pHWZAaivMiL8aZrDSnY9brE1woa8vz"
     });
     console.log(result);
     /**
     {
       "symbol": "ELF",
       "owner": "2661mQaaPnzLCoqXPeys3Vzf2wtGM1kSrqVBgNY4JUaGBxEsX8",
       "balance": "1000000000000"
     }*/
     // with no `.call`, `aelf-sdk` will sign and send a transaction to the chain, and return a transaction id.
     // make sure you have enough transaction fee `ELF` in your wallet
     const transactionId = await tokenContract.Transfer({
       symbol: "ELF",
       to: "7s4XoUHfPuqoZAwnTV7pHWZAaivMiL8aZrDSnY9brE1woa8vz",
       amount: "1000000000",
       memo: "transfer in demo"
     });
     console.log(transactionId);
     /**
       {
         "TransactionId": "123123"
       }
     */
   })()

Change the node endpoint
~~~~~~~~~~~~~~~~~~~~~~~~

Change the node endpoint by using ``aelf.setProvider``

.. code:: javascript

   import AElf from 'aelf-sdk';

   const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:1235'));
   aelf.setProvider(new AElf.providers.HttpProvider('http://127.0.0.1:8000'));

Web API
-------

*You can see how the Web Api of the node works in
``{chainAddress}/swagger/index.html``*

*tip: for an example, my local address:
‘http://127.0.0.1:1235/swagger/index.html’*

parameters and returns based on the URL:
``https://aelf-public-node.aelf.io/swagger/index.html``

The usage of these methods is based on the AElf instance, so if you
don’t have one please create it:

.. code:: javascript

   import AElf from 'aelf-sdk';

   // create a new instance of AElf, change the URL if needed
   const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:1235'));

1.getChainStatus
~~~~~~~~~~~~~~~~

Get the current status of the block chain.

*Web API path*

``/api/blockChain/chainStatus``

*GET*

*Parameters*

Empty

*Returns*

-  ``Object``

   -  ``ChainId - String``
   -  ``Branches - Object``
   -  ``NotLinkedBlocks - Object``
   -  ``LongestChainHeight - Number``
   -  ``LongestChainHash - String``
   -  ``GenesisBlockHash - String``
   -  ``GenesisContractAddress - String``
   -  ``LastIrreversibleBlockHash - String``
   -  ``LastIrreversibleBlockHeight - Number``
   -  ``BestChainHash - String``
   -  ``BestChainHeight - Number``

*Example*

.. code:: javascript

   aelf.chain.getChainStatus()
   .then(res => {
     console.log(res);
   })

2.getContractFileDescriptorSet
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Get the protobuf definitions related to a contract

*Web API path*

``/api/blockChain/contractFileDescriptorSet``

*GET*

*Parameters*

-  ``contractAddress - String`` address of a contract

*Returns*

-  ``String``

*Example*

.. code:: javascript


   aelf.chain.getContractFileDescriptorSet(contractAddress)
     .then(res => {
       console.log(res);
     })

3.getBlockHeight
~~~~~~~~~~~~~~~~

Get current best height of the chain.

*Web API path*

``/api/blockChain/blockHeight``

*GET*

*Parameters*

Empty

*Returns*

-  ``Number``

*Example*

.. code:: javascript

   aelf.chain.getBlockHeight()
     .then(res => {
       console.log(res);
     })

4.getBlock
~~~~~~~~~~

Get block information by block hash.

*Web API path*

``/api/blockChain/block``

*Parameters*

-  ``blockHash - String``
-  ``includeTransactions - Boolean`` :

   -  ``true`` require transaction ids list in the block
   -  ``false`` Doesn’t require transaction ids list in the block

*Returns*

-  ``Object``

   -  ``BlockHash - String``
   -  ``Header - Object``

      -  ``PreviousBlockHash - String``
      -  ``MerkleTreeRootOfTransactions - String``
      -  ``MerkleTreeRootOfWorldState - String``
      -  ``Extra - Array``
      -  ``Height - Number``
      -  ``Time - google.protobuf.Timestamp``
      -  ``ChainId - String``
      -  ``Bloom - String``
      -  ``SignerPubkey - String``

   -  ``Body - Object``

      -  ``TransactionsCount - Number``
      -  ``Transactions - Array``

         -  ``transactionId - String``

*Example*

.. code:: javascript

   aelf.chain.getBlock(blockHash, false)
     .then(res => {
       console.log(res);
     })

5.getBlockByHeight
~~~~~~~~~~~~~~~~~~

*Web API path*

``/api/blockChain/blockByHeight``

Get block information by block height.

*Parameters*

-  ``blockHeight - Number``
-  ``includeTransactions - Boolean`` :

   -  ``true`` require transaction ids list in the block
   -  ``false`` Doesn’t require transaction ids list in the block

*Returns*

-  ``Object``

   -  ``BlockHash - String``
   -  ``Header - Object``

      -  ``PreviousBlockHash - String``
      -  ``MerkleTreeRootOfTransactions - String``
      -  ``MerkleTreeRootOfWorldState - String``
      -  ``Extra - Array``
      -  ``Height - Number``
      -  ``Time - google.protobuf.Timestamp``
      -  ``ChainId - String``
      -  ``Bloom - String``
      -  ``SignerPubkey - String``

   -  ``Body - Object``

      -  ``TransactionsCount - Number``
      -  ``Transactions - Array``

         -  ``transactionId - String``

*Example*

.. code:: javascript

   aelf.chain.getBlockByHeight(12, false)
     .then(res => {
       console.log(res);
     })

6.getTxResult
~~~~~~~~~~~~~

Get the result of a transaction

*Web API path*

``/api/blockChain/transactionResult``

*Parameters*

-  ``transactionId - String``

*Returns*

-  ``Object``

   -  ``TransactionId - String``
   -  ``Status - String``
   -  ``Logs - Array``

      -  ``Address - String``
      -  ``Name - String``
      -  ``Indexed - Array``
      -  ``NonIndexed - String``

   -  ``Bloom - String``
   -  ``BlockNumber - Number``
   -  ``Transaction - Object``

      -  ``From - String``
      -  ``To - String``
      -  ``RefBlockNumber - Number``
      -  ``RefBlockPrefix - String``
      -  ``MethodName - String``
      -  ``Params - Object``
      -  ``Signature - String``

   -  ``ReadableReturnValue - Object``
   -  ``Error - String``

*Example*

.. code:: javascript

   aelf.chain.getTxResult(transactionId)
     .then(res => {
       console.log(res);
     })

7.getTxResults
~~~~~~~~~~~~~~

Get multiple transaction results in a block

*Web API path*

``/api/blockChain/transactionResults``

*Parameters*

-  ``blockHash - String``
-  ``offset - Number``
-  ``limit - Number``

*Returns*

-  ``Array`` - The array of method descriptions:

   -  the transaction result object

*Example*

.. code:: javascript

   aelf.chain.getTxResults(blockHash, 0, 2)
     .then(res => {
       console.log(res);
     })

8.getTransactionPoolStatus
~~~~~~~~~~~~~~~~~~~~~~~~~~

Get the transaction pool status.

*Web API path*

``/api/blockChain/transactionPoolStatus``

*Parameters*

Empty

9.sendTransaction
~~~~~~~~~~~~~~~~~

Broadcast a transaction

*Web API path*

``/api/blockChain/sendTransaction``

*POST*

*Parameters*

-  ``Object`` - Serialization of data into protobuf data, The object
   with the following structure :

   -  ``RawTransaction - String`` :

usually developers don’t need to use this function directly, just get a
contract method and send transaction by call contract method:

10.sendTransactions
~~~~~~~~~~~~~~~~~~~

Broadcast multiple transactions

*POST*

*Parameters*

-  ``Object`` - The object with the following structure :

   -  ``RawTransaction - String``

11.callReadOnly
~~~~~~~~~~~~~~~

Call a read-only method on a contract.

*POST*

*Parameters*

-  ``Object`` - The object with the following structure :

   -  ``RawTransaction - String``

12.getPeers
~~~~~~~~~~~

Get peer info about the connected network nodes

*GET*

*Parameters*

-  ``withMetrics - Boolean`` :

   -  ``true`` with metrics
   -  ``false`` without metrics

13.addPeer
~~~~~~~~~~

Attempts to add a node to the connected network nodes

*POST*

*Parameters*

-  ``Object`` - The object with the following structure :

   -  ``Address - String``

14.removePeer
~~~~~~~~~~~~~

Attempts to remove a node from the connected network nodes

*DELETE*

*Parameters*

-  ``address - String``

15.calculateTransactionFee
~~~~~~~~~~~~~~~~~~~~~~~~~~

Estimate transaction fee

*Wbe API path*

``/api/blockChain/calculateTransactionFee``

*POST*

*Parameters*

-  ``CalculateTransactionFeeInput - Object`` - The object with the
   following structure :

   -  ``RawTransaction - String``

*Returns*

-  ``CalculateTransactionFeeOutput - Object`` - The object with the
   following structure :

   -  ``Success - Bool``
   -  ``TransactionFee - Array``
   -  ``ResourceFee - Array``

*Example*

.. code:: javascript

   aelf.chain.calculateTransactionFee(rawTransaction)
      .then(res => {
         console.log(res);
      })

16.networkInfo
~~~~~~~~~~~~~~

Get information about the node’s connection to the network

*GET*

*Parameters*

Empty

AElf.wallet
-----------

``AElf.wallet`` is a static property of ``AElf``.

*Use the api to see detailed results*

1.createNewWallet
~~~~~~~~~~~~~~~~~

*Returns*

-  ``Object``

   -  ``mnemonic - String``: mnemonic
   -  ``BIP44Path - String``:
      m/purpose’/coin_type’/account’/change/address_index
   -  ``childWallet - Object``: HD Wallet
   -  ``keyPair - String``: The EC key pair generated by elliptic
   -  ``privateKey - String``: private Key
   -  ``address - String``: address

*Example*

.. code:: javascript

   import AElf from 'aelf-sdk';
   const wallet = AElf.wallet.createNewWallet();

2.getWalletByMnemonic
~~~~~~~~~~~~~~~~~~~~~

*Parameters*

``mnemonic - String`` : wallet’s mnemonic

*Returns*

-  ``Object``: Complete wallet object.

*Example*

.. code:: javascript

   const wallet = AElf.wallet.getWalletByMnemonic(mnemonic);

3.getWalletByPrivateKey
~~~~~~~~~~~~~~~~~~~~~~~

*Parameters*

-  ``privateKey: String`` : wallet’s private key

*Returns*

-  ``Object``: Complete wallet object, with empty mnemonic

*Example*

.. code:: javascript

   const wallet = AElf.wallet.getWalletByPrivateKey(privateKey);

4.signTransaction
~~~~~~~~~~~~~~~~~

Use wallet ``keypair`` to sign a transaction

*Parameters*

-  ``rawTxn - String``
-  ``keyPair - String``

*Returns*

-  ``Object``: The object with the following structure :

*Example*

.. code:: javascript

   const result = aelf.wallet.signTransaction(rawTxn, keyPair);

5.AESEncrypt
~~~~~~~~~~~~

Encrypt a string by aes algorithm

*Parameters*

-  ``input - String``
-  ``password - String``

*Returns*

-  ``String``

6.AESDecrypt
~~~~~~~~~~~~

Decrypt by aes algorithm

*Parameters*

-  ``input - String``
-  ``password - String``

*Returns*

-  ``String``

AElf.pbjs
---------

The reference to protobuf.js, read the
`documentation <https://github.com/protobufjs/protobuf.js>`__ to see how
to use.

AElf.pbUtils
------------

Some basic format methods of aelf.

For more information, please see the code in ``src/utils/proto.js``. It
is simple and easy to understand.

AElf.utils
~~~~~~~~~~

Some methods for aelf.

For more information, please see the code in ``src/utils/utils.js``. It
is simple and easy to understand.

Check address
^^^^^^^^^^^^^

.. code:: javascript

   const AElf = require('aelf-sdk');
   const {base58} = AElf.utils;
   base58.decode('$addresss'); // throw error if invalid

AElf.version
------------

.. code:: javascript

   import AElf from 'aelf-sdk';
   AElf.version // eg. 3.2.23

Requirements
------------

-  `Node.js <https://nodejs.org>`__
-  `NPM <http://npmjs.com/>`__

Support
-------

|browsers| |node|

About contributing
------------------

Read out [contributing guide]

About Version
-------------

https://semver.org/

.. |browsers| image:: https://img.shields.io/badge/browsers-latest%202%20versions-brightgreen.svg
.. |node| image:: https://img.shields.io/badge/node-%3E=10-green.svg
