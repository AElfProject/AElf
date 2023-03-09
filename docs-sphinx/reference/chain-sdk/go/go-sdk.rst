aelf-sdk.go - AELF Go API
=========================

This Go library helps in the communication with an AElf node. You can
find out more `here <https://github.com/AElfProject/aelf-sdk.go>`__.

Introduction
------------

aelf-sdk.go is a collection of libraries which allow you to interact
with a local or remote aelf node, using a HTTP connection.

The following documentation will guide you through installing and
running aelf-sdk.go, as well as providing a API reference documentation
with examples.

If you need more information you can check out the repo :
`aelf-sdk.go <https://github.com/AElfProject/aelf-sdk.go>`__

Adding aelf-sdk.go package
--------------------------

First you need to get aelf-sdk.go:

::

   > go get -u github.com/AElfProject/aelf-sdk.go

Examples
--------

Create instance
~~~~~~~~~~~~~~~

Create a new instance of AElfClient, and set url of an AElf chain node.

.. code:: go

   import ("github.com/AElfProject/aelf-sdk.go/client")

   var aelf = client.AElfClient{
       Host:       "http://127.0.0.1:8000",
       Version:    "1.0",
       PrivateKey: "cd86ab6347d8e52bbbe8532141fc59ce596268143a308d1d40fedf385528b458",
   }

Initiate a transfer transaction
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. code:: go

   // Get token contract address.
   tokenContractAddress, _ := aelf.GetContractAddressByName("AElf.ContractNames.Token")
   fromAddress := aelf.GetAddressFromPrivateKey(aelf.PrivateKey)
   methodName := "Transfer"
   toAddress, _ := util.Base58StringToAddress("7s4XoUHfPuqoZAwnTV7pHWZAaivMiL8aZrDSnY9brE1woa8vz")

   params := &pb.TransferInput{
       To:     toAddress,
       Symbol: "ELF",
       Amount: 1000000000,
       Memo:   "transfer in demo",
   }
   paramsByte, _ := proto.Marshal(params)

   // Generate a transfer transaction.
   transaction, _ := aelf.CreateTransaction(fromAddress, tokenContractAddress, methodName, paramsByte)
   signature, _ := aelf.SignTransaction(aelf.PrivateKey, transaction)
   transaction.Signature = signature

   // Send the transfer transaction to AElf chain node.
   transactionByets, _ := proto.Marshal(transaction)
   sendResult, _ := aelf.SendTransaction(hex.EncodeToString(transactionByets))

   time.Sleep(time.Duration(4) * time.Second)
   transactionResult, _ := aelf.GetTransactionResult(sendResult.TransactionID)
   fmt.Println(transactionResult)

   // Query account balance.
   ownerAddress, _ := util.Base58StringToAddress(fromAddress)
   getBalanceInput := &pb.GetBalanceInput{
       Symbol: "ELF",
       Owner:  ownerAddress,
   }
   getBalanceInputByte, _ := proto.Marshal(getBalanceInput)

   getBalanceTransaction, _ := aelf.CreateTransaction(fromAddress, tokenContractAddress, "GetBalance", getBalanceInputByte)
   getBalanceTransaction.Params = getBalanceInputByte
   getBalanceSignature, _ := aelf.SignTransaction(aelf.PrivateKey, getBalanceTransaction)
   getBalanceTransaction.Signature = getBalanceSignature

   getBalanceTransactionByets, _ := proto.Marshal(getBalanceTransaction)
   getBalanceResult, _ := aelf.ExecuteTransaction(hex.EncodeToString(getBalanceTransactionByets))
   balance := &pb.GetBalanceOutput{}
   getBalanceResultBytes, _ := hex.DecodeString(getBalanceResult)
   proto.Unmarshal(getBalanceResultBytes, balance)
   fmt.Println(balance)

Web API
-------

*You can see how the Web Api of the node works in
``{chainAddress}/swagger/index.html``* *tip: for an example, my local
address: ‘http://127.0.0.1:1235/swagger/index.html’*

The usage of these methods is based on the AElfClient instance, so if
you don’t have one please create it:

.. code:: go

   import ("github.com/AElfProject/aelf-sdk.go/client")

   var aelf = client.AElfClient{
       Host:       "http://127.0.0.1:8000",
       Version:    "1.0",
       PrivateKey: "680afd630d82ae5c97942c4141d60b8a9fedfa5b2864fca84072c17ee1f72d9d",
   }

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
   -  ``Branches - map[string]interface{}``
   -  ``NotLinkedBlocks - map[string]interface{}``
   -  ``LongestChainHeight - int64``
   -  ``LongestChainHash - string``
   -  ``GenesisBlockHash - string``
   -  ``GenesisContractAddress - string``
   -  ``LastIrreversibleBlockHash - string``
   -  ``LastIrreversibleBlockHeight - int64``
   -  ``BestChainHash - string``
   -  ``BestChainHeight - int64``

