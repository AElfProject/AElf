# Running a node with Docker

A pre-requisite to this tutorial is to install Docker on your system.

## Pull AElf Docker image

After you have completed the Docker installation, you can pull the latest version of the official AElf image with the following command:

```bash
docker pull aelf/node
```

While downloading you can make sure your Redis database instance is ready.

## Create configuration

First, choose a location for the configuration, for this tutorial we’ll create a directory called **singleNode**.

```bash
mkdir singleNode
cd singleNode
```

Next in the directory place **appsettings.json** and **appsettings.MainChain.MainNet.json** files. An example of **appsettings.json** can be found [**here**](https://github.com/AElfProject/AElf/blob/dev/src/AElf.Launcher/appsettings.json). And an example of **appsettings.MainChain.MainNet.json** can be found [**here**](https://github.com/AElfProject/AElf/blob/dev/src/AElf.Launcher/appsettings.MainChain.MainNet.json)

Then you can modify **appsettings.json** file. And the only fields you have to change are the IP and port of your Redis instance :

```json
{
  "ConnectionStrings": {
    "BlockchainDb": "redis://192.168.1.70:6379?db=1",
    "StateDb": "redis://192.168.1.70:6379?db=1"
  },
}
```

Replace "192.168.1.70" and 6379 with whatever host your Redis server is on.

## Starting the container

Once you have finished downloading the latest version of the AElf image, you can start the container:

```bash
docker run -it -p 8000:8000 -v <path/to/singleNode>:/opt/aelf-node -w /opt/aelf-node aelf/node:latest dotnet /app/AElf.Launcher.dll
```

## Access the node's Swagger

You now should have a node that's running, to check this open the browser and enter the address:

```bash
http://your-ip:8000/swagger/index.html
```

The ip should be localhost if you browser is local.

From here you can try out any of the available API commands on the Swagger page. You can also have a look at the API reference [**here**](../../reference/web-api/web-api.md).