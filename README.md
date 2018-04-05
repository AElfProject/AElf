# AElf - Decentralized Cloud Computing Blockchain Network
[![Build Status][1]][2] 

[1]: https://travis-ci.org/AElfProject/AElf.svg?branch=master
[2]: https://travis-ci.org/AElfProject/AElf

Early stages, still under development.

# Development Roadmap

## Phase 1: The Internal Affair Of The Wonderland (Nov 2017 - March 2018)

1. Graph-based scheduler algorithm implementation, see [INTRO SCHEDULER](docs/SCHEDULER.md).
2. Tagged primitive resource Data-Structure (sstable, aka sorted string table); the basic element for resource isolation, backed by distributed KV database. This structure is comparable to dataflow processing paradigm (e.g., mr/reactivex).
3. In-cluster wire protocol (data/task marshal/unmarshal).
4. Actor (e.g., akka) based task distribution inside a single cluster/ledger.
5. Contract as demostration of concurrent execution.

## Phase 2: A Tale Between Two Nodes (March 2018 - May 2018)

1. tx_pool/mem_pool design (unconfirmed tx).
2. Basic block in-memory construct, e.g.,: merkletree, block header, transactions, statsdb, caching.
3. Wire protocol for inter-ledger block/tx transferring.
4. [DPoS](docs/CONSENSUS.md) implementation, hashing, mining.
5. Block validator, crypto-related algorithms, e.g., signing, hmac.
6. P2P discovery/communication.

## Phase 3: The Main Chain (May 2018)

1. Cross-chain, two-way peg, merkle proofs.
2. Event trigger from one sidechain to another aka cross-chain interoperability.

## Phase 4: Governance (May 2018 - August 2018)

1. Voting mechanism for sidechain join/leave.
2. Voting mechanism for emergency treatment. 

## Phase 5: Ready To Launch (August 2018 - January 2019)

1. Code optimization before mainnet launching.
2. Code review, pre-releases. 
3. Business case battle test (public beta).
