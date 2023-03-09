aelf-sdk.cs - AELF C# API
=========================

This C# library helps in the communication with an AElf node. You can
find out more `here <https://github.com/AElfProject/aelf-sdk.cs>`__.

Introduction
------------

aelf-sdk.cs is a collection of libraries which allow you to interact
with a local or remote aelf node, using a HTTP connection.

The following documentation will guide you through installing and
running aelf-sdk.cs, as well as providing a API reference documentation
with examples.

If you need more information you can check out the repo :
`aelf-sdk.cs <https://github.com/AElfProject/aelf-sdk.cs>`__

Adding aelf-sdk.cs package
--------------------------

First you need to get AElf.Client package into your project. This can be
done using the following methods:

Package Manager:

::

   PM> Install-Package AElf.Client

.NET CLI

::

   > dotnet add package AElf.Client

PackageReference

::

   <PackageReference Include="AElf.Client" Version="X.X.X" />

Examples
--------

Create instance
~~~~~~~~~~~~~~~

Create a new instance of AElfClient, and set url of an AElf chain node.

.. code:: c#

   using AElf.Client.Service;

   // create a new instance of AElfClient
   AElfClient client = new AElfClient("http://127.0.0.1:1235");

Test connection
~~~~~~~~~~~~~~~

Check that the AElf chain node is connectable.

.. code:: c#

   var isConnected = await client.IsConnectedAsync();

Initiate a transfer transaction
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code:: c#

   // Get token contract address.
   var tokenContractAddress = await client.GetContractAddressByNameAsync(HashHelper.ComputeFrom("AElf.ContractNames.Token"));

   var methodName = "Transfer";
   var param = new TransferInput
   {
       To = new Address {Value = Address.FromBase58("7s4XoUHfPuqoZAwnTV7pHWZAaivMiL8aZrDSnY9brE1woa8vz").Value},
       Symbol = "ELF",
       Amount = 1000000000,
       Memo = "transfer in demo"
   };
   var ownerAddress = client.GetAddressFromPrivateKey(PrivateKey);

   // Generate a transfer transaction.
   var transaction = await client.GenerateTransaction(ownerAddress, tokenContractAddress.ToBase58(), methodName, param);
   var txWithSign = client.SignTransaction(PrivateKey, transaction); 

   // Send the transfer transaction to AElf chain node.
   var result = await client.SendTransactionAsync(new SendTransactionInput
   {
       RawTransaction = txWithSign.ToByteArray().ToHex()
   });

   await Task.Delay(4000);
   // After the transaction is mined, query the execution results.
   var transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
   Console.WriteLine(transactionResult.Status);

   // Query account balance.
   var paramGetBalance = new GetBalanceInput
   {
       Symbol = "ELF",
       Owner = new Address {Value = Address.FromBase58(ownerAddress).Value}
   };
   var transactionGetBalance =await client.GenerateTransaction(ownerAddress, tokenContractAddress.ToBase58(), "GetBalance", paramGetBalance);
   var txWithSignGetBalance = client.SignTransaction(PrivateKey, transactionGetBalance);

   var transactionGetBalanceResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
   {
       RawTransaction = txWithSignGetBalance.ToByteArray().ToHex()
   });

   var balance = GetBalanceOutput.Parser.ParseFrom(ByteArrayHelper.HexstringToByteArray(transactionGetBalanceResult));
   Console.WriteLine(balance.Balance);

Web API
-------

*You can see how the Web Api of the node works in
``{chainAddress}/swagger/index.html``* *tip: for an example, my local
address: ‘http://127.0.0.1:1235/swagger/index.html’*

The usage of these methods is based on the AElfClient instance, so if
you don’t have one please create it:

.. code:: c#

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

   -  ``ChainId - string``
   -  ``Branches - Dictionary<string,long>``
   -  ``NotLinkedBlocks - Dictionary<string,string>``
   -  ``LongestChainHeight - long``
   -  ``LongestChainHash - string``
   -  ``GenesisBlockHash - string``
   -  ``GenesisContractAddress - string``
   -  ``LastIrreversibleBlockHash - string``
   -  ``LastIrreversibleBlockHeight - long``
   -  ``BestChainHash - string``
   -  ``BestChainHeight - long``

