# Smart contract deployment

After the contract has been compiled, the user must register this contract with the blockchain. Generally, to deploy a contract, there must be transactions sent to Smart contract zero, 
which is one of AElf's genesis contracts. The node will then broadcast these transactions, and it will eventually get included in a block when the block gets executed the smart contract 
will be deployed. 

For contract deployment, what matters is the `ContractDeploymentAuthorityRequired` option in the `ContractOptions` for this network. 
It is determined since the launch of the chain. 

- if `ContractDeploymentAuthorityRequired` is false, anyone can directly deploy contract with transaction 
- Only account with specific authority is permitted to deploy contract if `ContractDeploymentAuthorityRequired` is true

This part will introduce contract deployment pipeline for different chain type on AElf mainnet/testnet/customnet network. 

## `ContractDeploymentAuthorityRequired` is false

Anyone can directly deploy contract with transaction if `ContractDeploymentAuthorityRequired` is false. 
It is usually set as false especially when it is for contract unit test or custom network. 

```protobuf
    rpc DeploySmartContract (ContractDeploymentInput) returns (aelf.Address) {
    }
    
    message ContractDeploymentInput {
        sint32 category = 1;
        bytes code = 2;
    }
```

The return value of this transaction indicates the address of the deployed contract. Note that you should specific 0 as category
for c# contract and provide your contract dll bytes.
 

## `ContractDeploymentAuthorityRequired` is true

`ContractDeploymentAuthorityRequired` is always true when it comes to public networks(Mainnet/Testnet). 
And contract pipelines are distinguished for different chain types. But for sure, no one can directly deploy.

For public network, no matter it is mainnet or testnet, things are going more complex. No one can directly deploy on 
the chain but few authorities have the permission to propose.

- Main Chain: only current miners have the permission to propose contract
- Exclusive Side Chain: only side chain creator are allowed to propose contract
- Shared Side Chain: anyone can propose contract 

And contract proposing steps are provided as below

```protobuf
    rpc ProposeNewContract (ContractDeploymentInput) returns (aelf.Hash) {
    }
    message ContractDeploymentInput {
        sint32 category = 1;
        bytes code = 2;
    }
    
    message ContractProposed
    {
        option (aelf.is_event) = true;
        aelf.Hash proposed_contract_input_hash = 1;
    }
```

Event `ContractProposed` will be fired containing `proposed_contract_input_hash` and this will also trigger the first proposal 
for one parliament organization, which is specified as contract deployment controller since the beginning of the chain.
This proposal would be expired in 24 hours. Once the proposal can be released (refer to [Parliament contract](../../reference/smart-contract-api/parliament.md) for detail), 
proposer should send transaction to 

```protobuf
    rpc ReleaseApprovedContract (ReleaseContractInput) returns (google.protobuf.Empty) {
    }
    message ReleaseContractInput {
        aelf.Hash proposal_id = 1;
        aelf.Hash proposed_contract_input_hash = 2;
    }
```

This will trigger the second proposal for one parliament organization, which is specified as contract code-check 
controller since the beginning of the chain. This proposal would be expired in 10 min. Once the proposal can be released, proposer should send transaction to

```protobuf
    rpc ReleaseCodeCheckedContract (ReleaseContractInput) returns (google.protobuf.Empty) {
    }
    message ReleaseContractInput {
        aelf.Hash proposal_id = 1;
        aelf.Hash proposed_contract_input_hash = 2;
    }
    
    message ContractDeployed
    {
        option (aelf.is_event) = true;
        aelf.Address author = 1 [(aelf.is_indexed) = true];
        aelf.Hash code_hash = 2 [(aelf.is_indexed) = true];
        aelf.Address address = 3;
        int32 version = 4;
        aelf.Hash Name = 5;
    }
```

Finally, the contract would be deployed. Event `ContractDeployed` containing new contract address will be fired and 
it is available in `TransactionResult.Logs`.


## Use aelf-command send or aelf-command proposal to deploy

If you set `ContractDeploymentAuthorityRequired: true` in appsetting.json, please use aelf-command proposal.

```bash
 $ aelf-command send <GenesisContractAddress> DeploySmartContract # aelf-command send
 $ aelf-command send <GenesisContractAddress> ProposeNewContract # aelf-command proposal
 # Follow the instructions
```

- You must input contract method parameters in the prompting way, note that you can input a relative or absolute path of contract file to pass a file to aelf-command, aelf-command will read the file content and encode it as a base64 string.
- After call ProposeNewContract, you need to wait for the organization members to approve your proposal and you can release your proposal by calling `ReleaseApprovedContract` and `ReleaseCodeCheckedContract` in this order.

### The deploy command(This command has been deprecated)

The **deploy** command on the cli will help you deploy the contract:

```bash 
aelf-command deploy <category> <code>
```

The deploy command will create and send the transaction to the nodes RPC. Here the **code** is the path to the compiled code. This will be embedded in the transaction as a parameter to the **DeploySmartContract** method on smart contract zero. The command will return the ID of the transaction that was sent by the command. You will see in the next section how to use it.

#### verify the result

When the deployment transaction gets included in a block, the contract should be deployed. To check this, you can use the transaction ID returned by the deploy command. When the status of the transaction becomes **mined**: ```"Status": "Mined"```, then the contract is ready to be called. 

The **ReadableReturnValue** field indicates the address of the deployed contract. You can use this address to call the contract methods.







