aelf-sdk.java - AELF Java API
=============================

This Java library helps in the communication with an AElf node. You can
find out more `here <https://github.com/AElfProject/aelf-sdk.java>`__.

Introduction
------------

aelf-sdk.java is a collection of libraries which allow you to interact
with a local or remote aelf node, using a HTTP connection.

The following documentation will guide you through installing and
running aelf-sdk.java, as well as providing a API reference
documentation with examples.

If you need more information you can check out the repo :
`aelf-sdk.java <https://github.com/AElfProject/aelf-sdk.java>`__

Adding aelf-sdk.java package
----------------------------

First you need to get elf-sdk.java package into your project:
`MvnRepository <https://mvnrepository.com/artifact/io.aelf/aelf-sdk>`__

Maven:

::

   <!-- https://mvnrepository.com/artifact/io.aelf/aelf-sdk -->
   <dependency>
       <groupId>io.aelf</groupId>
       <artifactId>aelf-sdk</artifactId>
       <version>0.X.X</version>
   </dependency>

Examples
--------

Create instance
~~~~~~~~~~~~~~~

Create a new instance of AElfClient, and set url of an AElf chain node.

.. code:: java

   using AElf.Client.Service;

   // create a new instance of AElf, change the URL if needed
   AElfClient client = new AElfClient("http://127.0.0.1:1235");

Test connection
~~~~~~~~~~~~~~~

Check that the AElf chain node is connectable.

.. code:: java

   boolean isConnected = client.isConnected();

Initiate a transfer transaction
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code:: java

   // Get token contract address.
   String tokenContractAddress = client.getContractAddressByName(privateKey, Sha256.getBytesSha256("AElf.ContractNames.Token"));

   Client.Address.Builder to = Client.Address.newBuilder();
   to.setValue(ByteString.copyFrom(Base58.decodeChecked("7s4XoUHfPuqoZAwnTV7pHWZAaivMiL8aZrDSnY9brE1woa8vz")));
   Client.Address toObj = to.build();

   TokenContract.TransferInput.Builder paramTransfer = TokenContract.TransferInput.newBuilder();
   paramTransfer.setTo(toObj);
   paramTransfer.setSymbol("ELF");
   paramTransfer.setAmount(1000000000);
   paramTransfer.setMemo("transfer in demo");
   TokenContract.TransferInput paramTransferObj = paramTransfer.build();

   String ownerAddress = client.getAddressFromPrivateKey(privateKey);

   Transaction.Builder transactionTransfer = client.generateTransaction(ownerAddress, tokenContractAddress, "Transfer", paramTransferObj.toByteArray());
   Transaction transactionTransferObj = transactionTransfer.build();
   transactionTransfer.setSignature(ByteString.copyFrom(ByteArrayHelper.hexToByteArray(client.signTransaction(privateKey, transactionTransferObj))));
   transactionTransferObj = transactionTransfer.build();

   // Send the transfer transaction to AElf chain node.
   SendTransactionInput sendTransactionInputObj = new SendTransactionInput();
   sendTransactionInputObj.setRawTransaction(Hex.toHexString(transactionTransferObj.toByteArray()));
   SendTransactionOutput sendResult = client.sendTransaction(sendTransactionInputObj);

   Thread.sleep(4000);
   // After the transaction is mined, query the execution results.
   TransactionResultDto transactionResult = client.getTransactionResult(sendResult.getTransactionId());
   System.out.println(transactionResult.getStatus());

   // Query account balance.
   Client.Address.Builder owner = Client.Address.newBuilder();
   owner.setValue(ByteString.copyFrom(Base58.decodeChecked(ownerAddress)));
   Client.Address ownerObj = owner.build();

   TokenContract.GetBalanceInput.Builder paramGetBalance = TokenContract.GetBalanceInput.newBuilder();
   paramGetBalance.setSymbol("ELF");
   paramGetBalance.setOwner(ownerObj);
   TokenContract.GetBalanceInput paramGetBalanceObj = paramGetBalance.build();

   Transaction.Builder transactionGetBalance = client.generateTransaction(ownerAddress, tokenContractAddress, "GetBalance", paramGetBalanceObj.toByteArray());
   Transaction transactionGetBalanceObj = transactionGetBalance.build();
   String signature = client.signTransaction(privateKey, transactionGetBalanceObj);
   transactionGetBalance.setSignature(ByteString.copyFrom(ByteArrayHelper.hexToByteArray(signature)));
   transactionGetBalanceObj = transactionGetBalance.build();

   ExecuteTransactionDto executeTransactionDto = new ExecuteTransactionDto();
   executeTransactionDto.setRawTransaction(Hex.toHexString(transactionGetBalanceObj.toByteArray()));
   String transactionGetBalanceResult = client.executeTransaction(executeTransactionDto);

   TokenContract.GetBalanceOutput balance = TokenContract.GetBalanceOutput.getDefaultInstance().parseFrom(ByteArrayHelper.hexToByteArray(transactionGetBalanceResult));
   System.out.println(balance.getBalance());

