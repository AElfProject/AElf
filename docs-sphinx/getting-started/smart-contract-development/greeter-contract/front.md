# Front end

This tutorial will show you how to develop a front-end app (JavaScript in our case) that will demonstrate how to interact with a contract that was developed with Boilerplate. 

At the top-level Boilerplate contains two folders:
- chain : used for developing the contracts.
- web : used for developing the front-end.

The **web** folder already contains some projects that can serve as examples. This tutorial presents a front-end for the Greeter contract shown in the previous tutorials.

## Run the front-end

After you run Boilerplate, open another terminal at the repo's root and navigate to the **greeter** project:

```bash
cd web/greeter
```

From here, you can install and run the Greeter's front end:

```bash
npm i
npm start
```

And a page will be opened by webpack in your default browser.

## Front-end code

The code is straightforward, it uses aelf-sdk + webpack. You can check out more [**here**](https://github.com/AElfProject/aelf-sdk.js).

**Warning**: be careful, this code is in no way production-ready and is for demonstration purposes only.

It demonstrates the following capabilities of the js sdk:
- getting the chain status.
- getting a contract object.
- calling a contract method.
- calling a view method.

### Getting the chain status

The following code snippet shows how to call the nodes API to get the chains status:

```javascript
aelf.chain.getChainStatus()
    .then(res => {
        if (!res) {
            throw new Error('Error occurred when getting chain status');
        }
        // use the chain status
    })
    .catch(err => {
        console.log(err);
    });
```

For more information about the chain status API : [GET /api/blockChain/chainStatus](../../../reference/web-api/web-api.md).

As we will see next, the chain status is very useful for retrieving the genesis contract.

#### getting a contract object

The following code snippet shows how to get a contract object with the js-sdk:

```javascript
async function getContract(name, walletInstance) {

    // if not loaded, load the genesis
    if (!genesisContract) {
        const chainStatus = await aelf.chain.getChainStatus();
        if (!chainStatus) {
            throw new Error('Error occurred when getting chain status');
        }
        genesisContract = await aelf.chain.contractAt(chainStatus.GenesisContractAddress, walletInstance);
    }

    // if the contract is not already loaded, get it by name.
    if (!contract[name]) {
        const address = await genesisContract.GetContractAddressByName.call(sha256(name));
        contract = {
            ...contract,
            [name]: await aelf.chain.contractAt(address, walletInstance)
        };
    }
    return contract[name];
}
```

As seen above, the following steps will enable you to build a contract object:
- use **getChainStatus** to get the genesis contract's address.
- use **contractAt** to build an instance of the genesis contract.
- use the genesis contract to get the address of the greeter contract with the **GetContractAddressByName** method.
- with the address use **contractAt** again to build a greeter contract object.

Once you have a reference to the greeter contract, you can use it to call the methods.

#### calling a contract method

The following snippet shows how to send a transaction to the contract:

```javascript
    greetToButton.onclick = () => {

        getContract('AElf.ContractNames.Greeter', wallet)
            .then(greeterContract => greeterContract.GreetTo({
                value: "SomeName"
            }))
            .then(tx => pollMining(tx.TransactionId))
            .then(ret => {
                greetToResponse.innerHTML = ret.ReadableReturnValue;
            })
            .catch(err => {
                console.log(err);
            });
    };
```

Here the **getContract** retrieves the greeter contract instance. On the instance it calls **GreetTo** that will send a transaction to the node. The **pollMining** method is a helper method that will wait for the transaction to be mined. After mined the transaction results, **ReadableReturnValue** will be used to see the result.

#### calling a view method

The following snippet shows how to call a view method on the contract:

```javascript
    getGreeted.onclick = () => {

        getContract('AElf.ContractNames.Greeter', wallet)
            .then(greeterContract => greeterContract.GetGreetedList.call())
            .then(ret => {
                greeted.innerHTML = JSON.stringify(ret, null, 2);
            })
            .catch(err => {
                console.log(err);
            });
    };
```

Here the **getContract** retrieves the greeter contract instance. On the instance, it calls **GetGreetedList** with ".call" appended to it, which will indicate a read-only execution (no broadcasted transaction).

## Next

This first series of tutorials showed you an end-to-end example of a dApp implemented with Boilerplate. Further tutorials will give more in-depth explanations about some aspect of the contracts.