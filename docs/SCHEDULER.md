# A brief introduction to AELF scheduler

When a block has been created by a miner, and the block finally arrives at the ledger, how can we process the transactions as fast as possible? The idea behind AELF is **resource isolation** and **parallel processing**, from many aspects of the blockchain, e.g.:

1.	a dedicated chain (resource isolation) for a specialized Smart Contract (cryptokitties).
2.	concurrent execution for unrelated transactions

This article will discuss the 2nd point, concurrent execution behind the scene.

In current blockchain mechanism, suppose we have some stats, which marked as S1, S2, S3, …., Sn, and transactions, marked as T1, T2, T3, … Tm, when transactions have been executed and finally update S(tate), they’ll compete for exclusive usage on the resources.
such as :

```
T1: S1, S2
T2: S3, S4
T3: S2, S3
```

Ethereum applies the transactions to the states one by one(https://github.com/ethereum/go-ethereum/blob/master/core/state_processor.go#L58 )

From the case above, there can be a better strategy for transaction execution, i.e

```
Step1: Execute T3 
Step2: Parallel execution on T1, T2
```

and the resource occupation steps will be:

```
Step1: S2,S3
Step2: (S1,S2),  (S3,S4)
```

From the higher abstraction of the S & T problem, we think we can develop an algorithm, or scheduler for transaction application.
The hints we got from the above scenario is, at beginning, every transactions are related, as:

`T1 <—> T3 <—>T2`

and after T3 is removed from the graph, T1 is unrelated with T2, as:

`T1 <—> (removed T3) <—>T2`

from the perspective of graph theory, unrelated vertex groups (islands), can be executed in parallel, as T1 and T2 can be executed in parallel (although T1 and T2 have only one task). The generics of the parallel execution we can deduce is, if we have task groups G1,G2… Gn, if Gm and Gn is not connected, we can execute in parallel.

`Gn <—> (no connection) <—> Gm`

and then, we can recursively split Gn into subgraphs of Gn1, Gn2,… Gnm when execution of transactions leads to elimination of dependencies.

Here comes the challenging part, which transaction we should pick next, will benefits the whole system the most? There could be a best strategy for picking next, It could be a NP-complete problem, but we can set up a greedy strategy for picking the most connected T(s), and execute them first, and maintain a data structure, such as heap for tracking the most connected Tasks.

Still, we’ve got to consider:

**What is S?**

S can be totally left to the contract designer. After all, it’s completely an abstract algorithm, but we’ve consider some data structures for S.

**S as SSTable**: [sorted string table](https://en.wikipedia.org/wiki/Bigtable) is one of the mostly used data structure in data processing paradigm. We can simply group data into different sets for parallel executing. Under such scenario, it’s comparable to many data-flow based blockchains, or non-turing complete virtual machines.

**S as document**: document, such as json/xml/struct, is one of the best representation of an account/user, and the frequencey of interoperation between accounts is quite loose, especially for IM or personal evolving games.

WRAP UP: Parallel execution scheduler is an important part in AELF, as an attempt to scale transaction computing power inside a single chain in AELF ecology. If it works as planned, complex industry usage will be made possible on AELF.