Web API
-------

*You can see how the Web Api of the node works in
``{chainAddress}/swagger/index.html``* *tip: for an example, my local
address: ‘http://127.0.0.1:1235/swagger/index.html’*

The usage of these methods is based on the AElfClient instance, so if
you don’t have one please create it:

.. code:: java

   using AElf.Client.Service;

   // create a new instance of AElf, change the URL if needed
   AElfClient client = new AElfClient("http://127.0.0.1:1235");

GetChainStatus
~~~~~~~~~~~~~~

Get the current status of the block chain.

*Web API path*

``/api/blockChain/chainStatus``

*Parameters*

Empty

*Returns*

-  ``ChainStatusDto``

   -  ``ChainId - String``
   -  ``Branches - HashMap<String, Long>``
   -  ``NotLinkedBlocks - ashMap<String, String>``
   -  ``LongestChainHeight - long``
   -  ``LongestChainHash - String``
   -  ``GenesisBlockHash - String``
   -  ``GenesisContractAddress - String``
   -  ``LastIrreversibleBlockHash - String``
   -  ``LastIrreversibleBlockHeight - long``
   -  ``BestChainHash - String``
   -  ``BestChainHeight - long``

*Example*

.. code:: java

   client.getChainStatus();

GetContractFileDescriptorSet
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Get the protobuf definitions related to a contract.

*Web API path*

``/api/blockChain/contractFileDescriptorSet``

*Parameters*

-  ``contractAddress - String`` address of a contract

*Returns*

-  ``byte[]``

*Example*

.. code:: java

   client.getContractFileDescriptorSet(address);

GetBlockHeight
~~~~~~~~~~~~~~

Get current best height of the chain.

*Web API path*

``/api/blockChain/blockHeight``

*Parameters*

Empty

*Returns*

-  ``long``

*Example*

.. code:: java

   client.getBlockHeight();

GetBlock
~~~~~~~~

Get block information by block hash.

*Web API path*

``/api/blockChain/block``

*Parameters*

-  ``blockHash - String``
-  ``includeTransactions - boolean`` :

   -  ``true`` require transaction ids list in the block
   -  ``false`` Doesn’t require transaction ids list in the block

*Returns*

-  ``BlockDto``

   -  ``BlockHash - String``
   -  ``Header - BlockHeaderDto``

      -  ``PreviousBlockHash - String``
      -  ``MerkleTreeRootOfTransactions - String``
      -  ``MerkleTreeRootOfWorldState - String``
      -  ``Extra - String``
      -  ``Height - long``
      -  ``Time - Date``
      -  ``ChainId - String``
      -  ``Bloom - String``
      -  ``SignerPubkey - String``

   -  ``Body - BlockBodyDto``

      -  ``TransactionsCount - int``
      -  ``Transactions - List<String>``

*Example*

.. code:: java

   client.getBlockByHash(blockHash);

GetBlockByHeight
~~~~~~~~~~~~~~~~

Get block information by block height.

*Web API path*

``/api/blockChain/blockByHeight``

*Parameters*

-  ``blockHeight - long``
-  ``includeTransactions - boolean`` :

   -  ``true`` require transaction ids list in the block
   -  ``false`` Doesn’t require transaction ids list in the block

*Returns*

-  ``BlockDto``

   -  ``BlockHash - String``
   -  ``Header - BlockHeaderDto``

      -  ``PreviousBlockHash - String``
      -  ``MerkleTreeRootOfTransactions - String``
      -  ``MerkleTreeRootOfWorldState - String``
      -  ``Extra - String``
      -  ``Height - long``
      -  ``Time - Date``
      -  ``ChainId - String``
      -  ``Bloom - String``
      -  ``SignerPubkey - String``

   -  ``Body - BlockBodyDto``

      -  ``TransactionsCount - int``
      -  ``Transactions - List<String>``

*Example*

.. code:: java

   client.getBlockByHeight(height);

GetTransactionResult
~~~~~~~~~~~~~~~~~~~~

Get the result of a transaction.

*Web API path*

``/api/blockChain/transactionResult``

*Parameters*

-  ``transactionId - String``

*Returns*

-  ``TransactionResultDto``

   -  ``TransactionId - String``
   -  ``Status - String``
   -  ``Logs - ist<LogEventDto>``

      -  ``Address - String``
      -  ``Name - String``
      -  ``Indexed - List<String>``
      -  ``NonIndexed - String``

   -  ``Bloom - String``
   -  ``BlockNumber - long``
   -  ``Transaction - TransactionDto``

      -  ``From - String``
      -  ``To - String``
      -  ``RefBlockNumber - long``
      -  ``RefBlockPrefix - String``
      -  ``MethodName - String``
      -  ``Params - String``
      -  ``Signature - String``

   -  ``Error - String``

