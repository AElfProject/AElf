ACS7 - Contract CrossChain Standard
===================================

ACS7 is for cross chain related contract implementation.

Interface
---------

This involves methods for chain creation and indexing:

-  ProposeCrossChainIndexing
    - Propose cross chain to be indexed and wait for approvals from authorized organizations;
-  ReleaseCrossChainIndexing
    - Release the proposed indexing if already approved. 
-  RecordCrossChainData
    - The method to record cross chain data and complete indexing.
-  RequestSideChainCreation
    - Request to create a new side chain e wait for approvals from authorized organizations;.
-  ReleaseSideChainCreation
    - Release the side chain creation request if already approved and it will call the method CreateSideChain. 
-  CreateSideChain
    - The method to create a new side chain.
-  Recharge
    - Recharge for one specific side chain. 
-  DisposeSideChain
    - Stop indexing for one specific side chain.
-  AdjustIndexingFeePrice
    - Adjust indexing fee for one specific side chain.
-  VerifyTransaction
    - Transaction cross chain verification.
-  GetChainInitializationData
    - Get side chain initialization data which is needed during the first time startup of side chain node. 

Usage
-----

ACS7 declares methods for the scenes about cross chain. AElf provides the implementation for ACS7, CrossChainContract. 
Please refer :doc:`api docs <../smart-contract-api/cross-chain>` of this contract for more details.