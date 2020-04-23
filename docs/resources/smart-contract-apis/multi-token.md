# multi-token

The multi-token contract is most essentially used for managing balances.

## Token life-cycle: creation, issuance and transfer.

These methods constitute the basic functionality needed to maintain balances for tokens. For a full listing of the contracts methods you can check the [Token Contract definition](https://github.com/AElfProject/AElf/blob/master/protobuf/token_contract.proto) on GitHub.

### **Create**

```Protobuf
rpc Create (CreateInput) returns (google.protobuf.Empty) { }

message CreateInput {
    string symbol = 1;
    string token_name = 2;
    sint64 total_supply = 3;
    sint32 decimals = 4;
    aelf.Address issuer = 5;
    bool is_burnable = 6;
    repeated aelf.Address lock_white_list = 7;
    bool is_profitable = 8;
    int32 issue_chain_id = 9;
}
```

The token contract permits the creation of an entirely new token and the first action needed before using a token is its creation. The **Create** method takes exactly on parameter, a **CreateInput** message.

- **issuer** is the creator of this token.
- **symbol** is a short string between 1 and 8 characters composed only of upper-case letters like for example "ELF" or "AETC" (no numbers allowed). Of course, since tokens are uniquely identified by the symbol, it must not already exist.
- **token_name** is a more descriptive name for your token or the long name. For example, "RMB" could be the token symbol and "RenMinBi" the token's name. This is a non-optional field up to 80 characters in length. 
- **total_supply** for the token is the amount of tokens that will exist. This must be larger than 0.
- **decimals** is a positive integer between 0-18.
- **issue_chain_id** is the id of the chain, this defaults to the chain id of the node.

### **Issue**

```Protobuf
rpc Issue (IssueInput) returns (google.protobuf.Empty) { }

message IssueInput {
    string symbol = 1;
    sint64 amount = 2;
    string memo = 3;
    aelf.Address to = 4;
}
```

Issuing some amount of tokens to an address is the action of increasing that addresses balance for the given token. The total amount of issued tokens must not exceed the total supply of the token and only the issuer (creator) of the token can issue tokens. Issuing tokens effectively increases the circulating supply. The **Issue** method takes exactly one parameter, a **IssueInput** message.

- **symbol** is the symbol that identifies the token, it must exist.
- **amount** is the amount to issue.
- **to** field the receiver address of the newly issued tokens.
- **memo** optionally you can specify a later accessible when parsing the transaction. 

### **Transfer**

```Protobuf
rpc Transfer (TransferInput) returns (google.protobuf.Empty) { }

message TransferInput {
    aelf.Address to = 1;
    string symbol = 2;
    sint64 amount = 3;
    string memo = 4;
}
```

Transferring tokens simply is the action of transferring a given amount of tokens from one address to another. The origin or source address is the signer of the transaction. The balance of the sender must be higher than the amount that is transferred.
The **Transfer** method takes exactly one parameter, a **TransferInput** message.

- **to** field is the receiver of the tokens.
- **symbol** is the symbol that identifies the token, it must exist.
- **amount** is the amount to to transfer.
- **memo** optionally you can specify a later accessible when parsing the transaction. 

### **TransferFrom**

```Protobuf
rpc TransferFrom (TransferFromInput) returns (google.protobuf.Empty) { }

message TransferFromInput {
    aelf.Address from = 1;
    aelf.Address to = 2;
    string symbol = 3;
    sint64 amount = 4;
    string memo = 5;
}
```

The **TransferFrom** action will transfer a specified amount of tokens from one address to another. For this operation to succeed the **from** address needs to have approved (see *allowances*) enough tokens to Sender of this transaction. If successful the amount will be removed from the allowance.

- **from** the source address of the tokens.
- **to** the destination address of the tokens.
- **symbol** the symbol of the token to transfer.
- **amount** the amount to transfer.
- **memo** an optional memo.

## Allowances.

Allowances allow some entity (in fact an address) to authorize another address to transfer tokens on his behalf (see **TransferFrom**). There are two methods available for controlling this, namely **Approve** and **UnApprove**.

### **Approve**

``` Proto
rpc Approve (ApproveInput) returns (google.protobuf.Empty) { }

message ApproveInput {
    aelf.Address spender = 1;
    string symbol = 2;
    sint64 amount = 3;
}
```

The approve action increases the allowance from the *Sender* to the **Spender** address, enabling the Spender to call **TransferFrom**.

- **spender** the address that will have it's allowance increased.
- **symbol** the symbol of the token to approve.
- **amount** the amount of tokens to approve.

### **UnApprove**

``` Proto
rpc UnApprove (UnApproveInput) returns (google.protobuf.Empty) { }

message UnApproveInput {
    aelf.Address spender = 1;
    string symbol = 2;
    sint64 amount = 3;
}
```

This is the reverse operation for **Approve**, it will decrease the allowance.

- **spender** the address that will have it's allowance decreased.
- **symbol** the symbol of the token to un-approve.
- **amount** the amount of tokens to un-approve.

## Locking.

### **Lock**

``` Proto
rpc Lock (LockInput) returns (google.protobuf.Empty) { }

message LockInput {
    aelf.Address address = 1;
    aelf.Hash lock_id = 2;
    string symbol = 3;
    string usage = 4;
    int64 amount = 5;
}
```

This method can be used to lock tokens.

- **address** the entity that wants to lock its tokens.
- **lock_id** id of the lock. 
- **symbol** the symbol of the token to lock.
- **usage** a memo.
- **amount** the amount of tokens to lock.

### **Unlock**

``` Proto
rpc Unlock (UnlockInput) returns (google.protobuf.Empty) { }

message UnlockInput {
    aelf.Address address = 1; // The one want to lock his token.
    aelf.Hash lock_id = 2;
    string symbol = 3;
    string usage = 4;
    int64 amount = 5;
}
```

This is the reverse operation of locking, it un-locks some previously locked tokens.

- **address** the entity that wants to un-lock its tokens.
- **lock_id** id of the lock. 
- **symbol** the symbol of the token to un-lock.
- **usage** a memo.
- **amount** the amount of tokens to un-lock.

## Burning tokens.

### **Burn**

``` Proto
rpc Burn (BurnInput) returns (google.protobuf.Empty) { }

message BurnInput {
    string symbol = 1;
    sint64 amount = 2;
}
```

This action will burn the specified amount of tokens, removing them from the token's *Supply*

- **symbol** the symbol of the token to burn.
- **amount** the amount of tokens to burn.

## Cross-chain

### **CrossChainCreateToken**

```Proto
rpc CrossChainCreateToken(CrossChainCreateTokenInput) returns (google.protobuf.Empty) { }

message CrossChainCreateTokenInput {
    int32 from_chain_id = 1;
    int64 parent_chain_height = 2;
    bytes transaction_bytes = 3;
    aelf.MerklePath merkle_path = 4;
}
```

This action is used for creating a "cross-chain" token. This action should be called on the side-chain's with the information about the transaction that created the token on the parent chain.

- **from_chain_id** the chain id of the chain on which the token was created.
- **parent_chain_height** the height of the transaction that created the token.
- **transaction_bytes** the transaction that created the token.
- **merkle_path** the merkle path created from the transaction that created the transaction.

### **CrossChainTransfer**

```Proto
rpc CrossChainTransfer (CrossChainTransferInput) returns (google.protobuf.Empty) { }

message CrossChainTransferInput {
    aelf.Address to = 1;
    string symbol = 2;
    sint64 amount = 3;
    string memo = 4;
    int32 to_chain_id = 5;
    int32 issue_chain_id = 6;
}
```

This action is used for transferring tokens across chains, this effectively burn the tokens on the chain.

- **to** the receiving account.
- **symbol** the token.
- **amount** the amount of tokens that will be transferred.
- **memo** an optional memo.
- **to_chain_id** the destination chain id.
- **issue_chain_id** the source chain id.

### **CrossChainReceiveToken**

```Proto
rpc CrossChainReceiveToken (CrossChainReceiveTokenInput) returns (google.protobuf.Empty) { }

message CrossChainReceiveTokenInput {
    int32 from_chain_id = 1;
    int64 parent_chain_height = 2;
    bytes transfer_transaction_bytes = 3;
    aelf.MerklePath merkle_path = 4;
}
```

This method is used on the destination chain for receiving tokens after a **Transfer** operation.

- **from_chain_id** the source chain.
- **parent_chain_height** the height of the transfer transaction.
- **transfer_transaction_bytes** the raw bytes of the transfer transaction.
- **merkle_path** the merkle path created from the transfer transaction. 

### **SetSymbolsToPayTxSizeFee**

```Proto

rpc SetSymbolsToPayTxSizeFee (SymbolListToPayTxSizeFee) returns (google.protobuf.Empty){
}

message SymbolListToPayTxSizeFee{
    repeated SymbolToPayTxSizeFee symbols_to_pay_tx_size_fee = 1;
}

message SymbolToPayTxSizeFee{
    string token_symbol = 1;
    sint32 base_token_weight = 2;
    sint32 added_token_weight = 3;
}
```
This action sets available tokens that can be used to pay for transaction fee.

- **symbols_to_pay_tx_size_fee** available token list.
  - **token_symbol** token symbol.
  - **base_token_weight** it is fixed to primary token.  
  - **added_token_weight** if base_token_weight set to 1 and added_token_weight set to 10, it will cost 10 this token instead of primary token.

### **UpdateCoefficientsForContract**

``` Proto
message UpdateCoefficientsInput {
    repeated sint32 piece_numbers = 1;// To specify pieces gonna update.
    CalculateFeeCoefficients coefficients = 2;
}

message CalculateFeeCoefficients {
    sint32 fee_token_type = 1;
    repeated CalculateFeePieceCoefficients piece_coefficients_list = 2;
}

message CalculateFeePieceCoefficients {
    repeated sint32 value = 1;
}

enum FeeTypeEnum {
    READ = 0;
    STORAGE = 1;
    WRITE = 2;
    TRAFFIC = 3;
    TX = 4;
}
```

This action sets methods used to calculate resource token fees.

- **fee_token_type** resource fee type (exclude TX).
- **piece_coefficients_list** it is a coefficients array.
  - **value** it is a int array. its first element indicates its piece key. other every three consecutive elements indicates a function, like (2, 1, 1) means (1/1) * x^2.

### **UpdateCoefficientsForSender**

``` Proto
rpc UpdateCoefficientsForSender (UpdateCoefficientsInput) returns (google.protobuf.Empty) {
}

message UpdateCoefficientsInput {
    repeated sint32 piece_numbers = 1;// To specify pieces gonna update.
    CalculateFeeCoefficients coefficients = 2;
}

message CalculateFeePieceCoefficients {
    repeated sint32 value = 1;
}
```

This action sets methods used to calculate transaction fee.

note: *for CalculateFeeCoefficients see UpdateCoefficientsForContract*

### **AdvanceResourceToken**

``` Proto
rpc AdvanceResourceToken (AdvanceResourceTokenInput) returns (google.protobuf.Empty) {
}

message AdvanceResourceTokenInput {
    aelf.Address contract_address = 1;
    string resource_token_symbol = 2;
    sint64 amount = 3;
}
```

This action transfers resource tokens to designated contract address.

- **contract_address** the contract address.
- **resource_token_symbol** resource token symbol.
- **amount** the amount of tokens.

### **TakeResourceTokenBack**

``` Proto
rpc TakeResourceTokenBack (TakeResourceTokenBackInput) returns (google.protobuf.Empty) {
}

message TakeResourceTokenBackInput {
    aelf.Address contract_address = 1;
    string resource_token_symbol = 2;
    sint64 amount = 3;
}
```

This method takes token from contract address

- **contract_address** the contract address.
- **resource_token_symbol** resource token symbol.
- **amount** the amount of tokens.

### **ValidateTokenInfoExists**

``` Proto
rpc ValidateTokenInfoExists(ValidateTokenInfoExistsInput) returns (google.protobuf.Empty){
}

message ValidateTokenInfoExistsInput{
    string symbol = 1;
    string token_name = 2;
    sint64 total_supply = 3;
    sint32 decimals = 4;
    aelf.Address issuer = 5;
    bool is_burnable = 6;
    sint32 issue_chain_id = 7;
    bool is_profitable = 8;
}
```

This method validates if the token exist.

- **symbol** the token symbol.
- **token_name** the token name.
- **total_supply**  total supply of the token.
- **decimals** decimals.
- **issuer** the token issuer.
- **is_burnable** indicates if the token is burnable.
- **issue_chain_id** issue chain id.
- **is_profitable** indicates if the token is profitable.

### **TransferToContract**

``` Proto
rpc TransferToContract (TransferToContractInput) returns (google.protobuf.Empty) {
}

message TransferToContractInput {
    string symbol = 1;
    sint64 amount = 2;
    string memo = 3;
}
```

This method transfer token to token address.

- **symbol** the token symbol.
- **amount** amount.
- **memo** transfer memo.

### **InitializeAuthorizedController**

``` Proto
rpc InitializeAuthorizedController(google.protobuf.Empty) returns (google.protobuf.Empty){
}
```

This method initializes the controller for calling UpdateCoefficientsForContract and UpdateCoefficientsForSender. Note that, if the current chain is side chain, it will create a controller for managing chain rental. 

### **ChangeUserFeeController**

``` Proto
rpc ChangeUserFeeController (acs1.AuthorityInfo) returns (google.protobuf.Empty) {}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

 **AuthorityInfo**:
- **contract address**: controller type.
- **owner address**: controller's address.

This method change the controller who sets the coefficient for calculating transaction size fee.

### **ChangeDeveloperController**

``` Proto
rpc ChangeDeveloperController (acs1.AuthorityInfo) returns (google.protobuf.Empty) {}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

**AuthorityInfo**:
- **contract address**: controller type.
- **owner address**: controller's address.

This method change the controller who sets the coefficient for calculating resource token.
## View methods

### **GetTokenInfo**

``` Proto
rpc GetTokenInfo (GetTokenInfoInput) returns (TokenInfo) { }

message GetTokenInfoInput {
    string symbol = 1;
}

message TokenInfo {
    string symbol = 1;
    string token_name = 2;
    sint64 supply = 3;
    sint64 total_supply = 4;
    sint32 decimals = 5;
    aelf.Address issuer = 6;
    bool is_burnable = 7;
    bool is_profitable = 8;
    sint32 issue_chain_id = 9;
    sint64 burned = 10;
}
```

This view method returns a **TokenInfo** object that describes information about a token.

Input:
- **symbol** the token for which you want the information.

Output:
- **symbol** the symbol of the token.
- **token_name** the full name of the token.
- **supply** the current supply of the token.
- **total_supply** the total supply of the token.
- **decimals** the amount of decimal places this token has.
- **issuer** the address that created the token.
- **is_burnable** a flag indicating if this token is burnable.
- **is_profitable** a flag indicating if this token is profitable.
- **issue_chain_id** the chain of this token.
- **burned** the amount of burned tokens.

### **GetNativeTokenInfo**

``` Proto
rpc GetNativeTokenInfo (google.protobuf.Empty) returns (TokenInfo) { }
```

note: *for TokenInfo see GetTokenInfo*

This view method returns the TokenInfo object associated with the native token.


### **GetResourceTokenInfo**

``` Proto
rpc GetResourceTokenInfo (google.protobuf.Empty) returns (TokenInfoList) { }

message TokenInfoList {
    repeated TokenInfo value = 1;
}
```

note: *for TokenInfo see GetTokenInfo*

This view method returns the list of TokenInfo objects associated with the chain's resource tokens.


### **GetBalance**

``` Proto
rpc GetBalance (GetBalanceInput) returns (GetBalanceOutput) { }

message GetBalanceInput {
    string symbol = 1;
    aelf.Address owner = 2;
}

message GetBalanceOutput {
    string symbol = 1;
    aelf.Address owner = 2;
    sint64 balance = 3;
}
```

This view method returns the balance of an address.

Input: 
- **symbol** the token for which to get the balance.
- **owner** the address for which to get the balance.

Output:
- **symbol** the token for which to get the balance.
- **owner** the address for which to get the balance.
- **balance** the current balance.

### **GetAllowance**

``` Proto
rpc GetAllowance (GetAllowanceInput) returns (GetAllowanceOutput) { }

message GetAllowanceInput {
    string symbol = 1;
    aelf.Address owner = 2;
    aelf.Address spender = 3;
}

message GetAllowanceOutput {
    string symbol = 1;
    aelf.Address owner = 2;
    aelf.Address spender = 3;
    sint64 allowance = 4;
}
```

This view method returns the allowance of one address to another.

Input: 
- **symbol** the token for which to get the allowance.
- **owner** the address for which to get the allowance (that approved tokens).
- **spender** the address of the spender.

Output:
- **symbol** the token for which to get the allowance.
- **owner** the address for which to get the allowance (that approved tokens).
- **spender** the address of the spender.
- **allowance** the current allowance.

### **IsInWhiteList**

``` Proto
rpc IsInWhiteList (IsInWhiteListInput) returns (google.protobuf.BoolValue) { }

message IsInWhiteListInput {
    string symbol = 1;
    aelf.Address address = 2;
}
```

This method returns wether or not the given address is in the lock whitelist.

- **symbol** the token.
- **address** the address that is checked.

### **GetLockedAmount**

``` Proto
rpc GetLockedAmount (GetLockedAmountInput) returns (GetLockedAmountOutput) { }

message GetLockedAmountInput {
    aelf.Address address = 1;
    string symbol = 2;
    aelf.Hash lock_id = 3;
}

message GetLockedAmountOutput {
    aelf.Address address = 1;
    string symbol = 2;
    aelf.Hash lock_id = 3;
    sint64 amount = 4;
}
```

This view method returns the amount of tokens currently locked by an address.

Input:
- **address** the address.
- **symbol** the token.
- **lock_id** the lock id.

Output:
- **address** the address.
- **symbol** the token.
- **lock_id** the lock id.
- **amount** the amount currently locked by the specified address.

### **GetCrossChainTransferTokenContractAddress**

``` Proto
rpc GetCrossChainTransferTokenContractAddress (GetCrossChainTransferTokenContractAddressInput) returns (aelf.Address) { }

message GetCrossChainTransferTokenContractAddressInput {
    int32 chainId = 1;
}
```

This view method returns the cross-chain transfer address for the given chain.

- **chainId** the id of the chain.

### **GetPrimaryTokenSymbol**

``` Proto
rpc GetPrimaryTokenSymbol (google.protobuf.Empty) returns (google.protobuf.StringValue) { 
```

This view method return the primary token symbol if it's set. If not, returns the Native symbol. 

### **GetCalculateFeeCoefficientOfContract**

``` Proto
rpc GetCalculateFeeCoefficientForContract (aelf.SInt32Value) returns (CalculateFeeCoefficients) { }

message CalculateFeeCoefficients {
    sint32 fee_token_type = 1;
    repeated CalculateFeePieceCoefficients piece_coefficients_list = 2;
}

message CalculateFeePieceCoefficients {
    repeated sint32 value = 1;
}

enum FeeTypeEnum {
    READ = 0;
    STORAGE = 1;
    WRITE = 2;
    TRAFFIC = 3;
    TX = 4;
}
```

This view method returns the resource tokens fee calculation method.

Input
resource fee type.

Output
note: *for CalculateFeeCoefficients see UpdateCoefficientsForContract*


### **GetCalculateFeeCoefficientOfSender**

``` Proto
rpc GetCalculateFeeCoefficientForSender (google.protobuf.Empty) returns (CalculateFeeCoefficients) { }

```

This view method returns transaction fee's calculation method.

note: *for CalculateFeeCoefficients see GetCalculateFeeCoefficientForContract*

### **GetSymbolsToPayTxSizeFee**

``` Proto
rpc GetSymbolsToPayTxSizeFee (google.protobuf.Empty) returns (SymbolListToPayTxSizeFee){
    option (aelf.is_view) = true;
}

message SymbolListToPayTxSizeFee{
    repeated SymbolToPayTxSizeFee symbols_to_pay_tx_size_fee = 1;
}

message SymbolToPayTxSizeFee{
    string token_symbol = 1;
    sint32 base_token_weight = 2;
    sint32 added_token_weight = 3;
}

```

This method returns available tokens that can be used to pay for transaction fee.

note: *for SymbolListToPayTxSizeFee see SetSymbolsToPayTxSizeFee*

### **GetDeveloperFeeController**

``` Proto
rpc GetDeveloperFeeController (google.protobuf.Empty) returns (DeveloperFeeController) {
}

message DeveloperFeeController {
    acs1.AuthorityInfo root_controller = 1;
    acs1.AuthorityInfo parliament_controller = 2;
    acs1.AuthorityInfo developer_controller = 3;
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

This method returns the controller for UpdateCoefficientsForContract. The root address consists originally of default parliament organization, developer organization. The type of root controller and developer controller is Assocation.

- **root_controller** root controller information.
- **parliament_controller** parliament controller information.
- **developer_controller** developer controller information.
 - **contract_address** in which contract the organization is created.
 - **owner_address** organization address

### **GetUserFeeController**

``` Proto
rpc GetUserFeeController (google.protobuf.Empty) returns (UserFeeController) {
}

message UserFeeController{
    acs1.AuthorityInfo root_controller = 1;
    acs1.AuthorityInfo parliament_controller = 2;
    acs1.AuthorityInfo referendum_controller = 3;
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

This method returns the controller for UpdateCoefficientsForSender. The root address consists originally of default parliament organization, referendum organization. The type of root controller and developer controller is Assocation.

- **root_controller** root controller information.
- **parliament_controller** parliament controller information.
- **referendum_controller** referndum controller information.
 - **contract_address** in which contract the organization is created.
 - **owner_address** organization address

### **GetSideChainRentalControllerCreateInfo**

``` Proto
rpc GetSideChainRentalControllerCreateInfo (google.protobuf.Empty) returns (acs1.AuthorityInfo) {
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

**AuthorityInfo**:
- **contract address**: controller type.
- **owner address**: controller's address.

### **GetResourceUsage**

``` Proto
rpc GetResourceUsage (google.protobuf.Empty) returns (ResourceUsage) {
}

message ResourceUsage {
    map<string, sint32> value = 1;
}
```

This method is used on a side chain. It returns how much resource tokens should be paid at the moment.

- **value** resource token symbol => amount.

### **GetOwningRental**

``` Proto
rpc GetOwningRental (google.protobuf.Empty) returns (OwningRental) {
}

message OwningRental {
    map<string, sint64> resource_amount = 1;
}
```

This method is used on a side chain. It returns how much resource tokens (count * value) should be paid at the moment.

- **resource_amount** resource token symbol => amount.

### **GetOwningRentalUnitValue**

``` Proto
rpc GetOwningRentalUnitValue (google.protobuf.Empty) returns (OwningRentalUnitValue) {
}

message OwningRentalUnitValue {
    map<string, sint64> resource_unit_value = 1;
}
```

This method is used in side chain. It returns resouces token' unit value. (pay = unit value * amount)

- **resource_unit_value** resource token symbol => unit value.


### **OwningRentalUnitValue**

``` Proto
rpc GetUserFeeController(google.protobuf.Empty) returns (UserFeeController){}

message UserFeeController{
    acs1.AuthorityInfo root_controller = 1;
    acs1.AuthorityInfo parliament_controller = 2;
    acs1.AuthorityInfo referendum_controller = 3;
}
```

Get the controllers(By defalult, the contoller consist of parliament and referendum). If you change the controller, just the root controller has value.

**returns**:
- **root controller**: the root controller, it is a association by default.
- **parliament controller**: parliament controller, member of the root controller.
- **referendum controller**: referendum controller, member of the root controller.


### **GetDeveloperFeeController**

``` Proto
rpc GetDeveloperFeeController (google.protobuf.Empty) returns (DeveloperFeeController) {}

message DeveloperFeeController {
    acs1.AuthorityInfo root_controller = 1;
    acs1.AuthorityInfo parliament_controller = 2;
    acs1.AuthorityInfo developer_controller = 3;
}
```

Get the controllers(By defalult, the contoller consist of parliament and developer). If you change the controller, just the root controller has value.

**returns**:
- **root controller**: the root controller, it is a association by default.
- **parliament controller**: parliament controller, member of the root controller.
- **developer controller**: developer controller consisiting of developers, member of the root controller.