*Example*

.. code:: java

   client.getTransactionResult(transactionId);

GetTransactionResults
~~~~~~~~~~~~~~~~~~~~~

Get multiple transaction results in a block.

*Web API path*

``/api/blockChain/transactionResults``

*Parameters*

-  ``blockHash - String``
-  ``offset - int``
-  ``limit - int``

*Returns*

-  ``List<TransactionResultDto>`` - The array of transaction result:

   -  the transaction result object

*Example*

.. code:: java

   client.getTransactionResults(blockHash, 0, 10);

GetTransactionPoolStatus
~~~~~~~~~~~~~~~~~~~~~~~~

Get the transaction pool status.

*Web API path*

``/api/blockChain/transactionPoolStatus``

*Parameters*

Empty

*Returns*

-  ``TransactionPoolStatusOutput``

   -  ``Queued`` - int
   -  ``Validated`` - int

*Example*

.. code:: java

   client.getTransactionPoolStatus();

SendTransaction
~~~~~~~~~~~~~~~

Broadcast a transaction.

*Web API path*

``/api/blockChain/sendTransaction``

*POST*

*Parameters*

-  ``SendTransactionInput`` - Serialization of data into protobuf data:

   -  ``RawTransaction - String``

*Returns*

-  ``SendTransactionOutput``

   -  ``TransactionId - String``

*Example*

.. code:: java

   client.sendTransaction(input);

SendRawTransaction
~~~~~~~~~~~~~~~~~~

Broadcast a transaction.

*Web API path*

``/api/blockChain/sendTransaction``

*POST*

*Parameters*

-  ``SendRawTransactionInput`` - Serialization of data into protobuf
   data:

   -  ``Transaction - String``
   -  ``Signature - String``
   -  ``ReturnTransaction - boolean``

*Returns*

-  ``SendRawTransactionOutput``

   -  ``TransactionId - String``
   -  ``Transaction - TransactionDto``

*Example*

.. code:: java

   client.sendRawTransaction(input);

SendTransactions
~~~~~~~~~~~~~~~~

Broadcast multiple transactions.

*Web API path*

``/api/blockChain/sendTransactions``

*POST*

*Parameters*

-  ``SendTransactionsInput`` - Serialization of data into protobuf data:

   -  ``RawTransactions - String``

*Returns*

-  ``List<String>``

*Example*

.. code:: java

   client.sendTransactions(input);

CreateRawTransaction
~~~~~~~~~~~~~~~~~~~~

Creates an unsigned serialized transaction.

*Web API path*

``/api/blockChain/rawTransaction``

*POST*

*Parameters*

-  ``CreateRawTransactionInput``

   -  ``From - String``
   -  ``To - String``
   -  ``RefBlockNumber - long``
   -  ``RefBlockHash - String``
   -  ``MethodName - String``
   -  ``Params - String``

*Returns*

-  ``CreateRawTransactionOutput``- Serialization of data into protobuf
   data:

   -  ``RawTransaction - String``

*Example*

.. code:: java

   client.createRawTransaction(input);

ExecuteTransaction
~~~~~~~~~~~~~~~~~~

Call a read-only method on a contract.

*Web API path*

``/api/blockChain/executeTransaction``

*POST*

*Parameters*

-  ``ExecuteTransactionDto`` - Serialization of data into protobuf data:

   -  ``RawTransaction - String``

*Returns*

-  ``String``

*Example*

.. code:: java

   client.executeTransaction(input);

ExecuteRawTransaction
~~~~~~~~~~~~~~~~~~~~~

Call a read-only method on a contract.

*Web API path*

``/api/blockChain/executeRawTransaction``

*POST*

*Parameters*

-  ``ExecuteRawTransactionDto`` - Serialization of data into protobuf
   data:

   -  ``RawTransaction - String``
   -  ``Signature - String``

*Returns*

-  ``String``

*Example*

.. code:: java

   client.executeRawTransaction(input);

GetPeers
~~~~~~~~

Get peer info about the connected network nodes.

*Web API path*

``/api/net/peers``

*Parameters*

-  ``withMetrics - boolean``

*Returns*

-  ``List<PeerDto>``

   -  ``IpAddress - String``
   -  ``ProtocolVersion - int``
   -  ``ConnectionTime - long``
   -  ``ConnectionStatus - String``
   -  ``Inbound - boolean``
   -  ``BufferedTransactionsCount - int``
   -  ``BufferedBlocksCount - int``
   -  ``BufferedAnnouncementsCount - int``
   -  ``NodeVersion - String``
   -  ``RequestMetrics - List<RequestMetric>``

      -  ``RoundTripTime - long``
      -  ``MethodName - String``
      -  ``Info - String``
      -  ``RequestTime - String``

