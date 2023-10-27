MultiToken Contract

Glossary

| English | Explanation | Details |
| --- | --- | --- |
| Lock white list  | Token lock allowlist  | If a system contract is added to the Lock white list when a token is created, it means that the system contract can lock the user's token in the MultiToken contract through the Lock method under certain conditions. |
| Transfer Callback  | Callback after transfer occurred  | This information is written in the external_info field of TokenInfo (which is a dictionary). There are three types in total, and each type has a different key: - Callbacks executed after each call to Transfer and TransferFrom with key aelf_transfer_callback - Callback executed after each call to Lock , key is aelf_lock_callback - Callback executed after each call to Unlock , key is aelf_unlock_callback Value can be resolved to CallbackInfo type, which means that after completing the corresponding operation, an inline transaction can be sent for the specified method of the specified contract. message CallbackInfo {     aelf.Address contract_address = 1;     string method_name = 2; } |

# Token related
## Create - Create
For information about tokens, follow the TokenInfo structure.
```
message TokenInfo {
// The symbol of the token.f
string symbol = 1;
// The full name of the token.
string token_name = 2;
// The current supply of the token.
int64 supply = 3;
// The total supply of the token.
int64 total_supply = 4;
// The precision of the token.
int32 decimals = 5;
// The address that created the token.
aelf.Address issuer = 6;
// A flag indicating if this token is burnable.
bool is_burnable = 7;
// The chain id of the token.
int32 issue_chain_id = 8;
// The amount of issued tokens.
int64 issued = 9;
// The external information of the token.
ExternalInfo external_info = 10;
}
```

| Property | Meaning | Value | Note |
| --- | --- | --- | --- |
| symbol | Token identification  | "ELF"  | In the MultiToken contract, symbol will be the unique identifier of the token The symbol of NFT adopts the XXX- {number} format, and number = 0 is considered as the collection of NFT. |
| token_name | Token name | "elf token" |  |
| supply | Token current supply | < 1, 000, 000, 000 | That is, the number that has been published through the Issue method |
| total_supply | Total Token Supply | 1, 000, 000, 000 | ELF published a total of 1 billion |
| decimals  | Exact decimal places of Token  | 8  | If decimals is 2, it means that 100 in the contract actually represents 1. The decimals of ELF are 8, which means that the 100,000,000 read in the contract represents an ELF. NFT type decimals must be 0 |
| issuer  | Publisher  | Economic system (Economic) contract address  | In fact, ELF will be published by the Economic contract in the genesis block, and the direct caller of the Issue method is the Economic contract (specifically, through the IssueNativeToken method). |
| is_burnable | Can it be destroyed | true | ELF can be destroyed. |
| issue_chain_id  | On which chain of aelf can the token be published? | 9992731 (Main Chain ChainId) | ELF can only be published on the main chain (and will be published at the beginning).  |
| issued | Tokens published | 1, 000, 000, 000 |  |
| external_info  | Token additional information | - | NFT Metadata can be defined , when the above properties can not cover the information contained in a class of Token, you need to use external_info this property to assist storage. In fact, the type of this attribute is a map. This field can be modified through the ResetExternalInfo method (the permission is in the Issuer) It is recommended to use __nft as the key of the nft attribute, such as __nft_image_url |

Any aelf account can create a new token type that meets the requirements of symbol when the transaction fee is sufficient. Creating a token itself is to add a TokenInfo instance in the State. TokenInfos of MultiToken, whose key is symbol.
The current rules for symbols are as follows:
- If the creator account is in the allowlist ( State. CreateTokenWhiteListMap ), you can create symbols of unlimited length containing letters A-Z and numbers 0-9.
- If the creator account is not in the allowlist, a symbol containing the letters A-Z with a length of up to 10 can be created.
  
# Publisher - Issues

When creating a token, you can specify an Issuer (see the table above ). The account designated as an Issuer can initialize the token and publish it.
As the token is published, the supply and issuance of TokenInfo will increase, and then the recipient's balance for the token will be changed from State. Balances .
By the way, the Issuer can be replaced using the ChangeTokenIssuer method.
NFT issues require the creation of a collection of NFTs and NFTs in advance.

# Transfer - Transfers & TransferFrom

Both Transfer and TransferFrom methods will cause the balance of the two accounts in State. Balances to be updated. The difference is that there is an additional From parameter in the imported parameter of TransferFrom, which identifies the account whose balance is reduced during this transfer - in Tranfer transactions, this account is always the transaction initiator (Context. Sender).
Successful execution of TransferFrom requires a prerequisite: the From account needs to authorize the amount of the TransferFrom transaction sponsor (Approve).
  
# Destruction

If the isBurnable field in TokenInfo is true, such tokens can be destroyed.
When a specified number of tokens are destroyed, the Supply field of TokenInfo will be reduced.

# Authorization and Disauthorization - Approve & UnApprove

In the blockchain, in order to reduce the frequent transfer operations of accounts, one account A is allowed to pre-authorize the specified amount of tokens for another account B: after that, B can transfer a part of the specified tokens from account A through the TransferFrom method at any time until the authorized limit is reached.
The authorized amount will be recorded in State. Allowances .
The UnApprove method is used to revoke authorization.

