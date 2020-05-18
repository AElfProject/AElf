## Smart contract deployment

After the contract has been compiled, the user must register this contract with the blockchain. To deploy a contract, there must be a deployment transaction sent to Smart contract zero, which is one of AElf's genesis contracts. The node will then broadcast this transaction, and it will eventually get included in a block when the block gets executed the smart contract will be deployed.

### Use aelf-command send or aelf-command proposal to deploy

If you set `ContractDeploymentAuthorityRequired: true` in appsetting.json, please use aelf-command proposal.

```bash
 $ aelf-command send <GenesisContractAddress> DeploySmartContract # aelf-command send
 $ aelf-command send <GenesisContractAddress> ProposeNewContract # aelf-command proposal
 # Follow the instructions
```

- You must input contract method parameters in the prompting way, note that you can input a relative or absolute path of contract file to pass a file to aelf-command, aelf-command will read the file content and encode it as a base64 string.
- After call ProposeNewContract, you need to wait for the organization members to approve your proposal and you can release your proposal by calling releaseApprove and releaseCodeCheck in this order.

### The deploy command(This command has been deprecated)

The **deploy** command on the cli will help you deploy the contract:

```bash 
aelf-command deploy <category> <code>
```

The deploy command will create and send the transaction to the nodes RPC. Here the **code** is the path to the compiled code. This will be embedded in the transaction as a parameter to the **DeploySmartContract** method on smart contract zero. The command will return the ID of the transaction that was sent by the command. You will see in the next section how to use it.

#### verify the result

When the deployment transaction gets included in a block, the contract should be deployed. To check this, you can use the transaction ID returned by the deploy command. When the status of the transaction becomes **mined**: ```"Status": "Mined"```, then the contract is ready to be called. 

The **ReadableReturnValue** field indicates the address of the deployed contract. You can use this address to call the contract methods.
