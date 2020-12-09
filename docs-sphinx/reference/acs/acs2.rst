ACS2 - Parallel Execution Standard
==================================

ACS2 is used to provide information for parallel execution of
transactions.

Interface
---------

A contract that inherits ACS2 only needs to implement one method:

Methods
~~~~~~~

+-------------------+--------------------------------------------+----------------------------------------------+----------------------------------------------------------------------------+
| Method Name       | Request Type                               | Response Type                                | Description                                                                |
+===================+============================================+==============================================+============================================================================+
| GetResourceInfo   | `aelf.Transaction <#aelf.Transaction>`__   | `acs2.ResourceInfo <#acs2.ResourceInfo>`__   | Gets the resource information that the transaction execution depends on.   |
+-------------------+--------------------------------------------+----------------------------------------------+----------------------------------------------------------------------------+

Types
~~~~~

.. raw:: html

   <div id="acs2.ResourceInfo">

.. raw:: html

   </div>

acs2.ResourceInfo
^^^^^^^^^^^^^^^^^

+-----------------------+----------------------------------------------------+--------------------------------------------------------+------------+
| Field                 | Type                                               | Description                                            | Label      |
+=======================+====================================================+========================================================+============+
| write\_paths          | `aelf.ScopedStatePath <#aelf.ScopedStatePath>`__   | The state path that depends on when writing.           | repeated   |
+-----------------------+----------------------------------------------------+--------------------------------------------------------+------------+
| read\_paths           | `aelf.ScopedStatePath <#aelf.ScopedStatePath>`__   | The state path that depends on when reading.           | repeated   |
+-----------------------+----------------------------------------------------+--------------------------------------------------------+------------+
| non\_parallelizable   | `bool <#bool>`__                                   | Whether the transaction is not executed in parallel.   |            |
+-----------------------+----------------------------------------------------+--------------------------------------------------------+------------+

.. raw:: html

   <div id="aelf.Address">

.. raw:: html

   </div>

aelf.Address
^^^^^^^^^^^^

+---------+----------------------+---------------+---------+
| Field   | Type                 | Description   | Label   |
+=========+======================+===============+=========+
| value   | `bytes <#bytes>`__   |               |         |
+---------+----------------------+---------------+---------+

.. raw:: html

   <div id="aelf.BinaryMerkleTree">

.. raw:: html

   </div>

aelf.BinaryMerkleTree
^^^^^^^^^^^^^^^^^^^^^

+---------------+-------------------------+---------------------------+------------+
| Field         | Type                    | Description               | Label      |
+===============+=========================+===========================+============+
| nodes         | `Hash <#aelf.Hash>`__   | The leaf nodes.           | repeated   |
+---------------+-------------------------+---------------------------+------------+
| root          | `Hash <#aelf.Hash>`__   | The root node hash.       |            |
+---------------+-------------------------+---------------------------+------------+
| leaf\_count   | `int32 <#int32>`__      | The count of leaf node.   |            |
+---------------+-------------------------+---------------------------+------------+

.. raw:: html

   <div id="aelf.Hash">

.. raw:: html

   </div>

aelf.Hash
^^^^^^^^^

+---------+----------------------+---------------+---------+
| Field   | Type                 | Description   | Label   |
+=========+======================+===============+=========+
| value   | `bytes <#bytes>`__   |               |         |
+---------+----------------------+---------------+---------+

.. raw:: html

   <div id="aelf.LogEvent">

.. raw:: html

   </div>

aelf.LogEvent
^^^^^^^^^^^^^

+----------------+-------------------------------+----------------------------------------------+------------+
| Field          | Type                          | Description                                  | Label      |
+================+===============================+==============================================+============+
| address        | `Address <#aelf.Address>`__   | The contract address.                        |            |
+----------------+-------------------------------+----------------------------------------------+------------+
| name           | `string <#string>`__          | The name of the log event.                   |            |
+----------------+-------------------------------+----------------------------------------------+------------+
| indexed        | `bytes <#bytes>`__            | The indexed data, used to calculate bloom.   | repeated   |
+----------------+-------------------------------+----------------------------------------------+------------+
| non\_indexed   | `bytes <#bytes>`__            | The non indexed data.                        |            |
+----------------+-------------------------------+----------------------------------------------+------------+

.. raw:: html

   <div id="aelf.MerklePath">

.. raw:: html

   </div>

aelf.MerklePath
^^^^^^^^^^^^^^^

+-----------------------+---------------------------------------------+--------------------------+------------+
| Field                 | Type                                        | Description              | Label      |
+=======================+=============================================+==========================+============+
| merkle\_path\_nodes   | `MerklePathNode <#aelf.MerklePathNode>`__   | The merkle path nodes.   | repeated   |
+-----------------------+---------------------------------------------+--------------------------+------------+

