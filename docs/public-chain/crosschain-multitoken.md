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

Every cross-chain contract implements ACS7 witch is the standard that defines cross-chain oper

  - Side chain validates main chain multi-token contract address
   - Main chain: ValidateSystemContractAddress，GenesisContract
   - Side chain: RegisterCrossChainTokenContractAddress，MultiTokenContract

  - Main chain validates side chain multi-token contract address
   - Side chain: ValidateSystemContractAddress，GenesisContract
   - Main chain: RegisterCrossChainTokenContractAddress，MultiTokenContract

- Main chain to Side chain transfer
 - Create token
  - Main chain: Create & Issue, MultiTokenContract
  - Side chain: CrossChainCreateToken, MultiTokenContract
 
 - Transfer
  - Main chain: CrossChainTransfer, MultiTokenContract

 - Receive
  - Side chain: CrossChainReceiveToken, MultiTokenContract

