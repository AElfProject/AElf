AELF API 1.0
============

Chain API
---------

Get information about a given block by block hash. Optionally with the list of its transactions.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/blockChain/block

Parameters
^^^^^^^^^^

+-------------+----------------------------+-------------+-------------+-------------+
| Type        | Name                       | Description | Schema      | Default     |
+=============+============================+=============+=============+=============+
| **Query**   | **blockHash**              | block hash  | string      |             |
|             |                            |             |             |             |
|             | *optional*                 |             |             |             |
+-------------+---------------+------------+-------------+-------------+-------------+
| **Query**   | **include Transactions**   | include     | boolean     | ``"false"`` |
|             |                            | transactions|             |             |
|             | *optional*                 | or not      |             |             |
+-------------+----------------------------+-------------+-------------+-------------+

Responses
^^^^^^^^^

========= =========== ========================
HTTP Code Description Schema
========= =========== ========================
**200**   Success     `BlockDto <#blockdto>`__
========= =========== ========================

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

Tags
^^^^

-  BlockChain

Get information about a given block by block height. Optionally with the list of its transactions.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/blockChain/blockByHeight

.. _parameters-1:

Parameters
^^^^^^^^^^

+-------------+--------------------------+-------------+-------------+-------------+
| Type        | Name                     | Description | Schema      | Default     |
+=============+==========================+=============+=============+=============+
| **Query**   | **blockHeight**          | block       | integer     |             |
|             |                          | height      | (int64)     |             |
|             |  *optional*              |             |             |             |
+-------------+--------------------------+-------------+-------------+-------------+
| **Query**   | **include Transactions** | include     | boolean     | ``"false"`` |
|             |                          | transactions|             |             |
|             |  *optional*              | or not      |             |             |
+-------------+--------------------------+-------------+-------------+-------------+

.. _responses-1:

Responses
^^^^^^^^^

========= =========== ========================
HTTP Code Description Schema
========= =========== ========================
**200**   Success     `BlockDto <#blockdto>`__
========= =========== ========================

.. _produces-1:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-1:

Tags
^^^^

-  BlockChain

Get the height of the current chain.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/blockChain/blockHeight

.. _responses-2:

Responses
^^^^^^^^^

========= =========== ===============
HTTP Code Description Schema
========= =========== ===============
**200**   Success     integer (int64)
========= =========== ===============

.. _produces-2:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-2:

Tags
^^^^

-  BlockChain

Get the current state about a given block
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/blockChain/blockState

.. _parameters-2:

Parameters
^^^^^^^^^^

========= ========================== =========== ======
Type      Name                       Description Schema
========= ========================== =========== ======
**Query** **blockHash** \ *optional* block hash  string
========= ========================== =========== ======

.. _responses-3:

Responses
^^^^^^^^^

========= =========== ==================================
HTTP Code Description Schema
========= =========== ==================================
**200**   Success     `BlockStateDto <#blockstatedto>`__
========= =========== ==================================

.. _produces-3:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-3:

Tags
^^^^

-  BlockChain

Get the current status of the block chain.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/blockChain/chainStatus

.. _responses-4:

Responses
^^^^^^^^^

========= =========== ====================================
HTTP Code Description Schema
========= =========== ====================================
**200**   Success     `ChainStatusDto <#chainstatusdto>`__
========= =========== ====================================

.. _produces-4:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-4:

Tags
^^^^

-  BlockChain

Get the protobuf definitions related to a contract
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/blockChain/contractFileDescriptorSet

.. _parameters-3:

Parameters
^^^^^^^^^^

========= ======================== ================ ======
Type      Name                     Description      Schema
========= ======================== ================ ======
**Query** **address** \ *optional* contract address string
========= ======================== ================ ======

.. _responses-5:

Responses
^^^^^^^^^

========= =========== =============
HTTP Code Description Schema
========= =========== =============
**200**   Success     string (byte)
========= =========== =============

.. _produces-5:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-5:

Tags
^^^^

-  BlockChain

POST /api/blockChain/executeRawTransaction
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. _parameters-4:

Parameters
^^^^^^^^^^

+----------+------------------------+----------------------------------------------------------+
| Type     | Name                   | Schema                                                   |
+==========+========================+==========================================================+
| **Body** | **input** \ *optional* | `ExecuteRawTransactionDto <#executerawtransactiondto>`__ |
+----------+------------------------+----------------------------------------------------------+

