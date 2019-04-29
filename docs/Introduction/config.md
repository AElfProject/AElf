## Node configuration

At application startup the node will read the **appsettings.json** configuration file. Every module will read the json section it needs so your configuration depends on your modules.

If you are using AElf **as is** and have not modified the default appsettings you will still need to give a minimum amount of options.

### Options you need to change

You will need to give the node the **Account** you want to use. This is generated with the CLI. 

- NodeAccount is the address that was printed by the CLI.
- NodeAccountPassword is the passeword that you used to generate the keypair and address.

```json
  "Account": {
    "NodeAccount": "5ta1yvi2dFEs4V7YLPgwkbnn816xVUvwWyTHPHcfxMVLrLB",
    "NodeAccountPassword": "passwrd"
  },
```

Next the **Consensus** section that looks like the following json snippet.

- InitialMiners is the list of block miners, the values in the array are the public key of the miners. Also printed by CLI at account creation.

```json
"Consensus": {
    "InitialMiners": [
      "04d8f8fd19cf9e3f7f84eb6be33ad28163d7e929b66bfd0a4059fbc9369cc770654d0506c5cdc9e69b86b6a14f06494e843998fb5565cbc30bb7ccb1cc3105e557"
    ],
    ...
  }
```

That all you need to modify to get the node working.

### Other important options 

**RPC**: you can configure the listening IP and port of the RPC with the following configuration section:

```json
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "http://*:1728/"
      }
    }
  }
  ```

**Network**: you can configure the network with the following options:

- Bootnodes is the list of peers you want your node to connect to on startup. Format: ["127.0.0.1:6800", ...].
- ListeningPort is the p2p port you listen on for incoming connections.

```json
  "Network": {
    "BootNodes": [],
    "ListeningPort": 7001
  },
```
  
**Database**: you can configure the connection strings to the database with the following json. Format: "protocol://IP:port?db=dbNumber"

```json
"ConnectionStrings": {
    "BlockchainDb": "redis://localhost:6379?db=4",
    "StateDb": "redis://localhost:6379?db=4"
  },
```