*Example*

.. code:: go

   chainStatus, err := aelf.GetChainStatus()

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

.. code:: go

   contractFile, err := aelf.GetContractFileDescriptorSet("pykr77ft9UUKJZLVq15wCH8PinBSjVRQ12sD1Ayq92mKFsJ1i")

GetBlockHeight
~~~~~~~~~~~~~~

Get current best height of the chain.

*Web API path*

``/api/blockChain/blockHeight``

*Parameters*

Empty

*Returns*

-  ``float64``

*Example*

.. code:: go

   height, err := aelf.GetBlockHeight()

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
      -  ``Height - int64``
      -  ``Time - string``
      -  ``ChainId - string``
      -  ``Bloom - string``
      -  ``SignerPubkey - string``

   -  ``Body - BlockBodyDto``

      -  ``TransactionsCount - int``
      -  ``Transactions - []string``

*Example*

.. code:: go

   block, err := aelf.GetBlockByHash(blockHash, true)

GetBlockByHeight
~~~~~~~~~~~~~~~~

*Web API path*

``/api/blockChain/blockByHeight``

Get block information by block height.

*Parameters*

-  ``blockHeight - int64``
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
      -  ``Height - int64``
      -  ``Time - string``
      -  ``ChainId - string``
      -  ``Bloom - string``
      -  ``SignerPubkey - string``

   -  ``Body - BlockBodyDto``

      -  ``TransactionsCount - int``
      -  ``Transactions - []string``

*Example*

.. code:: go

   block, err := aelf.GetBlockByHeight(100, true)

GetTransactionResult
~~~~~~~~~~~~~~~~~~~~

Get the result of a transaction.

*Web API path*

``/api/blockChain/transactionResult``

*Parameters*

-  ``transactionId - string``

*Returns*

-  ``TransactionResultDto``

   -  ``TransactionId - string``
   -  ``Status - string``
   -  ``Logs - []LogEventDto``

      -  ``Address - string``
      -  ``Name - string``
      -  ``Indexed - []string``
      -  ``NonIndexed - string``

   -  ``Bloom - string``
   -  ``BlockNumber - int64``
   -  ``BlockHash - string``
   -  ``Transaction - TransactionDto``

      -  ``From - string``
      -  ``To - string``
      -  ``RefBlockNumber - int64``
      -  ``RefBlockPrefix - string``
      -  ``MethodName - string``
      -  ``Params - string``
      -  ``Signature - string``

   -  ``ReturnValue - string``
   -  ``Error - string``

*Example*

.. code:: go

   transactionResult, err := aelf.GetTransactionResult(transactionID)

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

-  ``[]TransactionResultDto`` - The array of transaction result:

   -  the transaction result object

*Example*

.. code:: go

   transactionResults, err := aelf.GetTransactionResults(blockHash, 0, 10)

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

.. code:: go

   poolStatus, err := aelf.GetTransactionPoolStatus()

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

   -  ``TransactionId - string``

*Example*

.. code:: go

   sendResult, err := aelf.SendTransaction(input)

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

.. code:: go

   sendRawResult, err := aelf.SendRawTransaction(input)

SendTransactions
~~~~~~~~~~~~~~~~

Broadcast multiple transactions.

*Web API path*

``/api/blockChain/sendTransactions``

*POST*

*Parameters*

-  ``rawTransactions - string`` - Serialization of data into protobuf
   data:

*Returns*

-  ``[]interface{}``

*Example*

.. code:: go

   results, err := aelf.SendTransactions(transactions)

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
   -  ``RefBlockNumber - int64``
   -  ``RefBlockHash - string``
   -  ``MethodName - string``
   -  ``Params - string``

*Returns*

-  ``CreateRawTransactionOutput``- Serialization of data into protobuf
   data:

   -  ``RawTransactions - string``

*Example*

.. code:: go

   result, err := aelf.CreateRawTransaction(input)

ExecuteTransaction
~~~~~~~~~~~~~~~~~~

Call a read-only method on a contract.

*Web API path*

``/api/blockChain/executeTransaction``

*POST*

*Parameters*

