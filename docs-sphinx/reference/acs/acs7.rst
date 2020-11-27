ACS7 - Contract CrossChain Standard
===================================

ACS7 is for cross chain related contract implementation.

Interface
---------

This involves methods for chain creation and indexing:

-  `RequestSideChainCreation` Request to create a new side chain. This is going to create a new proposal about side chain creation and  wait for approvals from authorized organizations.
    It will create a new proposal for side chain creation and wait for approve. Refer :doc:`How to request side chain <../../tutorials/cross-chain/request-new-side-chain>` for more details.
-  `ReleaseSideChainCreation` Release the side chain creation request if already approved and it will call the method CreateSideChain. 
-  `CreateSideChain` The method to create a new side chain. The side chain should be created after this execution.
-  `Recharge` Recharge indexing fee for one specific side chain. 
-  `DisposeSideChain` Stop indexing for one specific side chain. The block from this side chain won't be indexed by main chain after tihs execution.
-  `ProposeCrossChainIndexing` Propose cross chain to be indexed. This is going to create a new proposal about cross chain indexing and wait for approvals from authorized organizations.
-  `ReleaseCrossChainIndexing` Release the proposed indexing if already approved. And then cross chain data is going to be indexed.
-  `AdjustIndexingFeePrice` Adjust indexing fee for one specific side chain.
-  `VerifyTransaction` Transaction cross chain verification. This is for transaction existence verification which is vital to cross chain stuffs. Refer :doc:`Cross chain verification <../../architecture/cross-chain/verify>` for more details.
-  `GetChainInitializationData` Get side chain initialization data which is needed during the first time startup of side chain node. 


Example
-------

ACS7 declares methods for the scenes about cross chain. AElf provides the implementation for ACS7, ``CrossChainContract``.
You can refer to the implementation of the :doc:`Cross chain contract api<../smart-contract-api/cross-chain>`.