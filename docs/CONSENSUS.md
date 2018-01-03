# Why we choose DPoS in ÆLF?
With the prosperity of blockchain technology, several technologies appears to the public which used to be obscure and technical, one is cryptography and the other is consensus. Today I’m going to talk about the consensus algorithms used in ÆLF blockchain to answer the question people usually asked me:

**"Why DPoS?"**

There are quite a few consensus algorithms nowadays, Paxos, Raft, PBFT, PoW, PoS, DPoS, what’s the difference between them, and how about the pros and cons of them? In my humble opinion, consensus mechanism can be categorized into two major types: **Cooperative and Competitive.**

The most commonly used **Cooperative Consensus algorithm** is [Paxos](https://en.wikipedia.org/wiki/Paxos_(computer_science)) and its alternative [Raft](http://thesecretlivesofdata.com/raft/). Paxos is used by [Chubby](https://research.google.com/archive/chubby.html), which is now in production in Google, and Raft is implemented in [etcd](https://github.com/coreos/etcd) by [CoreOS](https://coreos.com/) for its highly reliable distributed key-value storage. The logic behind cooperative consensus is that, suppose we have a fixed number of members, and if the majority of the members agrees on the proposal, good, then the system goes well. After we’ve reached this agreement, we start over and deal with the next round of proposal. What we can tell from this scenario is that, the voters are fixed, we cannot join and leave in random behaviour, and the voters know each other (every voters has one and only one vote).

The cooperative consensus algorithm is acting well on private or safe environment, because It’s fast (tps > [30k](https://github.com/coreos/etcd/blob/master/Documentation/op-guide/performance.md)). The only limitation is the latency of network briefly, and the voting result is **permanent**. Nobody can reverse the approved proposals. It’s been widely used in distributed systems in large companies for high availability. But the drawback of such system is obvious too. First, it’s distributed, but not decentralized. The difference is, a distributed system is a **concept of architecture** of a software system, but a decentralized system is a **concept of organization**.  

The voters in Paxos are controlled by a centralized authority in general, the peer nodes who are legitimate in the consortium are pre-configured statically by files (e.g. etcd config) or some kind of centralized certification system for voter issue. But if voters in the system can join and leave randomly without these controls, hackers can bring down the system easily by Sybil Attack, i.e. to fake the majority of the voters.

Scalability is another issue in common cooperative consensus implementations such as [PBFT](https://en.wikipedia.org/wiki/Byzantine_fault_tolerance), in theory every vote proposed by a voter must be delivered to every other voter in the consortium, suppose we have N voters, at least N*(N-1) messages will be delivered for each round. It’s a polynomial i.e. O(n^2). It doesn’t scale well as we can tell from the fundamental of algorithm analysis. It’s even worse in practice. For each round in PBFT voting, many extra communications are required, such as pre-prepare, prepare, commit. Actually, real world deployment of the such system is usually around 10 voters.

**Competitive consensus** algorithms are famous in crypto world, such as [Proof of Work](https://en.bitcoin.it/wiki/Proof_of_work). The nodes race to solve a difficult problem for the right to produce a block and get the reward. It’s fully decentralized, simple, elegant. Everyone can come and go at any time, and resilient to Sybil Attack. But there are several pitfalls in such scenario, first, if more than one node solves that problem at the same time, the blockchain forks temporarily, and creates many branches around the chain. In order to make sure a transaction is highly likely to be included in the longest chain, we usually have to wait for more than one confirmation of blocks, and even if we’ve waited for several confirmations, it’s actually not finalized (permanently etched into stone) compared to the Cooperative consensus mechanism above. Suppose we accidentally built a machine with unlimited computational power, previous transactions can be rolled-back (luckily not unlimited rollback actually, we still have some checkpoint that cannot be crossed.).

Besides, the time for solving that mathematical hard problem in competitive consensus is not stable (because of it’s un-predictable behaviour, hashing). The intervals between producing two consecutive blocks can be random between 1 minute and 1 hour (or more), a [Poisson distribution](https://en.bitcoin.it/wiki/Proof_of_work) precisely.

The racing game between nodes are wasteful compared to the cooperative ones, because the winner takes all, losers waste their time and money, only producing carbon dioxide to the world and nothing more. Since no one wants to lose, the nodes started to work together to solve that hashing problem, with promise that if one of them solves that problem, they share the reward fairly. The real world PoW is kind of DPoW — **Delegated Proof of Work**, we delegate our computational power to the mining pool, and it works so well in the real world.
The Proof of Stake consensus is an attempt to solve the problem of power consumption, a kind of PoS system is by combining (multiplicatively) your hashrate and stake, formalized as:

`Hash(blockHeader ) < GetTarget(nBits) * CoinAge`

(CoinAge - the stake, more stakes adjusts the degree of difficulty for block production)

Here is how we think:

In ÆLF, is there a consensus algorithm which took the advantages of both Cooperative and Competitive consensus mechanism and got rid of the disadvantages?

The ideal features of the consensus algorithm we’re looking for should be:

* permanent — it’s better not able to rollback a transaction, a fast confirmation is feasible.
* scalability — anyone can participate in the consensus procedure.
* green — no carbon dioxide.

The answer is [DPoS](https://bitshares.org/technology/delegated-proof-of-stake-consensus/) (thanks to BitShares)

If we can delegate the computational power to the mining pool (DPoW), why cannot stake? We can competitively (anti-sybil attack) vote to the delegates with stakes of our own (like the mining pool but it’s green), and the delegates start to produce blocks with a kind of sequence cooperatively (scalable). The delegates which have most ballots will take the turn to produce blocks in a cooperative way, and if a delegate failed to produce a block, it can be voted out. and the delegates will be voted again for a period of time, in the words of Twain “Governments and diapers must be changed often, and for the same reason”.

