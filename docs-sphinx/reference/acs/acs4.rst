ACS4 - Consensus Standard
=========================

ACS4 is used to customize consensus mechanisms.

Interface
---------

If you want to customize the consensus mechanism, you need to implement
the following five interfaces:

-  GetConsensusCommand, whose parameter is a binary array, returns
   ConsensusCommand defined in acs4.proto. This type is used to indicate
   the start time of the next block, the block time limit, and the final
   cut-off time for the account calling GetConsensus Command;
-  GetConsensusExtraData, the parameters and return values are binary
   arrays, which are used to generate consensus block header information
   through consensus contracts when a new block is produced;
-  GenerateConsensusTransactions, the parameter is a binary array, and
   the return value is of type TransactionList. It is used to generate a
   consensus system transaction when a block is generated. Each block
   will contain only one consensus transaction, which is used to write
   the latest consensus information to the State database;
-  ValidateConsensusBeforeExecution, the parameter is a binary array,
   and the return value is of type ValidationResult, is used to verify
   whether the consensus information in the block header is correct
   before the block executes;
-  ValidateConsensusAfterExecution, with the same parameter and return
   value, is used to verify that the consensus information written to
   the State is correct after the block executes.

ConsensusCommand, ValidationResult and TransactionList are defined as:

.. code:: proto

   message ConsensusCommand {
       int32 limit_milliseconds_of_mining_block = 1;// Time limit of mining next block.
       bytes hint = 2;// Context of Hint is diverse according to the consensus protocol we choose, so we use bytes.
       google.protobuf.Timestamp arranged_mining_time = 3;
       google.protobuf.Timestamp mining_due_time = 4;
   }
   message ValidationResult {
       bool success = 1;
       string message = 2;
       bool is_re_trigger = 3;
   }
   message TransactionList {
       repeated aelf.Transaction transactions = 1;
   }

Usage
-----

The five interfaces defined in ACS4 basically correspond to the five
methods of the IConsensusService interface in the AElf.Kernel.Consensus
project:

