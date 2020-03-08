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

This command will run the container and a shell within it. 

## Start Redis

In the container use the following command:

```bash 
service start redis-server
```

This will start the Redis instance that the node will use.

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

The ip should be the same as the one you provided for Redis.

From here you can try out any of the available API commands on the Swagger page.