.. raw:: html

   <div id="aelf.MerklePathNode">

.. raw:: html

   </div>

aelf.MerklePathNode
^^^^^^^^^^^^^^^^^^^

+-------------------------+-------------------------+------------------------------------+---------+
| Field                   | Type                    | Description                        | Label   |
+=========================+=========================+====================================+=========+
| hash                    | `Hash <#aelf.Hash>`__   | The node hash.                     |         |
+-------------------------+-------------------------+------------------------------------+---------+
| is\_left\_child\_node   | `bool <#bool>`__        | Whether it is a left child node.   |         |
+-------------------------+-------------------------+------------------------------------+---------+

.. raw:: html

   <div id="aelf.SInt32Value">

.. raw:: html

   </div>

aelf.SInt32Value
^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| value   | `sint32 <#sint32>`__   |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="aelf.SInt64Value">

.. raw:: html

   </div>

aelf.SInt64Value
^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| value   | `sint64 <#sint64>`__   |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="aelf.ScopedStatePath">

.. raw:: html

   </div>

aelf.ScopedStatePath
^^^^^^^^^^^^^^^^^^^^

+-----------+-----------------------------------+----------------------------------------------------------+---------+
| Field     | Type                              | Description                                              | Label   |
+===========+===================================+==========================================================+=========+
| address   | `Address <#aelf.Address>`__       | The scope address, which will be the contract address.   |         |
+-----------+-----------------------------------+----------------------------------------------------------+---------+
| path      | `StatePath <#aelf.StatePath>`__   | The path of contract state.                              |         |
+-----------+-----------------------------------+----------------------------------------------------------+---------+

.. raw:: html

   <div id="aelf.SmartContractRegistration">

.. raw:: html

   </div>