*Example*

.. code:: c#

   await client.GetChainStatusAsync();

GetContractFileDescriptorSet
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Get the protobuf definitions related to a contract.

*Web API path*

``/api/blockChain/contractFileDescriptorSet``

*Parameters*

-  ``contractAddress - string`` address of a contract

*Returns*

-  ``byte[]``

*Example*

.. code:: c#

   await client.GetContractFileDescriptorSetAsync(address);

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

.. code:: c#

   await client.GetBlockHeightAsync();

GetBlock
~~~~~~~~

Get block information by block hash.

*Web API path*

``/api/blockChain/block``

*Parameters*

-  ``blockHash - string``
-  ``includeTransactions - bool`` :

   -  ``true`` require transaction ids list in the block
   -  ``false`` Doesn’t require transaction ids list in the block

*Returns*

-  ``BlockDto``

   -  ``BlockHash - string``
   -  ``Header - BlockHeaderDto``

      -  ``PreviousBlockHash - string``
      -  ``MerkleTreeRootOfTransactions - string``
      -  ``MerkleTreeRootOfWorldState - string``
      -  ``Extra - string``
      -  ``Height - long``
      -  ``Time - DateTime``
      -  ``ChainId - string``
      -  ``Bloom - string``
      -  ``SignerPubkey - string``

   -  ``Body - BlockBodyDto``

      -  ``TransactionsCount - int``
      -  ``Transactions - List<string>``

*Example*

.. code:: c#

   await client.GetBlockByHashAsync(blockHash);

GetBlockByHeight
~~~~~~~~~~~~~~~~

*Web API path*

``/api/blockChain/blockByHeight``

Get block information by block height.

*Parameters*

-  ``blockHeight - long``
-  ``includeTransactions - bool`` :

   -  ``true`` require transaction ids list in the block
   -  ``false`` Doesn’t require transaction ids list in the block

*Returns*

-  ``BlockDto``

   -  ``BlockHash - string``
   -  ``Header - BlockHeaderDto``

      -  ``PreviousBlockHash - string``
      -  ``MerkleTreeRootOfTransactions - string``
      -  ``MerkleTreeRootOfWorldState - string``
      -  ``Extra - string``
      -  ``Height - long``
      -  ``Time - DateTime``
      -  ``ChainId - string``
      -  ``Bloom - string``
      -  ``SignerPubkey - string``

   -  ``Body - BlockBodyDto``

      -  ``TransactionsCount - int``
      -  ``Transactions - List<string>``

*Example*

.. code:: c#

   await client.GetBlockByHeightAsync(height);

GetTransactionResult
~~~~~~~~~~~~~~~~~~~~

Get the result of a transaction

*Web API path*

``/api/blockChain/transactionResult``

*Parameters*

-  ``transactionId - string``

*Returns*

-  ``TransactionResultDto``

   -  ``TransactionId - string``
   -  ``Status - string``
   -  ``Logs - LogEventDto[]``

      -  ``Address - string``
      -  ``Name - string``
      -  ``Indexed - string[]``
      -  ``NonIndexed - string``

   -  ``Bloom - string``
   -  ``BlockNumber - long``
   -  ``Transaction - TransactionDto``

      -  ``From - string``
      -  ``To - string``
      -  ``RefBlockNumber - long``
      -  ``RefBlockPrefix - string``
      -  ``MethodName - string``
      -  ``Params - string``
      -  ``Signature - string``

   -  ``Error - string``

*Example*

.. code:: c#

   await client.GetTransactionResultAsync(transactionId);

GetTransactionResults
~~~~~~~~~~~~~~~~~~~~~

Get multiple transaction results in a block.

*Web API path*

``/api/blockChain/transactionResults``

*Parameters*

-  ``blockHash - string``
-  ``offset - int``
-  ``limit - int``

*Returns*

-  ``List<TransactionResultDto>`` - The array of transaction result:

   -  the transaction result object

*Example*

.. code:: c#

   await client.GetTransactionResultsAsync(blockHash, 0, 10);

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

.. code:: c#

   await client.GetTransactionPoolStatusAsync();

SendTransaction
~~~~~~~~~~~~~~~

Broadcast a transaction.

