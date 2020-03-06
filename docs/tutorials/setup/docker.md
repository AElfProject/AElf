# Running a node with Docker

A pre-requisite to this tutorial is to install Docker on your system.

## Pull AElf Docker image

After you have completed the Docker installation, you can pull the latest version of the official AElf image with the following command:

```bash
docker pull aelf/node
```

While downloading you can follow the next sections.

### Generating the nodes account

Next you need to install the **aelf-command** command packet. Open a terminal and enter the following command:

```bash
npm i -g aelf-command
```

Windows Note: it's possible that you get some errors about python not being installed, you can safely ignore these.

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

## Create/edit configuration files

This will require the information printed during the creation of the account. Open the **appsettings.json** file and edit the following sections.

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

## Docker run

Next you need to run the image with docker:

```bash
TODO docker command
```

You now should have a node that's running, to check this run the following command that will query the node for its current block height:

```bash
aelf-command get-blk-height -e http://127.0.0.1:8000
```