aelf.SmartContractRegistration
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+------------------------+-------------------------+-----------------------------------------+---------+
| Field                  | Type                    | Description                             | Label   |
+========================+=========================+=========================================+=========+
| category               | `sint32 <#sint32>`__    | The category of contract code(0: C#).   |         |
+------------------------+-------------------------+-----------------------------------------+---------+
| code                   | `bytes <#bytes>`__      | The byte array of the contract code.    |         |
+------------------------+-------------------------+-----------------------------------------+---------+
| code\_hash             | `Hash <#aelf.Hash>`__   | The hash of the contract code.          |         |
+------------------------+-------------------------+-----------------------------------------+---------+
| is\_system\_contract   | `bool <#bool>`__        | Whether it is a system contract.        |         |
+------------------------+-------------------------+-----------------------------------------+---------+
| version                | `int32 <#int32>`__      | The version of the current contract.    |         |
+------------------------+-------------------------+-----------------------------------------+---------+

.. raw:: html

   <div id="aelf.StatePath">

.. raw:: html

   </div>

aelf.StatePath
^^^^^^^^^^^^^^

+---------+------------------------+---------------------------------------+------------+
| Field   | Type                   | Description                           | Label      |
+=========+========================+=======================================+============+
| parts   | `string <#string>`__   | The partial path of the state path.   | repeated   |
+---------+------------------------+---------------------------------------+------------+

.. raw:: html

   <div id="aelf.Transaction">

.. raw:: html

   </div>

aelf.Transaction
^^^^^^^^^^^^^^^^

+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| Field                | Type                          | Description                                                                                                                                                                                        | Label   |
+======================+===============================+====================================================================================================================================================================================================+=========+
| from                 | `Address <#aelf.Address>`__   | The address of the sender of the transaction.                                                                                                                                                      |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| to                   | `Address <#aelf.Address>`__   | The address of the contract when calling a contract.                                                                                                                                               |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| ref\_block\_number   | `int64 <#int64>`__            | The height of the referenced block hash.                                                                                                                                                           |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| ref\_block\_prefix   | `bytes <#bytes>`__            | The first four bytes of the referenced block hash.                                                                                                                                                 |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| method\_name         | `string <#string>`__          | The name of a method in the smart contract at the To address.                                                                                                                                      |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| params               | `bytes <#bytes>`__            | The parameters to pass to the smart contract method.                                                                                                                                               |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+
| signature            | `bytes <#bytes>`__            | When signing a transaction it’s actually a subset of the fields: from/to and the target method as well as the parameter that were given. It also contains the reference block number and prefix.   |         |
+----------------------+-------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+---------+

.. raw:: html

   <div id="aelf.TransactionExecutingStateSet">

.. raw:: html

   </div>

aelf.TransactionExecutingStateSet
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+-----------+---------------------------------------------------------------------------------------------------+-----------------------+------------+
| Field     | Type                                                                                              | Description           | Label      |
+===========+===================================================================================================+=======================+============+
| writes    | `TransactionExecutingStateSet.WritesEntry <#aelf.TransactionExecutingStateSet.WritesEntry>`__     | The changed states.   | repeated   |
+-----------+---------------------------------------------------------------------------------------------------+-----------------------+------------+
| reads     | `TransactionExecutingStateSet.ReadsEntry <#aelf.TransactionExecutingStateSet.ReadsEntry>`__       | The read states.      | repeated   |
+-----------+---------------------------------------------------------------------------------------------------+-----------------------+------------+
| deletes   | `TransactionExecutingStateSet.DeletesEntry <#aelf.TransactionExecutingStateSet.DeletesEntry>`__   | The deleted states.   | repeated   |
+-----------+---------------------------------------------------------------------------------------------------+-----------------------+------------+

.. raw:: html

   <div id="aelf.TransactionExecutingStateSet.DeletesEntry">

.. raw:: html

   </div>

aelf.TransactionExecutingStateSet.DeletesEntry
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| key     | `string <#string>`__   |               |         |
+---------+------------------------+---------------+---------+
| value   | `bool <#bool>`__       |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="aelf.TransactionExecutingStateSet.ReadsEntry">

.. raw:: html

   </div>

aelf.TransactionExecutingStateSet.ReadsEntry
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| key     | `string <#string>`__   |               |         |
+---------+------------------------+---------------+---------+
| value   | `bool <#bool>`__       |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="aelf.TransactionExecutingStateSet.WritesEntry">

.. raw:: html

   </div>

aelf.TransactionExecutingStateSet.WritesEntry
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+---------+------------------------+---------------+---------+
| Field   | Type                   | Description   | Label   |
+=========+========================+===============+=========+
| key     | `string <#string>`__   |               |         |
+---------+------------------------+---------------+---------+
| value   | `bytes <#bytes>`__     |               |         |
+---------+------------------------+---------------+---------+

.. raw:: html

   <div id="aelf.TransactionResult">

.. raw:: html

   </div>

aelf.TransactionResult
^^^^^^^^^^^^^^^^^^^^^^

+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| Field             | Type                                                          | Description                                                                                                                                                                                                                                                                | Label      |
+===================+===============================================================+============================================================================================================================================================================================================================================================================+============+
| transaction\_id   | `Hash <#aelf.Hash>`__                                         | The transaction id.                                                                                                                                                                                                                                                        |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| status            | `TransactionResultStatus <#aelf.TransactionResultStatus>`__   | The transaction result status.                                                                                                                                                                                                                                             |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| logs              | `LogEvent <#aelf.LogEvent>`__                                 | The log events.                                                                                                                                                                                                                                                            | repeated   |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| bloom             | `bytes <#bytes>`__                                            | Bloom filter for transaction logs. A transaction log event can be defined in the contract and stored in the bloom filter after the transaction is executed. Through this filter, we can quickly search for and determine whether a log exists in the transaction result.   |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| return\_value     | `bytes <#bytes>`__                                            | The return value of the transaction execution.                                                                                                                                                                                                                             |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| block\_number     | `int64 <#int64>`__                                            | The height of the block hat packages the transaction.                                                                                                                                                                                                                      |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| block\_hash       | `Hash <#aelf.Hash>`__                                         | The hash of the block hat packages the transaction.                                                                                                                                                                                                                        |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+
| error             | `string <#string>`__                                          | Failed execution error message.                                                                                                                                                                                                                                            |            |
+-------------------+---------------------------------------------------------------+----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+

.. raw:: html

   <div id="aelf.TransactionResultStatus">

.. raw:: html

   </div>

aelf.TransactionResultStatus
^^^^^^^^^^^^^^^^^^^^^^^^^^^^

+----------------------------+----------+-------------------------------------------------------------------------------------+
| Name                       | Number   | Description                                                                         |
+============================+==========+=====================================================================================+
| NOT\_EXISTED               | 0        | The execution result of the transaction does not exist.                             |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| PENDING                    | 1        | The transaction is in the transaction pool waiting to be packaged.                  |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| FAILED                     | 2        | Transaction execution failed.                                                       |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| MINED                      | 3        | The transaction was successfully executed and successfully packaged into a block.   |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| CONFLICT                   | 4        | When executed in parallel, there are conflicts with other transactions.             |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| PENDING\_VALIDATION        | 5        | The transaction is waiting for validation.                                          |
+----------------------------+----------+-------------------------------------------------------------------------------------+
| NODE\_VALIDATION\_FAILED   | 6        | Transaction validation failed.                                                      |
+----------------------------+----------+-------------------------------------------------------------------------------------+


Usage
-----

AElf uses the key-value database to store data. For the data generated
during the contract execution, a mechanism called **State Path** is used
to determine the key of the data.

For example ``Token contract`` defines a property,

.. code:: c#

    public MappedState<Address, string, long> Balances { get; set; }

it can be used to access, modify balance.

Assuming that the address of the ``Token contract`` is
**Nmjj7noTpMqZ522j76SDsFLhiKkThv1u3d4TxqJMD8v89tWmE**. If you want to
know the balance of the address
**2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz**, you can directly
use this key to access redis / ssdb to get its value.

.. code:: text

   Nmjj7noTpMqZ522j76SDsFLhiKkThv1u3d4TxqJMD8v89tWmE/Balances/2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz/ELF

On AElf, the implementation of parallel transaction execution is also
based on the key , developers need to provide a method may access to the
``StatePath``, then the corresponding transactions will be properly
grouped before executing: if the two methods do not access the same
StatePath, then you can safely place them in different groups.

Attention: The transaction will be canceled and labeled to “can not be
groupped” when the StatePath mismatchs the method.

If you are interested in the logic, you can view the code
ITransactionGrouper, as well as IParallelTransactionExecutingService .

Implementation
--------------

Token contract, as an example, the core logic of method ``Transfer`` is
to modify the balance of address. It accesses the balances property
mentioned above twice.

At this point, we need to notify ``ITransactionGrouper`` via the
``GetResourceInfo`` method of the key of the ELF balance of address A
and address B:

.. code:: c#

   var args = TransferInput.Parser.ParseFrom(txn.Params);
   var resourceInfo = new ResourceInfo
   {
       Paths =
       {
           GetPath(nameof(TokenContractState.Balances), txn.From.ToString(), args.Symbol),
           GetPath(nameof(TokenContractState.Balances), args.To.ToString(), args.Symbol),
       }
   };
   return resourceInfo;

The ``GetPath`` forms a ``ScopedStatePath`` from several pieces of data
that make up the key:

.. code:: c#

   private ScopedStatePath GetPath(params string[] parts)
   {
       return new ScopedStatePath
       {
           Address = Context.Self,
           Path = new StatePath
           {
               Parts =
               {
                   parts
               }
           }
       }
   }

Test
----

You can construct two transactions, and the transactions are passed
directly to an implementation instance of ``ITransactionGrouper``, and
the ``GroupAsync`` method is used to see whether the two transactions
are parallel.

We prepare two stubs that implement the ACS2 contract with different
addresses to simulate the Transfer:

.. code:: c#

   var keyPair1 = SampleECKeyPairs.KeyPairs[0];
   var acs2DemoContractStub1 = GetACS2DemoContractStub(keyPair1);
   var keyPair2 = SampleECKeyPairs.KeyPairs[1];
   var acs2DemoContractStub2 = GetACS2DemoContractStub(keyPair2);

Then take out some services and data needed for testing from
Application:

.. code:: c#

   var transactionGrouper = Application.ServiceProvider.GetRequiredService<ITransactionGrouper>();
   var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
   var chain = await blockchainService.GetChainAsync();

Finally, check it via transactionGrouper:

.. code:: c#

   // Situation can be parallel executed.
   {
       var groupedTransactions = await transactionGrouper.GroupAsync(new ChainContext
       {
           BlockHash = chain.BestChainHash,
           BlockHeight = chain.BestChainHeight
       }, new List<Transaction>
       {
           acs2DemoContractStub1.TransferCredits.GetTransaction(new TransferCreditsInput
           {
               To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[2].PublicKey),
               Symbol = "ELF",
               Amount = 1
           }),
           acs2DemoContractStub2.TransferCredits.GetTransaction(new TransferCreditsInput
           {
               To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[3].PublicKey),
               Symbol = "ELF",
               Amount = 1
           }),
       });
       groupedTransactions.Parallelizables.Count.ShouldBe(2);
   }
   // Situation cannot.
   {
       var groupedTransactions = await transactionGrouper.GroupAsync(new ChainContext
       {
           BlockHash = chain.BestChainHash,
           BlockHeight = chain.BestChainHeight
       }, new List<Transaction>
       {
           acs2DemoContractStub1.TransferCredits.GetTransaction(new TransferCreditsInput
           {
               To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[2].PublicKey),
               Symbol = "ELF",
               Amount = 1
           }),
           acs2DemoContractStub2.TransferCredits.GetTransaction(new TransferCreditsInput
           {
               To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[2].PublicKey),
               Symbol = "ELF",
               Amount = 1
           }),
       });
       groupedTransactions.Parallelizables.Count.ShouldBe(1);
   }

Example
-------

You can refer to the implementation of the ``MultiToken contract`` for
``GetResourceInfo``. Noting that for the ``ResourceInfo`` provided by
the method ``Transfer``, you need to consider charging a transaction fee
in addition to the two keys mentioned in this article.
