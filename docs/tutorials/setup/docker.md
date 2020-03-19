# Running a node with Docker

A pre-requisite to this tutorial is to install Docker on your system.

## Pull AElf Docker image

After you have completed the Docker installation, you can pull the latest version of the official AElf image with the following command:

```bash
docker pull aelf/node
```

While downloading you can make sure your Redis database instance is ready.

## Starting the container

Once you have finished downloading the latest version of the AElf image, you can start the container and edit the configuration:

```bash
docker run -it -p 8000:8000 aelf/node:latest /bin/bash
```

This command will run the container and a shell within it. From here you can modify the configuration: use you favorite editor to modify the **appsettings.json**, here we use vim:

```bash
vim appsettings.json
```

This will open the file (press i for insert mode in vim). The only fields you have to change are the IP and port of your Redis instance :

```json
  "ConnectionStrings": {
    "BlockchainDb": "redis://192.168.1.70:6379?db=1",
    "StateDb": "redis://192.168.1.70:6379?db=1"
  },
```

Replace "192.168.1.70" and 6379 with whatever host your Redis server is on.

Note: with vim, press ESC then ":wq" to save and exit.

## Run the program

At this point you are still *inside* Docker, the next step is to run the AElf node:

```bash
dotnet AElf.Launcher.dll
```

## Access the node's Swagger

You now should have a node that's running, to check this open the browser and enter the address:

```bash
http://your-ip:8000/swagger/index.html
```

The ip should be localhost if you browser is local.

From here you can try out any of the available API commands on the Swagger page. You can also have a look at the API reference [**here**](../../web-api-reference/reference.md).