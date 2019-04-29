# Thoughts on the Interoperability of AElf

When we talk about interoperability amongst blockchains, the first question we must answer is:

> "How does an action on one blockchain trigger a predetermined action on another blockchain, in a reliable and incorruptible manner?"

In its simplest form, we can consider potential interoperability between Ethereum and Bitcoin. We could write a piece of code to monitor events on Ethereum smart contracts by using the proper libraries, such as [web3js](https://github.com/ethereum/web3.js/) or [JSONRPC](https://github.com/ethereum/wiki/wiki/JSON-RPC). This code would check if a given contract has been called and verified as valid, and if this transaction has been confirmed for more than 12 or 36 times on its block-height, then the program can safely trigger predetermined actions on the Bitcoin blockchain by creating a signed [raw transaction](https://en.bitcoin.it/wiki/Raw_Transactions), and broadcasting it to the Bitcoin network.

One advantage of this 'adapter' style of blockchain interoperability is its simplicity. Basically, we can write as many adapters as we want to connect with different blockchains, broadcast the transactions to any one of them, and even trigger cascaded actions. For example, if I deposit Ether to one specific smart contract on Ethereum, the monitor can trigger actions on the Bitcoin blockchain to pay the bill with the [Lighting Network](https://lightning.network/) to some service provider, which finally triggers the movement of a tiny cute android connected to the IOTA network. Yes, it appears that the '**adapter way**' eliminates the boundaries of different blockchains and works like a charm, except for one major drawback - it is centralized.

Let’s think twice on a critical assumption in the scenario above. We laid our trust on the centralized monitor program - the 'middleman'. We trust this middleman for his guaranteed behavior to ensure predetermined actions occur on other blockchains. But what if he doesn’t keep his promise? What if a link in the chain of blockchains has broken?

We can say with certainty: this process is not '**atomic**' from a technique perspective.

If the counterparties in different blockchains want to exchange assets, and meanwhile they don’t trust each other, there has to be a middleman they both trust. Think about how you buy and sell assets on a centralized exchange, e.g. Bittrex - we somewhat lay trust on these exchanges, intentionally or unconsciously.

Decentralized exchanges \(DEX\) are becoming more common every day. Projects like [0x](https://0xproject.com/), [Kyber Network](https://kyber.network/) and [AirSwap](https://www.airswap.io/) have won great attention from the public. Generally speaking, DEX is a special use case in cross-chain interoperability. The idea behind DEX is called an '**atomic swap**', i.e. we swap assets without third parties, and are provided '**end-to-end**' security in token exchanges \(or at least we hope we are\).

In AElf, making the whole process automatic, guaranteed, and decentralized is our goal.

The '**mainchain**' endorsement mechanism we are going to set up in AElf is a Delegated Proof-of-Stake \(DPoS\), a sort of 'decentralized middleman' who can provide '**trust**' for each sidechain in the AElf ecosystem. All AElf sidechains can lay their trust on the '**mainchain**' for cross-chain transactions. The sidechain can provide a proof that can be verified through the mainchain, enabling other sidechains to confidently trust the corresponding transaction has indeed happened. The whole process of a cross-chain transaction can be formalized as below:

1. A transaction happens on the sidechain A.
2. A node of sidechain A broadcasts the transaction to nodes in the **mainchain** to record this transaction with minimal required information, i.e., the Merkle root + block header which is unforgeable \(or as difficult as mining a block\).
3. The node of sidechain A actively makes a function call \(broadcast tx\) to the corresponding method from another sidechain B with a proof — i.e., the Merkle proof.
4. The method in nodes of sidechain B tries to verify the proof on the '**mainchain**', which both sidechains trust, and execute the corresponding actions if verified valid.

This process is actually an '**active**' process. If we trust the smart contracts \(open source\) on both sidechains then we can confirm that the above procedure will definitely run as expected - automatically and controlled by the one who initiated the transaction.

As we can see from above, Merkle proofs have played an important part in cross-chain transactions.

Let’s talk a little more about the Merkle proof:

In cryptography, a '**Merkle proof**' is a kind of signature proof which can be used to prove that a given transaction exists in the target block. A Merkle proof contains:

\(Merkle root of the block, hash1, hash2,…. hashN\)

Basically, it’s a list of hashes as an evidence taken from the Merkle tree to prove the existence of a transaction.

The security of Merkle proofs rely upon the computational infeasibility of hash collision, i.e. you cannot possibly fake a meaningful transaction whose transaction hash can be computed the same as the previous one.

Generally speaking, once the Merkle root in a sidechain has been included in the mainchain, we can conclude that all the transactions in that block has been confirmed by the '**mainchain**'. Meanwhile, the mainchain is kept decentralized with DPoS.

