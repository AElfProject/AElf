# QuickStart

## Manual build & run the sources

This method is not as straightforward as the docker quickstart but is a lot more flexible. If your aim is to develop some dApps it's better you follow these more advanced ways of launching a node. This section will walk you through configuring, running and interacting with an AElf node.

Pre-requisite: this guide has a dependency on Redis, so you should install it and run a server. A part from this, only very basic command line knowledge is required and you can just follow the steps.

First, if you haven't already done it, clone our [repository](https://github.com/AElfProject/AElf)

```bash
git clone https://github.com/AElfProject/AElf.git aelf
cd aelf/src
```

Navigate into the newly created **aelf** directory.

### Generating the nodes account

Installing **aelf-command**:

```bash
npm i -g aelf-command # please install Nodejs before this
```

After installing **aelf-command** you can use the following command to create an account/key-pair:

```bash
aelf-command create
```

The command prompts for a password, enter it and don't forget it. The output of the command should look something like this:

```bash
Your wallet info is :
Mnemonic            : great mushroom loan crisp ... door juice embrace
Private Key         : e038eea7e151eb451ba2901f7...b08ba5b76d8f288
Public Key          : 0478903d96aa2c8c0...6a3e7d810cacd136117ea7b13d2c9337e1ec88288111955b76ea
Address             : 2Ue31YTuB5Szy7cnr3SCEGU2gtGi5uMQBYarYUR5oGin1sys6H
✔ Save account info into a file? … no / yes
✔ Enter a password … ********
✔ Confirm password … ********
✔
Account info has been saved to "/Users/xxx/.local/share/**aelf**/keys/2Ue31YTuB5Szy7cnr...Gi5uMQBYarYUR5oGin1sys6H.json"
```

In the next steps of the tutorial you will need the **Public Key** and the **Address** for the account you just created. You'll notice the last line of the 
commands output will show you the path to the newly created key. The **aelf** is the data directory (datadir) and this is where the node will read the keys from.

Note that a more detailed section about the cli can be found [command line interface](../cli/cli.md).

### Node configuration

We have one last step before we can run the node, we have to set up some configuration. Navigate into the **AElf.Launcher** directory:

```bash
cd AElf.Launcher/
```

This folder contains the default **appsettings.json** file, with some default values already given. There's still some fields that are empty and that need configuring. This will require the information printed during the creation of the account. Open the **appsettings.json** file and edit the following sections.

The account/key-pair associated with the node we are going to run:

```json
"Account":
{
    "NodeAccount": "2Ue31YTuB5Szy7cnr3SCEGU2gtGi5uMQBYarYUR5oGin1sys6H",
    "NodeAccountPassword": "********"
},
```

The *NodeAccount* field corresponds to the address, you also have to enter the password that you entered earlier.

```json
"InitialMinerList" : [
    "0478903d96aa2c8c0...6a3e7d810cacd136117ea7b13d2c9337e1ec88288111955b76ea"
],
```

This is a configuration that is used to specify the initial miners for the DPoS consensus, for now just configure one, it's the accounts public key that was printed during the account creation.

Note that if your Redis server is on another host listening on a different port than the default, you will also have to configure the connection strings (port/db number):

```json
  "ConnectionStrings": {
    "BlockchainDb": "redis://localhost:6379?db=1",
    "StateDb": "redis://localhost:6379?db=1"
  },
```

We've created an account/key-pair and modified the configuration to use this account for the node and mining, we're now ready to launch the node.

### Launch and test

Now we build and run the node navigate into the **aelf** directory and build the solution with the following commands:

```bash
dotnet build AElf.Launcher.csproj --configuration Release
dotnet bin/Release/netcoreapp2.2/AElf.Launcher.dll > aelf-logs.logs &
cd ..
```

You now should have a node that's running, to check this run the following command that will query the node for its current block height:

```bash
aelf-command get-blk-height -e http://127.0.0.1:1728
```

### Cleanup

To stop the node you can simply find and kill the process with:

```bash
ps -f | grep  [A]Elf.Launcher.dll | awk '{print $2}'
```

If needed you should also clean your redis database, with either of the following commands:

```bash
redis-cli FLUSHALL (clears all dbs)
```

```bash
redis-cli -n <database_number> FLUSHDB (clear a specified db)
```

### Extra

For reference and after you've started a node, you can get infos about an account with the *aelf-command console* command:

```bash
aelf-command console -a 2Ue31YTuB5Szy7cnr3SCEGU2gtGi5uMQBYarYUR5oGin1sys6H

✔ Enter the password you typed when creating a wallet … ********
✔ Succeed!
Welcome to aelf interactive console. Ctrl + C to terminate the program. Double tap Tab to list objects

   ╔═══════════════════════════════════════════════════════════╗
   ║                                                           ║
   ║   NAME       | DESCRIPTION                                ║
   ║   AElf       | imported from aelf-sdk                     ║
   ║   aelf       | the instance of an aelf-sdk, connect to    ║
   ║              | http://127.0.0.1:8000                  ║
   ║   _account   | the instance of an AElf wallet, address    ║
   ║              | is                                         ║
   ║              | 2Ue31YTuB5Szy7cnr3SCEGU2gtGi5uMQBYarYUR…   ║
   ║              | 5oGin1sys6H                                ║
   ║                                                           ║
   ╚═══════════════════════════════════════════════════════════╝
```
