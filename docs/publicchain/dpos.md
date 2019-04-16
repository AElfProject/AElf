## AElf consensus 

In every blockchain there is some form of block production and entities that produce these blocks according to some set of rules. The process of reaching consensus is an essential part of every blockchain, since its what determines which transactions get included in the block and in what order. 
The consensus protocol is split into two parts: election and scheduling. Election is the process that determines **who** gets to produce and scheduling decides on the **when**.

Timeline:

During the history of block production time is divided into **terms**, **rounds** and **timeslots**. A **term** is the largest of the time chunks, it corresponds to a potential change in block producers. The **round** corresponds to a shuffling in the order the producers produce. Finally a timeslot is the smallest unit of time, it refers to the time in which a producer should - according to the schedule - produce a block.

Election (terms):

The heart of any blockchain system are those entities that produce blocks. The block producers form a finite set of 2N+1 mining nodes. It was decided that N would starts at 8 for the public chain and increases by 1 every year, so every year 2 extra block producer can be elected compared to the previous year. These nodes in the AElf system follow and enforce all of the consensus rules defined by the system.

The block producers are **elected** by the users of the network. The election is continuous, users can vote at any time with their tokens to elect the producer they prefer. It is also possible that a user finds multiple candidates suitable and sends votes to multiple producers.

Election is a powerful and important aspect of AElf, because users can stake ELF tokens when voting for producers and in return receive special tokens that can enable the user to become a **citizen** of the ecosystem and participate actively in the blockchain’s ecosystem.

Every **term** gives place to a new set of producers. The list of all producers is ordered by vote and 2N+1 will be chosen to produce blocks in the next rounds. If a producer fails by not following the rules (whatever the reason) he is replaced with the next producer in the list of ordered producers.

Shuffling (rounds):

Rounds are the second largest time slot use by the consensus system. The main purpose is to randomize the order of block production to avoid any conspiracy by knowing in advance the order of production, thus providing an extra layer of security.

The randomness is based on the three following properties:
(1) the **in-value**: A random value which is a value inputted from the mining node and kept privately by the mining node itself in round. It will become public after all block generations in round are completed and the value is discarded.
 (2) the **out-value**: simply the hash of the in-value. Every node in the aelf network can look up this value at any time.
 (3) the random hash calculated based on the previous round signatures and the **in-value** of the miner in the current round.

The order is based on the random hash modulo the producer count. The order is dynamic because collisions can occur, this is of course perfectly normal.

Producing a block (slots):

A slot is the smallest time interval it corresponds to the time allocated to a block producer to producer a block. The producer will be processing other producers block until his time slot comes. When his time slot start the producer will pack some available transaction - it will be his turn to increase the chains height - and when finished will broadcast a block.
If a block producer misses a slot there’s simply no blocks produced at that time. The producer after him will produce the block at the next height. Eventually, if a producer misses many blocks he will be removed from the current producers.

Rules:

One important aspect of the rules of consensus is that they must be resistant to a certain amount of naturally occurring events such as network lag, faulty nodes and also malicious actors attempting to cheat the system. 

In the event that one or more of these problems occur the non-malicious nodes must still be able to reach consensus - meaning that they eventually will agree on which chain to follow. At least ⅓ of the nodes have to be honest and well functioning for the systems to work and as long this is true the block producers will always end up agreeing.

A node following AElf consensus will chose the longest chain in the event of forks, so this means that even if there is lag or even a network split, when the faulty nodes recovers a normal situation, they’ll switch to follow the longest chain. The longest chain will be produced by the largest group of block producers (more precisely, block producers following the same rules and agreeing on the same blocks).
It is possible for forks to have the same height, but this does not happen for very long since the schedule is random and there’s an uneven amount of producers, cause at least one of the forks to eventually grow higher.