# Lock & Unlock - Lock & Unlock
  
These two methods only provide convenience for other system contracts (AssertSystemContractOrLockWhiteListAddress judgments) to lock user tokens: when the contract attempts to lock a user's token, it only needs to call the Lock method of the MultiToken contract across the contract; then the Unlock method can be called to return the user's token to the user.
The MultiToken contract actually assigns a virtual address to each lock position (introducing LockId to calculate fromVirtualAddress - > virtualAddress) to avoid mixing funds from different user lock positions.

# Cross-chain transfer

In the aelf mainnet, Tokens can only be created on the mainnet. After creation, TokenInfo will not be automatically synchronized to each sidechain, and can only be manually synchronized.
- First, construct a proof on the mainnet through ValidateTokenInfoExists method, which will automatically synchronize Merkle Root from the main chain to the sidechain with the main sidechain's Indexing mechanism.
- Then on the sidechain, through CrossChainCreateToken method, verify that a TokenInfo does exist on the main chain, and this TokenInfo information can be added to the MultiToken contract of the sidechain.

After synchronizing the TokenInfo information of the main chain to the sidechains, cross-chain transfers can be completed between the main sidechains and sibling sidechains (through the CrossChainTransfer method and the CrossChainReceiveToken method).
See aelf跨链机制和侧链for specific process.

# ACS1: Fee related
In the charging process, it involves two methods ChargeTransactionFees and ClaimTransactionFees, and the transactions corresponding to these two methods are automatically generated. ChargeTransactionFees generated by the transaction execution Pre-plugin, ClaimTransactionFees generated by the system transaction generator.
See ACS1 Method Fees收取过程.

In addition to ChargeTransactionFees and ClaimTransactionFees, methods related to charging logic include:
- InitialCoefficients: Initialize the parameters of the formula for calculating costs
- SetSymbolsToPayTxSizeFee: Set the token list that can be used to pay fees
- UpdateCoefficientsForSender: Update parameters of ACS1 size fee calculation formula

There is a method IsStopExecuting in the definition of the IPreExecutionPlugin interface, which is also used to determine whether to stop the execution of the original transaction if the Pre-plugin transaction fails.
ACS5 (see below) can directly cause Pre-plugin to throw an AssertionException, which will cause the transaction to fail directly, so there is no focus on implementing this interface.
But ACS1 is different, ACS1's Pre-plugin generated ChargeTransactionFees transaction is to be executed successfully , if a transaction to the time of packaging to execute failure (that is, through the pre-validation), it will still be packaged, and deduct fees, but in the deduction of fees at the same time, do not execute the original transaction.
ACS1 and ACS5 can of course be mixed, and ACS1 will be executed first (because ExecutionPluginForMethodFeeModule registered before ExecutionPluginForCallThresholdModule), and the transaction fee will be charged before ACS5 is executed.
If a transaction is determined to be blocked by a Pre-plugin transaction, the execution result will still be retained and the StateDb will be modified. In this scenario, the ACS1 fee will be charged. (See PlainTransactionExecutingService. GetTransactionResult, if the IsExecutionStoppedByPrePlugin is met, the event will be placed in the Logs of the transaction result and the bloom filter will be updated.)

# ACS5: Contract Method Call Threshold Related
For contracts that implement ACS5, a call threshold can be set for each method in the contract, such as requiring the caller's X token balance to be no less than Y (or requiring the caller's X token balance and the authorization amount of the contract to be no less than Y).

ACS5 defines two methods:
- SetMethodCallingThreshold, used to set the threshold for calling a certain method in the contract
- GetMethodCallingThreshold: Used to obtain the call threshold of a method set by the contract
  A MethodCallingThreshold structure is also defined:

```
message MethodCallingThreshold {
// The threshold for method calling, token symbol -> amount.
map<string, int64> symbol_to_amount = 1;
// The type of threshold check.
ThresholdCheckType threshold_check_type = 2;
}

enum ThresholdCheckType {
// Check balance only.
BALANCE = 0;
// Check balance and allowance at the same time.
ALLOWANCE = 1;
}
```

When any other contract implements the ACS5 standard, when the method of the contract is executed, a pre-plugin transaction will be generated through MethodCallingThresholdPreExecutionPlugin GetPreTransactionsAsync (similar to the pre-plugin mechanism of ACS1).
The information for this transaction is:
- From: The address of the Sender of the original transaction
- To: the address of the MultiToken contract
- MethodName：CheckThreshold
- Params: A CheckThresholdInput instance:

```
  message CheckThresholdInput {
  // The sender of the transaction.
  aelf.Address sender = 1;
  // The threshold to set, Symbol->Threshold.
  map<string, int64> symbol_to_threshold = 2;
  // Whether to check the allowance.
  bool is_check_allowance = 3;
  }
```