-  ``rawTransaction - string``

*Returns*

-  ``string``

*Example*

.. code:: go

   executeresult, err := aelf.ExecuteTransaction(rawTransaction)

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

.. code:: go

   executeRawresult, err := aelf.ExecuteRawTransaction(executeRawinput)

GetPeers
~~~~~~~~

Get peer info about the connected network nodes.

*Web API path*

``/api/net/peers``

*Parameters*

-  ``withMetrics - bool``

*Returns*

-  ``[]PeerDto``

   -  ``IpAddress - string``
   -  ``ProtocolVersion - int``
   -  ``ConnectionTime - int64``
   -  ``ConnectionStatus - string``
   -  ``Inbound - bool``
   -  ``BufferedTransactionsCount - int``
   -  ``BufferedBlocksCount - int``
   -  ``BufferedAnnouncementsCount - int``
   -  ``RequestMetrics - []RequestMetric``

      -  ``RoundTripTime - int64``
      -  ``MethodName - string``
      -  ``Info - string``
      -  ``RequestTime - string``

*Example*

.. code:: go

   peers, err := aelf.GetPeers(false);

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

.. code:: go

   addResult, err := aelf.AddPeer("127.0.0.1:7001");

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

.. code:: go

   removeResult, err := aelf.RemovePeer("127.0.0.1:7001");

CalculateTransactionFee
~~~~~~~~~~~~~~~~~~~~~~~

Estimate transaction fee.

*Web API path*

``/api/blockChain/calculateTransactionFee``

*POST*

*Parameters*

-  ``CalculateTransactionFeeInput - object`` - The object with the
   following structure :

   -  ``RawTrasaction - string``

*Returns*

-  ``TransactionFeeResultOutput - object`` - The object with the
   following structure :

   -  ``Success - bool``
   -  ``TransactionFee - map[string]interface{}``
   -  ``ResourceFee - map[string]interface{}``

*Example*

.. code:: go

   calculateTransactionFee, err := aelf.CalculateTransactionFee(transactionFeeInput)

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

.. code:: go

   networkInfo, err := aelf.GetNetworkInfo()

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

.. code:: go

   isConnected := aelf.IsConnected()

GetGenesisContractAddress
~~~~~~~~~~~~~~~~~~~~~~~~~

Get the address of genesis contract.

*Parameters*

Empty

*Returns*

-  ``string``

*Example*

.. code:: go

   contractAddress, err := aelf.GetGenesisContractAddress()

GetContractAddressByName
~~~~~~~~~~~~~~~~~~~~~~~~

Get address of a contract by given contractNameHash.

*Parameters*

-  ``contractNameHash - string``

*Returns*

-  ``Address``

*Example*

.. code:: go

   contractAddress, err := aelf.GetContractAddressByName("AElf.ContractNames.Token")

CreateTransaction
~~~~~~~~~~~~~~~~~

Build a transaction from the input parameters.

*Parameters*

-  ``from - string``
-  ``to - string``
-  ``methodName - string``
-  ``params - []byte``

*Returns*

``Transaction``

*Example*

.. code:: go

   transaction, err := aelf.CreateTransaction(fromAddress, toAddress, methodName, param)

GetFormattedAddress
~~~~~~~~~~~~~~~~~~~

Convert the Address to the displayed
string：symbol_base58-string_base58-string-chain-id.

*Parameters*

-  ``address - string``

*Returns*

-  ``string``

*Example*

.. code:: go

   formattedAddress, err := aelf.GetFormattedAddress(address);

SignTransaction
~~~~~~~~~~~~~~~

Sign a transaction using private key.

*Parameters*

-  ``privateKey - string``
-  ``transaction - Transaction``

*Returns*

-  ``[]byte``

*Example*

.. code:: go

   signature, err := aelf.SignTransaction(privateKey, transaction)

GetAddressFromPubKey
~~~~~~~~~~~~~~~~~~~~

Get the account address through the public key.

*Parameters*

-  ``pubKey - string``

*Returns*

-  ``string``

*Example*

.. code:: go

   address := aelf.GetAddressFromPubKey(pubKey);

GetAddressFromPrivateKey
~~~~~~~~~~~~~~~~~~~~~~~~

Get the account address through the private key.

*Parameters*

-  ``privateKey - string``

*Returns*

-  ``string``

*Example*

.. code:: go

   address := aelf.GetAddressFromPrivateKey(privateKey)

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

.. code:: go

   keyPair := aelf.GenerateKeyPairInfo()

Supports
--------

Go 1.13