+-------------------------+-----------------------------+-----------------------------------------+------------------------------------+
| ACS4                    | IConsensusService           | Methodology                             | The Timing To Call                 |
+=========================+=============================+=========================================+====================================+
| ``GetConsensusCommand`` | Task TriggerConsensusAsync  | When TriggerConsensusAsync is called,   | 1. When the node is started;       |
|                         |                             |                                         |                                    |
|                         | (ChainContext chainContext);| it will use the account configured by   | 2. When the BestChainFound-        |
|                         |                             |                                         |                                    |
|                         |                             | the node to call the GetConsensusCommand| EventData event is thrown;         |
|                         |                             |                                         |                                    |
|                         |                             | method of the consensus contract        | 3. When the validation of consensus|
|                         |                             |                                         |                                    |
|                         |                             | to obtain block information             | data fails and the consensus needs |
|                         |                             |                                         |                                    |
|                         |                             | ConsensusCommand), and use it to        | to be triggered again (The         |
|                         |                             |                                         |                                    |
|                         |                             | (see IConsensusScheduler implementation)| IsReTrigger field of the           |
|                         |                             |                                         |                                    |
|                         |                             | .                                       | ValidationResult type is true);    |
+-------------------------+-----------------------------+-----------------------------------------+------------------------------------+
| ``GetConsensus-``       | Task<byte[]> GetConsensus   | When a node produces a block, it will   | At the time that the node produces |
|                         |                             |                                         |                                    |
| ``ExtraData``           | ExtraDataAsync(ChainContext | generate block header information for   | a new block.                       |
|                         |                             |                                         |                                    |
|                         | chainContext);              | the new block by IBlockExtraDataService.|                                    |
|                         |                             |                                         |                                    |
|                         |                             | This service is implemented to traverse |                                    |
|                         |                             |                                         |                                    |
|                         |                             | all IBlockExtraDataProvider             |                                    |
|                         |                             |                                         |                                    |
|                         |                             | implementations, and they generate      |                                    |
|                         |                             |                                         |                                    |
|                         |                             | binary array information into the       |                                    |
|                         |                             |                                         |                                    |
|                         |                             | ExtraData field of BlockHeader. The     |                                    |
|                         |                             |                                         |                                    |
|                         |                             | consensus block header information is   |                                    |
|                         |                             |                                         |                                    |
|                         |                             | provided by ConsensusExtraDataProvider, |                                    |
|                         |                             |                                         |                                    |
|                         |                             | in which the GetConsensusExtraDataAsync |                                    |
|                         |                             |                                         |                                    |
|                         |                             | of the IConsensusService in the         |                                    |
|                         |                             |                                         |                                    |
|                         |                             | consensus contract is called, and the   |                                    |
|                         |                             |                                         |                                    |
|                         |                             | GetConsensusExtraDataAsync method is    |                                    |
|                         |                             |                                         |                                    |
|                         |                             | implemented by calling the              |                                    |
|                         |                             |                                         |                                    |
|                         |                             | GetConsensusExtraData in the consensus  |                                    |
|                         |                             |                                         |                                    |
|                         |                             | contract.                               |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
+-------------------------+-----------------------------+-----------------------------------------+------------------------------------+
| ``GenerateConsensus-``  | Task<List<Transaction>>     | In the process of generating new blocks,| At the time that the node produces |
|                         |                             |                                         |                                    |
| ``Transactions``        | GenerateConsensus-          | a consensus transaction needs to be     | a new block.                       |
|                         |                             |                                         |                                    |
|                         | TransactionsAsync(          | generated as one of the system          |                                    |
|                         |                             |                                         |                                    |
|                         | ChainContext chainContext); | transactions. The basic principle is the|                                    |
|                         |                             |                                         |                                    |
|                         |                             | same as GetConsensusExtraData.          |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
+-------------------------+-----------------------------+-----------------------------------------+------------------------------------+
| ``ValidateConsensus-``  | Task<bool> ValidateConsensus| As long as the IBlockValidationProvider | At the time that the node produces |
|                         |                             |                                         |                                    |
| ``BeforeExecution``     | BeforeExecutionAsync(       | interface is implemented, a new block   | a new block.                       |
|                         |                             |                                         |                                    |
|                         | chainContext, byte[]        | validator can be added.  The consensus  |                                    |
|                         |                             |                                         |                                    |
|                         | consensusExtraData);        | validator is ConsensusValidationProvider|                                    |
|                         |                             |                                         |                                    |
|                         |                             | , where ValidateBlockBeforeExecuteAsync |                                    |
|                         |                             |                                         |                                    |
|                         |                             | is implemented by calling the           |                                    |
|                         |                             |                                         |                                    |
|                         |                             | ValidateConsensusBeforeExecution method |                                    |
|                         |                             |                                         |                                    |
|                         |                             | of the consensus contract.              |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
+-------------------------+-----------------------------+-----------------------------------------+------------------------------------+
| ``ValidateConsensus-``  | Task<bool> ValidateConsensus| The implementation of                   | At the time that the node produces |
|                         |                             |                                         |                                    |
| ``AfterExecution``      | AfterExecutionAsync         | ValidateBlockAfterExecuteAsync in       | a new block.                       |
|                         |                             |                                         |                                    |
|                         | ( ChainContext chainContext,| ConsensusValidationProvider is to call  |                                    |
|                         |                             |                                         |                                    |
|                         | byte[] consensusExtraData); | the ValidateConsensusAfterExecution     |                                    |
|                         |                             |                                         |                                    |
|                         |                             | in the consensus contract.              |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
|                         |                             |                                         |                                    |
+-------------------------+-----------------------------+-----------------------------------------+------------------------------------+











.. .. list-table::
..    :widths: 15 10 30 30
..    :header-rows: 1

..    * - ACS4  
..      - IConsensusService
..      - Methodology
..      - The Timing To Call
..    * - GetConsensusCommand
..      - Task TriggerConsensusAsync(ChainContext chainContext)
..      - When TriggerConsensusAsync is called, it will use the account configured by the node to call the GetConsensusCommand method of the consensus contract to obtain block information(ConsensusCommand), and use it to update the local consensus scheduler (see IConsensusScheduler implementation).
..      - 1. When the node is started;

Example
-------

You can refer to the implementation of the AEDPoS contract.
