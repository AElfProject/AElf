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

- create 2 folders, one for each miner (miner1 and miner2).
- copy the app settings and appsettings.MainChain.MainNet files and log4net config.
- publish AElf (another folder).
- open 2 terminals in each of the miners folders.
- update both configurations: the account, miners list and the bootnodes.


** How to run a node and join the existing system **


