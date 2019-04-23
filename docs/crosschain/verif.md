## Cross chain verification

Verification is the key features that enables side chains. Because side chains do not have direct knowledge about other side chains they need a way to verify information from other chains. Side chains need the ability to verify that a transaction was included in another side chains block.

#### Indexing 

The role of the main chain node is to index all the side chains blocks. This way it knows exactly the current state of all the side chains. Side chains also index main chain blocks and this is how they can gain knowledge about the inclusion of transactions in other chains.

Indexing is a continuous process, the main chain is permanently gathering information from the side chains and the side chains are permanently getting information from the main chain. When a side chain wants to verify a transaction from another side chain it must wait until the correct main chain block has been indexed.

#### Merkle tree

Merkle tree is a basic binary tree structure. Node value (which is not a leaf node) is the hash calculated from its children values until to the tree root. 
<!-- TODO: maybe a structure demo is needed here. -->

#### Merkle roots

When a transaction gets included in a side chain's block the block will also include a merkle root of the transactions of this block. This root is local to this side chain's blockchain and by itself of little value to other side chains because they follow a different protocol. So communication between side chains goes through the main chain in the form of a merkle path. During indexing process, main chain is going to calculate the root with the data from side chains, and side chains in turn get the root in future indexing. This root is used for final check in cross chain transaction verification.

#### Merkle path

Merkle path is the node collection for one leaf node to calculate with to the root. Correct merkle path is necessary to complete any work related to cross chain verification. For the transaction ***tx*** from chain ***A***, you need the whole merkle path root for ***tx*** to calculate the final root if you want to verify the existence of this transaction on other chains, and verify the root by checking whether it is equals to the one got from indexing before.
<!-- TODO: maybe a structure demo is needed here. -->