*Example*

.. code:: java

   client.getPeers(false);

AddPeer
~~~~~~~

Attempts to add a node to the connected network nodes.

*Web API path*

``/api/net/peer``

*POST*

*Parameters*

-  ``AddPeerInput``

   -  ``Address - String``

*Returns*

-  ``boolean``

*Example*

.. code:: java

   client.addPeer("127.0.0.1:7001");

RemovePeer
~~~~~~~~~~

Attempts to remove a node from the connected network nodes.

*Web API path*

``/api/net/peer``

*DELETE*

*Parameters*

-  ``address - string``

*Returns*

-  ``boolean``

*Example*

.. code:: java

   client.removePeer("127.0.0.1:7001");

calculateTransactionFee
~~~~~~~~~~~~~~~~~~~~~~~

Estimate transaction fee.

*Web API path*

``/api/blockChain/calculateTransactionFee``

*POST*

*Parameters*

-  ``CalculateTransactionFeeInput``

   -  ``RawTrasaction - string``

*Returns*

-  ``CalculateTransactionFeeOutput`` - The object with the following
   structure :

   -  ``Success - boolean``
   -  ``TransactionFee - HashMap<String, Long>``
   -  ``ResourceFee - HashMap<String, Long>``

*Example*

.. code:: java

   CalculateTransactionFeeOutput output = client.calculateTransactionFee(input);

GetNetworkInfo
~~~~~~~~~~~~~~

Get the network information of the node.

*Web API path*

``/api/net/networkInfo``

*Parameters*

Empty

*Returns*

-  ``NetworkInfoOutput``

   -  ``Version - String``
   -  ``ProtocolVersion - int``
   -  ``Connections - int``

*Example*

.. code:: java

   client.getNetworkInfo();

AElf Client
-----------

IsConnected
~~~~~~~~~~~

Verify whether this sdk successfully connects the chain.

*Parameters*

Empty

*Returns*

-  ``boolean``

*Example*

.. code:: java

   client.isConnected();

GetGenesisContractAddress
~~~~~~~~~~~~~~~~~~~~~~~~~

Get the address of genesis contract.

*Parameters*

Empty

*Returns*

-  ``String``

*Example*

.. code:: java

   client.getGenesisContractAddress();

GetContractAddressByName
~~~~~~~~~~~~~~~~~~~~~~~~

Get address of a contract by given contractNameHash.

*Parameters*

-  ``privateKey - String``
-  ``contractNameHash - byte[]``

*Returns*

-  ``String``

*Example*

.. code:: java

   client.getContractAddressByName(privateKey, contractNameHash);

GenerateTransaction
~~~~~~~~~~~~~~~~~~~

Build a transaction from the input parameters.

*Parameters*

-  ``from - String``
-  ``to - String``
-  ``methodName - String``
-  ``input - byte[]``

*Returns*

-  ``Transaction``

*Example*

.. code:: java

   client.generateTransaction(from, to, methodName, input);

GetFormattedAddress
~~~~~~~~~~~~~~~~~~~

Convert the Address to the displayed
string：symbol_base58-string_base58-String-chain-id.

*Parameters*

-  ``privateKey - String``
-  ``address - String``

*Returns*

-  ``String``

*Example*

.. code:: java

   client.getFormattedAddress(privateKey, address);

SignTransaction
~~~~~~~~~~~~~~~

Sign a transaction using private key.

*Parameters*

-  ``privateKeyHex - String``
-  ``transaction - Transaction``

*Returns*

-  ``String``

*Example*

.. code:: java

   client.signTransaction(privateKeyHex, transaction);

GetAddressFromPubKey
~~~~~~~~~~~~~~~~~~~~

Get the account address through the public key.

*Parameters*

-  ``pubKey - String``

*Returns*

-  ``String``

*Example*

.. code:: java

   client.getAddressFromPubKey(pubKey);

GetAddressFromPrivateKey
~~~~~~~~~~~~~~~~~~~~~~~~

Get the account address through the private key.

*Parameters*

-  ``privateKey - String``

*Returns*

-  ``String``

*Example*

.. code:: java

   client.getAddressFromPrivateKey(privateKey);

GenerateKeyPairInfo
~~~~~~~~~~~~~~~~~~~

Generate a new account key pair.

*Parameters*

Empty

*Returns*

-  ``KeyPairInfo``

   -  ``PrivateKey - String``
   -  ``PublicKey - String``
   -  ``Address - String``

*Example*

.. code:: java

   client.generateKeyPairInfo();

Supports
--------

-  JDK1.8+
-  Log4j2.6.2