- Sender: Whose account do you want to verify?
- symbol_to_threshold: the token type and the token corresponding to the call threshold amount map, as long as the user meets one of the thresholds, it is passed
- is_check_allowance: In addition to checking the account token balance, do you also need to check the authorization amount of the account for the contract to be executed (see Approve/UnApprove method)?

CheckThreshold is the method used by MultiToken contracts to verify the threshold for contract calls.
1. The SymbolToThreshold in the imported parameter is obtained by reading the contract to be executed in the MethodCallingThresholdPreExecutionPlugin GetPreTransactionsAsync when generating the CheckThreshold transaction.
2. Determine the list of tokens that meet the balance requirements one by one according to SymbolToThreshold, and record them as meetBalanceSymbolList;
3. Then judge the list of tokens that meet the authorization limit from the meetBalanceSymbolList one by one. If any of them meet (or do not need to check the authorization limit), set meetThreshold to true.
4. If meetThreshold is not true, an AssertionException is thrown, causing the original transaction to fail together.

# ACS8: Resource cost related
The contract that implements ACS8 (in fact, it cannot be called implementation, as long as the proto file of a contract declares its base with acs8.proto), the execution of each method will deduct the resource coin of this contract address .
The resource coins that may be deducted in this scenario include: WRITE, READ, STORAGE, TRAFFIC.
Since the deduction is based on the resources consumed during the actual execution of the original transaction (such as reading several keys, writing several keys, etc.), the core logic of the ACS8 mechanism is in the post-plugin transaction: after the original transaction is executed, a transaction will be generated and executed after all inline transactions are executed.
Since the mechanisms of Pre-plugin and Post-plugin are very similar, many introductions have been made in the documentation of ACS1 and ACS5. This section only briefly points out the code locations related to ACS8.

## Post-plugin transaction: ChargeResourceToken
Transaction generated: ResourceConsumptionPostExecutionPlugin GetPostTransactionsAsync

Transaction parameters:
- From: The address of the contract to be called in the original transaction
- To: MultiToken contract address
- MethodName：ChargeResourceToken
- Params: An example ChargeResourceTokenInput:

```
  message ChargeResourceTokenInput {
  // Collection of charge resource token, Symbol->Amount.
  map<string, int64> cost_dic = 1;
  // The sender of the transaction.
  aelf.Address caller = 2;
  }
```

- cost_dic: The cost of each token calculated based on the resources consumed during the actual execution of the original transaction
- Caller: Sender of the original transaction
  
Among them, the value of the cost_dic is similar to the logic of obtaining the value of the SymbolsToPayTxSizeFee field when the ACS1 mechanism generates ChargeTransactionFees transactions, involving the setting, updating and off-chain calculation of the calculation formula. Finally, the cost is calculated through the ResourceTokenFeeService. CalculateFeeAsync method. ·

This ResourceTokenCharged event is handled in ResourceTokenChargedLogEventProcessor. ProcessAsync, putting data from a TotalResourceTokensMaps instance into ExecutedData using TotalResourceTokensMapsProvider. SetTotalResourceTokensMapsAsync method.
Actual completed transaction fee charged: DonateResourceToken
In the next block production process, BP will generate a DonateResourceToken transaction through DonateResourceTransactionGenerator GenerateTransactionsAsync. Obtain totalResourceTokensMaps instance from the TotalResourceTokensMapsProvider method as an imported parameter of the DonateResourceToken transaction.
After receiving the block, other nodes use DonateResourceTokenValidationProvider ValidateBlockAfterExecuteAsync to verify whether the execution result of DonateResourceToken transaction matches the local resource coin fee calculation result.
During the execution of DonateResourceToken, it may be found that the balance of resource coins in the contract is insufficient. At this time, the debt of the contract will be recorded in the State. OwningResourceToken , and a ResourceTokenOwned event will be thrown.
What to do if there is a debt in the contract? Just don't let it be executed in the future. This logic is implemented using a Pre-plugin transaction.

## Pre-plugin transaction: CheckResourceToken
Generated by the ResourceConsumptionPreExecutionPlugin GetPreTransactionsAsync method before the original transaction is executed.
This transaction is only used to check whether there is a debt in the contract. If there is a debt, an AssertionException will be thrown, and all transactions of this contract cannot be executed.
This means that the developer (or operator) of the contract needs to advance resource coins to the contract address in a timely manner.
Advance resource coins can be directly deposited into the contract address, but in this case, the advanced account will not be able to retrieve the advanced resource coins. In order to record how much resource coins an account has advanced, AdvanceResourceToken and TakeResourceTokenBack methods have been added.

## Advance and Recover Resource Coins: AdvanceResourceToken & TakeResourceTokenBack
AdvanceResourceToken is used to advance resource coins, and Approve is not required before use (after all, MultiToken contracts can change State. Balances by themselves). There are two logics: modify the State. AdvancedResourceToken based on the number of resource coins to be advanced, and then complete DoTransfer to transfer the balance of resource coins.
TakeResourceTokenBack, based on the advance data of State.AdvancedResourceToken query Sender, and then return the resource coin to the advance account (the return quantity is the specified quantity in the input), of course, can not take more, otherwise it will report an error: Can't take back that more.. .
