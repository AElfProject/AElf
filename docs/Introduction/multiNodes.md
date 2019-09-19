# QuickStart

## Manual build & run the sources

This section will walk you through the steps for launching two nodes or more by repeating the steps. It is largely based on the section 
about running a single node (quickstart), so following the fist tutorial is highly recommended.

Make sure that you can run a single node. To sum the steps for one node:
- clone the repository.
- use **aelf-command create** to create a keypair that is by default saved to the datadir.
- modify the **NodeAccount**, **NodeAccountPassword** values in the **appsettings.json** (AElf.Launcher project).
- add the key-pair public key as a miner in the **InitialMinerList**.
- Launch the node.

These are basically the step you need to repeat to form a network of nodes.

### Setup the environment

Create two folders in the location of your choice, one for each miner (lets say **miner1** and **miner2**). From the AElf repository that you cloned, there's templates for **appsettings.json** and **appsettings.MainChain.MainNet.json** copy these two files and place a copy in each of the miners folders.

Generate two accounts, one for each miner, be sure to keep the addresses and the password as well as the password.

### Modify the configuration

Modify each miners configuration with their respective accounts like in the previous tutorial. Once this is done you should update both config files with both accounts, so the configuration for **InitialMinerList** will look something like this:

```json
"InitialMinerList" : [
    "0478903d96aa2c8c0...6a3e7d810cacd136117ea7b13d2c9337e1ec88288111955b76ea",
    "cacd136117ea7b13d...ea3cac7d8107ea7b13d2c98d1361rp10cad136188111955b76ea"
],
```

### 

- open 2 terminals in each of the miners folders.
- update both configurations: the account, miners list and the bootnodes.

- publish AElf (another folder).


** How to run a node and join the existing system **


