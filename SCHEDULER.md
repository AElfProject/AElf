# A brief introduction on scheduler

When a block has generated from the miner, and the block finally arrived at the ledger, how can we apply the transaction as fast as possible?
The idea behind AELF is **resource isolation**, from many aspects of the blockchain, eg:

1. a dedicated chain for a specialized scenario(cryptokitties).
2. resources isolation/concurrent execution for transactions.

This article will discuss the 2nd aspect, resource isolation and concurrent execution behind the scene.

suppose we have resources, which marked as R1,R2,R3,….Rn, and transactions, marked as T1, T2, T3, … Tm

when transactions are executing and finally update R, they’ll compete for exclusive usage on the resources.

such as :

```
T1: R1,R2
T2: R3, R4
T3: R2,R3
```

actually, ethereum apply the transactions one by one(https://github.com/ethereum/go-ethereum/blob/master/core/state_processor.go#L58 )

actually, from the case above, there can be a better strategy for transaction execution, i.e
```
Step1: Execute T3 
Step2: Parallel execution on T1, T2
```
and the resource occupation steps will be:
```
Step1: R2,R3
Step2: (R1,R2),  (R3,R4)
```
From the higher abstraction of the R & T problem, we think we can evolve an algorithm, or scheduler for transaction application.

The hints we got from the above scenario is , at beginning, every transactions are related, as:

`T1 <—> T3 <—>T2`

and after T3 is removed from the graph, T1 is unrelated with T2, as:

`T1 <—> (removed T3) <—>T2`

from the perspective of graph theory, unrelated vertex groups(islands), can execute in parallel , as T1 and T2 can be executed in parallel(although T1 and T2 have only one task). The generics of the parallel execution we can deduce is ,  if we have task groups G1,G2… Gn,   if Gm and Gn is not connected, we can execute in parallel.

`Gn <—> (no connection) <—> Gm`

and then, we can recursively split Gn into subgraphs of  Gn1, Gn2,… Gnm when execution of transactions leads to isolations of dependencies.

Here comes the hard part, which transaction we pick next, will benefits most? there could be a best strategy for picking next, I could be a NP-complete problem, but we can setup a greedy strategy for picking the most connected T(s) ,and execute them first, and maintain a data structure, such as heap for tracking the most connected Tasks.

Still, we’ve got to consider:

What is **R**?

R can be totally left to the contract designer, after all , it’s completely an abstract algorithm, but we’ve consider some data structures for R.

R as sstable:
sorted string table is one of the mostly used data structure in data processing paradigm,, we can simply group data into different sets for parallel executing, under such scenario, it’s comparable to many data-flow based blockchain, or many non-turing complete virtual machines.

R as document:
document, such as json/xml/struct, is one of the best representation of an account/user, and the interoperation between accounts is quite loose, especially for IM or personal evolving games.

WRAP UP:
Parallel execution scheduler is an important part in AELF, as an attempt to scale transaction computing power inside a single chain in AELF ecology. If it works finally, complex industry usage will be possible on blockchain.
