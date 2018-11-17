# Why we choose DPoS in AElf?
With the prosperity of blockchain, several technologies have gained the attention of the public which previously used to be obscure and technical - primarily cryptography and consensus. Today I’m going to talk about the consensus algorithms used in AElf blockchain to answer the question people usually ask me:

**"Why DPoS?"**

There are quite a few consensus algorithms nowadays, Paxos, Raft, PBFT, PoW, PoS, DPoS, what is the difference between them, and what are the positives and negatives of each algorithm? In my humble opinion, consensus mechanism can be categorized into two major types: **Cooperative and Competitive. **

The most commonly used **Cooperative Consensus algorithm** is [Paxos](https://en.wikipedia.org/wiki/Paxos_(computer_science)) and its alternative [Raft](http://thesecretlivesofdata.com/raft/). Paxos is used by [Chubby](https://research.google.com/archive/chubby.html), which is now in production in Google, and Raft is implemented in [etcd](https://github.com/coreos/etcd) by [CoreOS](https://coreos.com/) for its highly reliable distributed key-value storage. The logic behind cooperative consensus is that, suppose we have a fixed number of members, and if the majority of the members agrees on the proposal, then good, the system goes well. After they’ve reached this agreement, we start over and deal with the next round of proposals. What we can tell from this scenario is that: the voters are fixed, we cannot join and leave in random behaviour, and the voters know each other (every voter has one and **only** one vote).

The cooperative consensus algorithm is acting well on a private or safe environment because it is fast (TPS > [30k](https://github.com/coreos/etcd/blob/master/Documentation/op-guide/performance.md)). The only limitation is the latency of network, and the voting result is **permanent**. Nobody can reverse the approved proposals. It has been widely used in distributed systems in large companies for high availability. However, the drawback of such a system is obvious too. Firstly, it is distributed, but not decentralized. The difference is that a distributed system is a **concept of architecture** of a software system, but a decentralized system is a **concept of organization**.  

The voters in Paxos, generally, are controlled by a centralized authority, the peer nodes who are legitimate in the consortium are pre-configured statically by files (e.g. etcd config) or some kind of centralized certification system for voter issues. But if voters in the system are permitted to join and leave randomly without these controls, hackers can bring down the system easily by Sybil Attack, i.e. to fake the majority of the voters.

Scalability is another issue in common cooperative consensus implementations such as [PBFT](https://en.wikipedia.org/wiki/Byzantine_fault_tolerance). In theory, every vote proposed by a voter must be delivered to every other voter in the consortium - suppose we have N voters; at least N*(N-1) messages will be delivered for each round. It’s a polynomial, i.e. O(n^2). It doesn’t scale well as we can tell from the fundamental of algorithm analysis. It is even worse in practice. For each round in PBFT voting many extra communications are required, such as pre-prepare, prepare and commit. Real world deployment of the such a system usually requires around 10 voters.

**Competitive consensus** algorithms are famous in the world of cryptocurrency, such as [Proof of Work](https://en.bitcoin.it/wiki/Proof_of_work). The nodes race to solve a difficult problem for the right to produce a block and to receive the reward. It is fully decentralized, simple and elegant. Everyone can come and go at any time, and the algorithm is resilient to the Sybil Attack. There are, however, several pitfalls in such scenario. First, if more than one node solves that problem at the same time, the blockchain forks temporarily and creates many branches around the chain. In order to make sure a transaction is highly likely to be included in the longest chain, we usually have to wait for more than one confirmation of blocks and even if we’ve waited for several confirmations, it is actually not finalized (permanently etched into stone) compared to the Cooperative consensus mechanism above. Suppose we accidentally built a machine with unlimited computational power, with its use previous transactions could be rolled-back (luckily this would not be an unlimited rollback, as actually we still have some checkpoint that cannot be crossed).

Besides, the time for solving that mathematically difficult problems in a competitive consensus is not stable (due to its unpredictable behaviour - hashing). The intervals between producing two consecutive blocks can be random, between 1 minute and 1 hour (or more), a [Poisson distribution](https://en.bitcoin.it/wiki/Proof_of_work) to be precise.

The racing game between nodes are wasteful compared to the cooperative ones as the winner takes all, the losers waste their time and money, only producing carbon dioxide to the world and nothing more. Since no one wants to lose, the nodes started to work together to solve that hashing problem with the promise that if one of them solves that problem then they share the reward fairly. The real world PoW is a kind of DPoW — **Delegated Proof of Work**, we delegate our computational power to the mining pool, and it works very well in the real world.
The Proof of Stake consensus is an attempt to solve the problem of power consumption, a kind of PoS system is created by combining (multiplicatively) your hash rate and stake, formalized as:

`Hash(blockHeader ) < GetTarget(nBits) * CoinAge`

(CoinAge - the stake, more stakes adjust the degree of difficulty for block production)

Here is how we think:

In AElf, is there a consensus algorithm which took the advantages of both Cooperative and Competitive consensus mechanism and got rid of the disadvantages?

The ideal features of the consensus algorithm we’re looking for should be:

* Permanence — It is better not to be able to rollback a transaction, a fast confirmation is feasible.
* Scalability — Anyone can participate in the consensus procedure.
* Green — No carbon dioxide.

The answer is [DPoS](https://bitshares.org/technology/delegated-proof-of-stake-consensus/) (thanks to BitShares)

If we can delegate the computational power to the mining pool (DPoW), then why not stake? We can competitively (anti-Sybil attack) vote to the delegates with stakes of our own (like the mining pool but it’s green), and the delegates start to produce blocks with a kind of sequence cooperatively (scalable). The delegates which have the most ballots will take the turn to produce blocks in a cooperative way and if a delegate failed to produce a block then it can be voted out and the delegates will be voted again for a period of time. In the words of Twain “Politicians and diapers must be changed often, and for the same reason”.
