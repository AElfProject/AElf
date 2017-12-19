# AElf

# Development Roadmap

## Phase1 : inner affair

1. graph-based scheduler algorithm implementation.
2. tagged resource data-structure(sstable-sorted string table), the basic element for resource isolation, backed by distributed KV database.
3. in-cluster wire protocol(data/task marshal/unmarshal).
4. actor(eg. akka) based task distribution inside a single cluster/ledger.
5. a contract for demostration of concurrent execution.

## Phase2 : between ledger nodes

1. tx_pool/mem_pool design(unconfirmed tx).
2. basic block in-memory construct, eg: merkletree, block header, transactions, statsdb, caching.
3. wire protocol for inter-ledger block/tx transfering.
4. dPoS implementation, mining.
5. block validator.
6. p2p discovery/communication

## Phase3: the mainchain

1. crosschain，two-way peg
2. event trigger from one sidechain to another.