*Web API path*

``/api/blockChain/sendTransaction``

*POST*

*Parameters*

-  ``SendTransactionInput`` - Serialization of data into protobuf data:

   -  ``RawTransaction - string``

*Returns*

-  ``SendTransactionOutput``

   -  ``TransactionId`` - string

*Example*

.. code:: c#

   await client.SendTransactionAsync(input);

SendRawTransaction
~~~~~~~~~~~~~~~~~~

Broadcast a transaction.

*Web API path*

``/api/blockChain/sendTransaction``

*POST*

*Parameters*

-  ``SendRawTransactionInput`` - Serialization of data into protobuf
   data:

   -  ``Transaction - string``
   -  ``Signature - string``
   -  ``ReturnTransaction - bool``

*Returns*

-  ``SendRawTransactionOutput``

   -  ``TransactionId - string``
   -  ``Transaction - TransactionDto``

*Example*

.. code:: c#

   await client.SendRawTransactionAsync(input);

SendTransactions
~~~~~~~~~~~~~~~~

Broadcast multiple transactions.

*Web API path*

``/api/blockChain/sendTransactions``

*POST*

*Parameters*

-  ``SendTransactionsInput`` - Serialization of data into protobuf data:

   -  ``RawTransactions - string``

*Returns*

``string[]``

*Example*

.. code:: c#

   await client.SendTransactionsAsync(input);

CreateRawTransaction
~~~~~~~~~~~~~~~~~~~~

Creates an unsigned serialized transaction.

*Web API path*

``/api/blockChain/rawTransaction``

*POST*

*Parameters*

-  ``CreateRawTransactionInput``

   -  ``From - string``
   -  ``To - string``
   -  ``RefBlockNumber - long``
   -  ``RefBlockHash - string``
   -  ``MethodName - string``
   -  ``Params - string``

*Returns*

-  ``CreateRawTransactionOutput``- Serialization of data into protobuf
   data:

   -  ``RawTransactions - string``

*Example*

.. code:: c#

   await client.CreateRawTransactionAsync(input);

ExecuteTransaction
~~~~~~~~~~~~~~~~~~

Call a read-only method on a contract.

*Web API path*

``/api/blockChain/executeTransaction``

*POST*

*Parameters*

-  ``ExecuteTransactionDto`` - Serialization of data into protobuf data:

   -  ``RawTransaction - string``

*Returns*

-  ``string``

*Example*

.. code:: c#

   await client.ExecuteTransactionAsync(input);

ExecuteRawTransaction
~~~~~~~~~~~~~~~~~~~~~

Call a read-only method on a contract.

*Web API path*

``/api/blockChain/executeRawTransaction``

*POST*

*Parameters*

-  ``ExecuteRawTransactionDto`` - Serialization of data into protobuf
   data:

   -  ``RawTransaction - string``
   -  ``Signature - string``

*Returns*

-  ``string``

*Example*

.. code:: c#

   await client.ExecuteRawTransactionAsync(input);

GetPeers
~~~~~~~~

Get peer info about the connected network nodes.

*Web API path*

``/api/net/peers``

*Parameters*

-  ``withMetrics - bool``

*Returns*

-  ``List<PeerDto>``

   -  ``IpAddress - string``
   -  ``ProtocolVersion - int``
   -  ``ConnectionTime - long``
   -  ``ConnectionStatus - string``
   -  ``Inbound - bool``
   -  ``BufferedTransactionsCount - int``
   -  ``BufferedBlocksCount - int``
   -  ``BufferedAnnouncementsCount - int``
   -  ``RequestMetrics - List<RequestMetric>``

      -  ``RoundTripTime - long``
      -  ``MethodName - string``
      -  ``Info - string``
      -  ``RequestTime - string``

*Example*

.. code:: c#

   await client.GetPeersAsync(false);

AddPeer
~~~~~~~

Attempts to add a node to the connected network nodes.

*Web API path*

``/api/net/peer``

*POST*

*Parameters*

-  ``ipAddress - string``

*Returns*

-  ``bool``

*Example*

.. code:: c#

   await client.AddPeerAsync("127.0.0.1:7001");

RemovePeer
~~~~~~~~~~

Attempts to remove a node from the connected network nodes.

