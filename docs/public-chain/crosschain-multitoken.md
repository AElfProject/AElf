## crosschain multitoken

AElf's public chain eco-system is designed to be cross-chain. This multi-chain structure of AElf is based on a core concept that is one chain for one business concept. Side-chain's can have their own token and also share tokens in order to participate in a cross-chain economy.

### Introduction 

Interbank transfer is an example of breaking the independence of closed systems around us, closed systems have a high motivation and need to breakthrough. Here are some main requirements for this breakthrough:
- Cross-chain transactions are very similar to the interbank transfer we just mentioned. Assets are transferred from one chain to another. If you have assets on chain A, I am on chain B, it's essential we must be able to complete the exchange.
- The more important demand derived from asset exchange is the decentralized exchange. Most exchanges are currently operated by a company that is completely centralized.
- Resource isolation and chain expansion.
- Asset mapping, many projects’ ERC20 token are on Ethereum, which needs to be mapped to its own chain at a later stage. If there is no cross-chain technology, this process will need to be completed by a centralized organization.
- Unexplored scenarios, this is a requirement that we have not yet discovered. With the gradual maturity of cross-chain technology, there will be more and more cross-chain scenarios and as such, it must be flexible enough to implement a wide variety of scenarios.

### Technical aspect

In order to deploy a side-chain a proposal must be issues in the Parliament. Votes will be accumulated and eventually the proposal approved and released. The release sets up state in the crosschain contract and the token contract. After this a link is created between the side chain and the main and indexing will start. Every cross-chain contract implements ACS7 that is the standard that defines cross-chain operations that enable cross chain transfer functionality.

Here's a high level overview of the process of token creation and cross chain transfer:
 - the first step involves creating and issue on the token contract of the main chain. A transaction also has to be sent to the side chain's token contract to register the token.
 - transfer transactions can start: first transfer from the main-chain to the side-chain by calling the main-chain's token contract CrossChainTransfer method.
 - in order to complete the transfer, the side-chain must receive the tokens by calling the CrossChainReceiveToken method of the token contract.