.. _responses-6:

Responses
^^^^^^^^^

========= =========== ======
HTTP Code Description Schema
========= =========== ======
**200**   Success     string
========= =========== ======

Consumes
^^^^^^^^

-  ``application/json-patch+json; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/*+json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _produces-6:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-6:

Tags
^^^^

-  BlockChain

Call a read-only method on a contract.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   POST /api/blockChain/executeTransaction

.. _parameters-5:

Parameters
^^^^^^^^^^

+----------+------------------------+------------------------------------------------------+
| Type     | Name                   | Schema                                               |
+==========+========================+======================================================+
| **Body** | **input** \ *optional* | `ExecuteTransactionDto <#executetransactiondto>`__   |
+----------+------------------------+------------------------------------------------------+

.. _responses-7:

Responses
^^^^^^^^^

========= =========== ======
HTTP Code Description Schema
========= =========== ======
**200**   Success     string
========= =========== ======

.. _consumes-1:

Consumes
^^^^^^^^

-  ``application/json-patch+json; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/*+json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _produces-7:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-7:

Tags
^^^^

-  BlockChain

Get the merkle path of a transaction.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/blockChain/merklePathByTransactionId

.. _parameters-6:

Parameters
^^^^^^^^^^

========= ============================== ======
Type      Name                           Schema
========= ============================== ======
**Query** **transactionId** \ *optional* string
========= ============================== ======

.. _responses-8:

Responses
^^^^^^^^^

========= =========== ==================================
HTTP Code Description Schema
========= =========== ==================================
**200**   Success     `MerklePathDto <#merklepathdto>`__
========= =========== ==================================

.. _produces-8:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-8:

Tags
^^^^

-  BlockChain

Creates an unsigned serialized transaction
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   POST /api/blockChain/rawTransaction

.. _parameters-7:

Parameters
^^^^^^^^^^

+----------+------------------------+------------------------------------------------------------+
| Type     | Name                   | Schema                                                     |
+==========+========================+============================================================+
| **Body** | **input** \ *optional* | `CreateRawTransactionInput <#createrawtransactioninput>`__ |
+----------+------------------------+------------------------------------------------------------+

.. _responses-9:

Responses
^^^^^^^^^

+-----------+-------------+--------------------------------------------------------------+
| HTTP Code | Description | Schema                                                       |
+===========+=============+==============================================================+
| **200**   | Success     | `CreateRawTransactionOutput <#createrawtransactionoutput>`__ |
+-----------+-------------+--------------------------------------------------------------+

.. _consumes-2:

Consumes
^^^^^^^^

-  ``application/json-patch+json; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/*+json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _produces-9:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-9:

Tags
^^^^

-  BlockChain

send a transaction
~~~~~~~~~~~~~~~~~~

::

   POST /api/blockChain/sendRawTransaction

.. _parameters-8:

Parameters
^^^^^^^^^^

+----------+------------------------+-------------------------------+
| Type     | Name                   | Schema                        |
+==========+========================+===============================+
| **Body** | **input** \ *optional* | `SendRawTransactionInput      |
|          |                        | <#sendrawtransactioninput>`__ |
+----------+------------------------+-------------------------------+

.. _responses-10:

Responses
^^^^^^^^^

+-----------+-------------+--------------------------------+
| HTTP Code | Description | Schema                         |
+===========+=============+================================+
| **200**   | Success     | `SendRawTransactionOutput      |
|           |             | <#sendrawtransactionoutput>`__ |
+-----------+-------------+--------------------------------+

.. _consumes-3:

Consumes
^^^^^^^^

-  ``application/json-patch+json; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/*+json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _produces-10:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-10:

Tags
^^^^

-  BlockChain

Broadcast a transaction
~~~~~~~~~~~~~~~~~~~~~~~

::

   POST /api/blockChain/sendTransaction

.. _parameters-9:

Parameters
^^^^^^^^^^

+----------+------------------------+----------------------------+
| Type     | Name                   | Schema                     |
+==========+========================+============================+
| **Body** | **input** \ *optional* | `SendTransactionInput      |
|          |                        | <#sendtransactioninput>`__ |
+----------+------------------------+----------------------------+

.. _responses-11:

Responses
^^^^^^^^^

========= =========== ==================================================
HTTP Code Description Schema
========= =========== ==================================================
**200**   Success     `SendTransactionOutput <#sendtransactionoutput>`__
========= =========== ==================================================

.. _consumes-4:

Consumes
^^^^^^^^

-  ``application/json-patch+json; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/*+json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _produces-11:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-11:

Tags
^^^^

-  BlockChain

Broadcast multiple transactions
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   POST /api/blockChain/sendTransactions

.. _parameters-10:

Parameters
^^^^^^^^^^

+----------+------------------------+-----------------------------+
| Type     | Name                   | Schema                      |
+==========+========================+=============================+
| **Body** | **input** \ *optional* | `SendTransactionsInput      |
|          |                        | <#sendtransactionsinput>`__ |
+----------+------------------------+-----------------------------+

.. _responses-12:

Responses
^^^^^^^^^

========= =========== ================
HTTP Code Description Schema
========= =========== ================
**200**   Success     < string > array
========= =========== ================

.. _consumes-5:

Consumes
^^^^^^^^

-  ``application/json-patch+json; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/*+json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _produces-12:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-12:

Tags
^^^^

-  BlockChain

Estimate transaction fee
~~~~~~~~~~~~~~~~~~~~~~~~

::

    POST /api/blockChain/calculateTransactionFee

.. _parameters-21:

Parameters
^^^^^^^^^^

========= ============================ ============================================================================= ===========
Type      Name                         Schema                                                                        Default
========= ============================ ============================================================================= ===========
**Body**  **Input** \ *optional*       `CalculateTransactionFeeInput <#calculatetransactionfeeinput>`__
========= ============================ ============================================================================= ===========

.. _responses-21:

Responses
^^^^^^^^^

========= =========== ========================================================================================
HTTP Code Description Schema
========= =========== ========================================================================================
**200**   Success     `CalculateTransactionFeeOutput <#calculatetransactionfeeoutput>`__
========= =========== ========================================================================================

.. _consumes-21:

Consumes
^^^^^^^^
-  ``application/json-patch+json; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/*+json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _produces-21:

Produces
^^^^^^^^
-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-21:

Tags
^^^^

-  BlockChain

GET /api/blockChain/taskQueueStatus
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. _responses-13:

Responses
^^^^^^^^^

========= =========== ==================================================
HTTP Code Description Schema
========= =========== ==================================================
**200**   Success     < `TaskQueueInfoDto <#taskqueueinfodto>`__ > array
========= =========== ==================================================

.. _produces-13:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-13:

Tags
^^^^

-  BlockChain

Get the transaction pool status.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/blockChain/transactionPoolStatus

.. _responses-14:

Responses
^^^^^^^^^

+-----------+-------------+------------------------------------------+
| HTTP Code | Description | Schema                                   |
+===========+=============+==========================================+
| **200**   | Success     | `GetTransactionPoolStatusOutput          |
|           |             | <#gettransactionpoolstatusoutput>`__     |
+-----------+-------------+------------------------------------------+

.. _produces-14:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-14:

Tags
^^^^

-  BlockChain

Get the current status of a transaction
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/blockChain/transactionResult

.. _parameters-11:

Parameters
^^^^^^^^^^

========= ============================== ============== ======
Type      Name                           Description    Schema
========= ============================== ============== ======
**Query** **transactionId** \ *optional* transaction id string
========= ============================== ============== ======

.. _responses-15:

Responses
^^^^^^^^^

========= =========== ================================================
HTTP Code Description Schema
========= =========== ================================================
**200**   Success     `TransactionResultDto <#transactionresultdto>`__
========= =========== ================================================

The transaction result DTO object returned
contains the transaction that contains the parameter values used for the
call. The node will return the byte array as a base64 encoded string if
it can’t decode it. 

.. _produces-15:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-15:

Tags
^^^^

-  BlockChain

Get multiple transaction results.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/blockChain/transactionResults

.. _parameters-12:

Parameters
^^^^^^^^^^

========= ========================== =========== =============== =======
Type      Name                       Description Schema          Default
========= ========================== =========== =============== =======
**Query** **blockHash** \ *optional* block hash  string          
**Query** **limit** \ *optional*     limit       integer (int32) ``10``
**Query** **offset** \ *optional*    offset      integer (int32) ``0``
========= ========================== =========== =============== =======

.. _responses-16:

Responses
^^^^^^^^^

========= =========== ==========================================================
HTTP Code Description Schema
========= =========== ==========================================================
**200**   Success     < `TransactionResultDto <#transactionresultdto>`__ > array
========= =========== ==========================================================

.. _produces-16:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-16:

Tags
^^^^

-  BlockChain

Net API
-------

Get information about the node’s connection to the network.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/net/networkInfo

.. _responses-17:

Responses
^^^^^^^^^

========= =========== ================================================
HTTP Code Description Schema
========= =========== ================================================
**200**   Success     `GetNetworkInfoOutput <#getnetworkinfooutput>`__
========= =========== ================================================

.. _produces-17:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-17:

Tags
^^^^

-  Net

Attempts to add a node to the connected network nodes
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   POST /api/net/peer

.. _parameters-13:

Parameters
^^^^^^^^^^

======== ====================== ================================
Type     Name                   Schema
======== ====================== ================================
**Body** **input** \ *optional* `AddPeerInput <#addpeerinput>`__
======== ====================== ================================

.. _responses-18:

Responses
^^^^^^^^^

========= ============== =======
HTTP Code Description    Schema
========= ============== =======
**200**   Success        boolean

**401**   Unauthorized      
========= ============== =======

.. _security-2:

Security
^^^^^^^^

- Basic Authentication

.. _consumes-6:

Consumes
^^^^^^^^

-  ``application/json-patch+json; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/*+json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _produces-18:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-18:

Tags
^^^^

-  Net

Attempts to remove a node from the connected network nodes
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   DELETE /api/net/peer

.. _parameters-14:

Parameters
^^^^^^^^^^

========= ======================== =========== ======
Type      Name                     Description Schema
========= ======================== =========== ======
**Query** **address** \ *optional* ip address  string
========= ======================== =========== ======

.. _responses-19:

Responses
^^^^^^^^^

========= ============= =======
HTTP Code Description   Schema
========= ============= =======
**200**   Success       boolean

**401**   Unauthorized       
========= ============= =======

.. _security-1:

Security
^^^^^^^^

- Basic Authentication

.. _produces-19:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-19:

Tags
^^^^

-  Net

Get peer info about the connected network nodes
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

::

   GET /api/net/peers

.. _parameters-15:

Parameters
^^^^^^^^^^

========= ============================ ======= ===========
Type      Name                         Schema  Default
========= ============================ ======= ===========
**Query** **withMetrics** \ *optional* boolean ``"false"``
========= ============================ ======= ===========

.. _responses-20:

Responses
^^^^^^^^^

========= =========== ================================
HTTP Code Description Schema
========= =========== ================================
**200**   Success     < `PeerDto <#peerdto>`__ > array
========= =========== ================================

.. _produces-20:

Produces
^^^^^^^^

-  ``text/plain; v=1.0``
-  ``application/json; v=1.0``
-  ``text/json; v=1.0``
-  ``application/x-protobuf; v=1.0``

.. _tags-20:

Tags
^^^^

-  BlockChain


Definitions
~~~~~~~~~~~

AddPeerInput
^^^^^^^^^^^^

======================== =========== ======
Name                     Description Schema
======================== =========== ======
**Address** \ *optional* ip address  string
======================== =========== ======

BlockBodyDto
^^^^^^^^^^^^

================================== ================
Name                               Schema
================================== ================
**Transactions** \ *optional*      < string > array
**TransactionsCount** \ *optional* integer (int32)
================================== ================

BlockDto
^^^^^^^^

========================== ====================================
Name                       Schema
========================== ====================================
**BlockHash** \ *optional* string
**Body** \ *optional*      `BlockBodyDto <#blockbodydto>`__
**Header** \ *optional*    `BlockHeaderDto <#blockheaderdto>`__
**BlockSize** \ *optional* integer (int32)
========================== ====================================

BlockHeaderDto
^^^^^^^^^^^^^^

================================================= ==================
Name                                              Schema
================================================= ==================
**Bloom** \ *optional*                            string
**ChainId** \ *optional*                          string
**Extra** \ *optional*                            string
**Height** \ *optional*                           integer (int64)
**MerkleTreeRootOfTransactions** \ *optional*     string
**MerkleTreeRootOfWorldState** \ *optional*       string
**MerkleTreeRootOfTransactionState** \ *optional* string
**PreviousBlockHash** \ *optional*                string
**SignerPubkey** \ *optional*                     string
**Time** \ *optional*                             string (date-time)
================================================= ==================

BlockStateDto
^^^^^^^^^^^^^

============================= ======================
Name                          Schema
============================= ======================
**BlockHash** \ *optional*    string
**BlockHeight** \ *optional*  integer (int64)
**Changes** \ *optional*      < string, string > map
**Deletes** \ *optional*      < string > array
**PreviousHash** \ *optional* string
============================= ======================

ChainStatusDto
^^^^^^^^^^^^^^

+-----------------------------------+---------------------------------+
| Name                              | Schema                          |
+===================================+=================================+
| **BestChainHash** \ *optional*    | string                          |
+-----------------------------------+---------------------------------+
| **BestChainHeight** \ *optional*  | integer (int64)                 |
+-----------------------------------+---------------------------------+
| **Branches** \ *optional*         | < string, integer (int64) > map |
+-----------------------------------+---------------------------------+
| **ChainId** \ *optional*          | string                          |
+-----------------------------------+---------------------------------+
| **GenesisBlockHash** \ *optional* | string                          |
+-----------------------------------+---------------------------------+
| **GenesisContractAddress**        | string                          |
| \ *optional*                      |                                 |
+-----------------------------------+---------------------------------+
| **LastIrreversibleBlockHash**     | string                          |
| \ *optional*                      |                                 |
+-----------------------------------+---------------------------------+
| **LastIrreversibleBlockHeight**   | integer (int64)                 |
| \ *optional*                      |                                 |
+-----------------------------------+---------------------------------+
| **LongestChainHash** \ *optional* | string                          |
+-----------------------------------+---------------------------------+
| **LongestChainHeight**            | integer (int64)                 |
| \ *optional*                      |                                 |
+-----------------------------------+---------------------------------+
| **NotLinkedBlocks** \ *optional*  | < string, string > map          |
+-----------------------------------+---------------------------------+

CreateRawTransactionInput
^^^^^^^^^^^^^^^^^^^^^^^^^

+---------------------------------+----------------------------+-----------------+
| Name                            | Description                | Schema          |
+=================================+============================+=================+
| **From** \ *required*           | from address               | string          |
+---------------------------------+----------------------------+-----------------+
| **MethodName** \ *required*     | contract method name       | string          |
+---------------------------------+----------------------------+-----------------+
| **Params** \ *required*         | contract method parameters | string          |
+---------------------------------+----------------------------+-----------------+
| **RefBlockHash** \ *required*   | refer block hash           | string          |
+---------------------------------+----------------------------+-----------------+
| **RefBlockNumber** \ *required* | refer block height         | integer (int64) |
+---------------------------------+----------------------------+-----------------+
| **To** \ *required*             | to address                 | string          |
+---------------------------------+----------------------------+-----------------+

CreateRawTransactionOutput
^^^^^^^^^^^^^^^^^^^^^^^^^^

=============================== ======
Name                            Schema
=============================== ======
**RawTransaction** \ *optional* string
=============================== ======

ExecuteRawTransactionDto
^^^^^^^^^^^^^^^^^^^^^^^^

=============================== =============== ======
Name                            Description     Schema
=============================== =============== ======
**RawTransaction** \ *optional* raw transaction string
**Signature** \ *optional*      signature       string
=============================== =============== ======

ExecuteTransactionDto
^^^^^^^^^^^^^^^^^^^^^

=============================== =============== ======
Name                            Description     Schema
=============================== =============== ======
**RawTransaction** \ *optional* raw transaction string
=============================== =============== ======

GetNetworkInfoOutput
^^^^^^^^^^^^^^^^^^^^

+-----------------------+-----------------------+-----------------------+
| Name                  | Description           | Schema                |
+=======================+=======================+=======================+
| **Connections**       | total number of open  | integer (int32)       |
| \ *optional*          | connections between   |                       |
|                       | this node and other   |                       |
|                       | nodes                 |                       |
+-----------------------+-----------------------+-----------------------+
| **ProtocolVersion**   | network protocol      | integer (int32)       |
| \ *optional*          | version               |                       |
+-----------------------+-----------------------+-----------------------+
| **Version**           | node version          | string                |
| \ *optional*          |                       |                       |
+-----------------------+-----------------------+-----------------------+

GetTransactionPoolStatusOutput
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

========================== ===============
Name                       Schema
========================== ===============
**Queued** \ *optional*    integer (int32)
**Validated** \ *optional* integer (int32)
========================== ===============

LogEventDto
^^^^^^^^^^^

=========================== ================
Name                        Schema
=========================== ================
**Address** \ *optional*    string
**Indexed** \ *optional*    < string > array
**Name** \ *optional*       string
**NonIndexed** \ *optional* string
=========================== ================

MerklePathDto
^^^^^^^^^^^^^

================================= ====================================================
Name                              Schema
================================= ====================================================
**MerklePathNodes** \ *optional*  < `MerklePathNodeDto <#merklepathnodedto>`__ > array
================================= ====================================================

MerklePathNodeDto
^^^^^^^^^^^^^^^^^

================================ =======
Name                             Schema
================================ =======
**Hash** \ *optional*            string
**IsLeftChildNode** \ *optional* boolean
================================ =======

MinerInRoundDto
^^^^^^^^^^^^^^^

+--------------------------------------+------------------------------+
| Name                                 | Schema                       |
+======================================+==============================+
| **ActualMiningTimes** \ *optional*   | < string (date-time) > array |
+--------------------------------------+------------------------------+
| **ExpectedMiningTime** \ *optional*  | string (date-time)           |
+--------------------------------------+------------------------------+
| **ImpliedIrreversibleBlockHeight**   | integer (int64)              |
| \ *optional*                         |                              |
+--------------------------------------+------------------------------+
| **InValue** \ *optional*             | string                       |
+--------------------------------------+------------------------------+
| **MissedBlocks** \ *optional*        | integer (int64)              |
+--------------------------------------+------------------------------+
| **Order** \ *optional*               | integer (int32)              |
+--------------------------------------+------------------------------+
| **OutValue** \ *optional*            | string                       |
+--------------------------------------+------------------------------+
| **PreviousInValue** \ *optional*     | string                       |
+--------------------------------------+------------------------------+
| **ProducedBlocks** \ *optional*      | integer (int64)              |
+--------------------------------------+------------------------------+
| **ProducedTinyBlocks** \ *optional*  | integer (int32)              |
+--------------------------------------+------------------------------+

PeerDto
^^^^^^^

+----------------------------------+-----------------------------------+
| Name                             | Schema                            |
+==================================+===================================+
| **BufferedAnnouncementsCount**   | integer (int32)                   |
| \ *optional*                     |                                   |
+----------------------------------+-----------------------------------+
| **BufferedBlocksCount**          | integer (int32)                   |
| \ *optional*                     |                                   |
+----------------------------------+-----------------------------------+
| **BufferedTransactionsCount**    | integer (int32)                   |
| \ *optional*                     |                                   |
+----------------------------------+-----------------------------------+
| **ConnectionTime** \ *optional*  | integer (int64)                   |
+----------------------------------+-----------------------------------+
| **Inbound** \ *optional*         | boolean                           |
+----------------------------------+-----------------------------------+
| **IpAddress** \ *optional*       | string                            |
+----------------------------------+-----------------------------------+
| **ProtocolVersion** \ *optional* | integer (int32)                   |
+----------------------------------+-----------------------------------+
| **RequestMetrics** \ *optional*  | <                                 |
|                                  | `RequestMetric <#requestmetric>`__|
|                                  | > array                           |
+----------------------------------+-----------------------------------+
| **ConnectionStatus** \ *optional*| string                            |
+----------------------------------+-----------------------------------+
| **NodeVersion** \ *optional*     | string                            |
+----------------------------------+-----------------------------------+

RequestMetric
^^^^^^^^^^^^^

============================== ==========================
Name                           Schema
============================== ==========================
**Info** \ *optional*          string
**MethodName** \ *optional*    string
**RequestTime** \ *optional*   `Timestamp <#timestamp>`__
**RoundTripTime** \ *optional* integer (int64)
============================== ==========================

RoundDto
^^^^^^^^

+----------------------------------+----------------------------------+
| Name                             | Schema                           |
+==================================+==================================+
| **Co                             | integer (int64)                  |
| nfirmedIrreversibleBlockHeight** |                                  |
| \ *optional*                     |                                  |
+----------------------------------+----------------------------------+
| **Confirm                        | integer (int64)                  |
| edIrreversibleBlockRoundNumber** |                                  |
| \ *optional*                     |                                  |
+----------------------------------+----------------------------------+
| **Ext                            | string                           |
| raBlockProducerOfPreviousRound** |                                  |
| \ *optional*                     |                                  |
+----------------------------------+----------------------------------+
| **IsMinerListJustChanged**       | boolean                          |
| \ *optional*                     |                                  |
+----------------------------------+----------------------------------+
| **RealTimeMinerInformation**     | < string,                        |
| \ *optional*                     | `MinerInRoundDto                 |
|                                  | <#minerinrounddto>`__ > map      |
+----------------------------------+----------------------------------+
| **RoundId** \ *optional*         | integer (int64)                  |
+----------------------------------+----------------------------------+
| **RoundNumber** \ *optional*     | integer (int64)                  |
+----------------------------------+----------------------------------+
| **TermNumber** \ *optional*      | integer (int64)                  |
+----------------------------------+----------------------------------+

SendRawTransactionInput
^^^^^^^^^^^^^^^^^^^^^^^

+----------------------------+----------------------------+---------+
| Name                       | Description                | Schema  |
+============================+============================+=========+
| **ReturnTransaction**      | return transaction detail  | boolean |
| \ *optional*               | or not                     |         |
+----------------------------+----------------------------+---------+
| **Signature** \ *optional* | signature                  | string  |
+----------------------------+----------------------------+---------+
| **Transaction**            | raw transaction            | string  |
| \ *optional*               |                            |         |
+----------------------------+----------------------------+---------+

SendRawTransactionOutput
^^^^^^^^^^^^^^^^^^^^^^^^

============================== ====================================
Name                           Schema
============================== ====================================
**Transaction** \ *optional*   `TransactionDto <#transactiondto>`__
**TransactionId** \ *optional* string
============================== ====================================

SendTransactionInput
^^^^^^^^^^^^^^^^^^^^

=============================== =============== ======
Name                            Description     Schema
=============================== =============== ======
**RawTransaction** \ *optional* raw transaction string
=============================== =============== ======

SendTransactionOutput
^^^^^^^^^^^^^^^^^^^^^

============================== ======
Name                           Schema
============================== ======
**TransactionId** \ *optional* string
============================== ======

SendTransactionsInput
^^^^^^^^^^^^^^^^^^^^^

================================ ================ ======
Name                             Description      Schema
================================ ================ ======
**RawTransactions** \ *optional* raw transactions string
================================ ================ ======

TaskQueueInfoDto
^^^^^^^^^^^^^^^^

===================== ===============
Name                  Schema
===================== ===============
**Name** \ *optional* string
**Size** \ *optional* integer (int32)
===================== ===============

Timestamp
^^^^^^^^^

======================== ===============
Name                     Schema
======================== ===============
**Nanos** \ *optional*   integer (int32)
**Seconds** \ *optional* integer (int64)
======================== ===============

TransactionDto
^^^^^^^^^^^^^^

=============================== ===============
Name                            Schema
=============================== ===============
**From** \ *optional*           string
**MethodName** \ *optional*     string
**Params** \ *optional*         string
**RefBlockNumber** \ *optional* integer (int64)
**RefBlockPrefix** \ *optional* string
**Signature** \ *optional*      string
**To** \ *optional*             string
=============================== ===============

TransactionResultDto
^^^^^^^^^^^^^^^^^^^^

================================ ========================================
Name                             Schema
================================ ========================================
**BlockHash** \ *optional*       string
**BlockNumber** \ *optional*     integer (int64)
**Bloom** \ *optional*           string
**Error** \ *optional*           string
**Logs** \ *optional*            < `LogEventDto <#logeventdto>`__ > array
**ReturnValue** \ *optional*     string
**Status** \ *optional*          string
**Transaction** \ *optional*     `TransactionDto <#transactiondto>`__
**TransactionId** \ *optional*   string
**TransactionSize** \ *optional* integer (int32)
================================ ========================================

CalculateTransactionFeeInput
^^^^^^^^^^^^^^^^^^^^^^^^^^^^

================================ ========================================
Name                             Schema
================================ ========================================
**RawTrasaction** \ *optional*   string
================================ ========================================

CalculateTransactionFeeOutput
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

================================ ========================================
Name                             Schema
================================ ========================================
**Success** \ *optional*         bool
**TransactionFee** \ *optional*  Dictionary<string, long>
**ResourceFee** \ *optional*     Dictionary<string, long>
================================ ========================================