*Web API path*

``/api/net/peer``

*DELETE*

*Parameters*

-  ``ipAddress - string``

*Returns*

-  ``bool``

*Example*

.. code:: c#

   await client.RemovePeerAsync("127.0.0.1:7001");

CalculateTransactionFeeAsync
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Estimate transaction fee.

*Web API path*

``/api/blockChain/calculateTransactionFee``

*POST*

*Parameters*

-  ``CalculateTransactionFeeInput`` - The object with the following
   structure :

   -  ``RawTrasaction - String``

*Returns*

-  ``TransactionFeeResultOutput`` - The object with the following
   structure :

   -  ``Success - bool``
   -  ``TransactionFee - Dictionary<string, long>``
   -  ``ResourceFee - Dictionary<string, long>``

*Example*

.. code:: c#

   var input = new CalculateTransactionFeeInput{
       RawTransaction = RawTransaction
   };
   await Client.CalculateTransactionFeeAsync(input);

GetNetworkInfo
~~~~~~~~~~~~~~

Get the network information of the node.

*Web API path*

``/api/net/networkInfo``

*Parameters*

Empty

*Returns*

-  ``NetworkInfoOutput``

   -  ``Version - string``
   -  ``ProtocolVersion - int``
   -  ``Connections - int``

*Example*

.. code:: c#

   await client.GetNetworkInfoAsync();

AElf Client
-----------

IsConnected
~~~~~~~~~~~

Verify whether this sdk successfully connects the chain.

*Parameters*

Empty

*Returns*

-  ``bool``

*Example*

.. code:: c#

   await client.IsConnectedAsync();

GetGenesisContractAddress
~~~~~~~~~~~~~~~~~~~~~~~~~

Get the address of genesis contract.

*Parameters*

Empty

*Returns*

-  ``string``

*Example*

.. code:: c#

   await client.GetGenesisContractAddressAsync();

GetContractAddressByName
~~~~~~~~~~~~~~~~~~~~~~~~

Get address of a contract by given contractNameHash.

*Parameters*

``contractNameHash - Hash``

*Returns*

-  ``Address``

*Example*

.. code:: c#

   await client.GetContractAddressByNameAsync(contractNameHash);

GenerateTransaction
~~~~~~~~~~~~~~~~~~~

Build a transaction from the input parameters.

*Parameters*

-  ``from - string``
-  ``to - string``
-  ``methodName - string``
-  ``input - IMessage``

*Returns*

-  ``Transaction``

*Example*

.. code:: c#

   await client.GenerateTransactionAsync(from, to, methodName, input);

GetFormattedAddress
~~~~~~~~~~~~~~~~~~~

Convert the Address to the displayed
string：symbol_base58-string_base58-string-chain-id.

*Parameters*

-  ``address - Address``

*Returns*

-  ``string``

*Example*

.. code:: c#

   await client.GetFormattedAddressAsync(address);

SignTransaction
~~~~~~~~~~~~~~~

Sign a transaction using private key.

*Parameters*

-  ``privateKeyHex - string``
-  ``transaction - Transaction``

*Returns*

-  ``Transaction``

*Example*

.. code:: c#

   client.SignTransaction(privateKeyHex, transaction);

GetAddressFromPubKey
~~~~~~~~~~~~~~~~~~~~

Get the account address through the public key.

*Parameters*

-  ``pubKey - string``

*Returns*

``string``

*Example*

.. code:: c#

   client.GetAddressFromPubKey(pubKey);

GetAddressFromPrivateKey
~~~~~~~~~~~~~~~~~~~~~~~~

Get the account address through the private key.

*Parameters*

-  ``privateKeyHex - string``

*Returns*

-  ``string``

*Example*

.. code:: c#

   client.GetAddressFromPrivateKey(privateKeyHex);

GenerateKeyPairInfo
~~~~~~~~~~~~~~~~~~~

Generate a new account key pair.

*Parameters*

Empty

*Returns*

-  ``KeyPairInfo``

   -  ``PrivateKey - string``
   -  ``PublicKey - string``
   -  ``Address - string``

*Example*

.. code:: c#

   client.GenerateKeyPairInfo();

Supports
--------

.NET Standard 2.0
