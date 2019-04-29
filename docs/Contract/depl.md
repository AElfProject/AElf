## Smart contract deployment

After the contract has been compiled, the user must register this contract with the blockchain. To deploy a contract there must be a deployment transaction sent to Smart contract zero, which is one of AElfs genesis contracts. The node will then broadcast this transaction and it will eventually get included in a block, when the block gets executed the smart contract will be deployed.

#### the deploy command

The **deploy** command on the cli will help you deploy the contract:

```bash 
aelf-cli deploy <category> <code>
```

The deploy command will create and send the transaction to the nodes RPC. Here the **code** is the path to the compiled code. This will be embedded in the transaction as a parameter to the **DeploySmartContract** method on smart contract zero. The command will return the ID of the transaction that was sent by the command. You will see in the next section how to use it.

#### verify the result

When the deployement transaction gets included in a block the contract should be deployed. To check this, you can use the transaction ID returned by the deploy command. When the status of the transaction becomes **mined**: ```"Status": "Mined"```, then the contract is ready to be called. 

The **ReadableReturnValue** field indicates the address of the deployed contract. You can use this address to call the contracts methods.
