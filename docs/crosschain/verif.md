## Cross chain verification

Verification is the key features that enables side chains. Because side chains do not have direct knowledge about other side chains they need a way to verify information from other chains. Side chains need the ability to verify that a transaction was included in another side chains block.

#### Indexing 

The role of the mainchain node is to index all the side chains blocks. This way it knows exactly the current state of all the side chains. Side chains also index mainchain blocks and this is how they can gain knowledge about the inclusion of transactions in other chains.

Indexing is a continuous process, the main chain is permanently gathering information from the side chains and the side chains are permanently getting information from the mainchain. When a side chain wants to verify a transaction from another side chain it must wait until the correct mainchain block has been indexed.


#### Merkle roots

When a transaction gets included in a side chains block the block will also include a merkle root of the transactions of this block. This root is local to this side chains blockchain and by itself of little value to other side chains because they follow a different protocol. So communication between side chains goes through the main chain in the form of a merkle path.

