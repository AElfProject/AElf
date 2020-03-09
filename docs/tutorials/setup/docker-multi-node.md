# Running multi-nodes with Docker

This tutorial will show you how to run multiple nodes with Docker. This resembles the the Docker tutorial for one node, a part from you will need two instances running.

## Setup

Make sure your Redis instance is ready, this tutorial requires two clean instances (we'll use db1 an db2).

You can start by opening three terminals, in one of them make sure you have the latest version of the image:

```bash
docker pull aelf/node
```

Wait for any update to finish.

### Node one

```bash
docker run -it -p 8000:8000 -p 6800:6800 aelf/node:latest /bin/bash
```

Here 8000 will be the API endpoint port and 6800 the listening port.

From here you can modify the configuration: use you favorite editor to modify the **appsettings.json**, here we use vim:

```bash
vim appsettings.json
```

```json
  "ConnectionStrings": {
    "BlockchainDb": "redis://192.168.1.70:6379?db=1",
    "StateDb": "redis://192.168.1.70:6379?db=1"
  },
```

Replace "192.168.1.70" and 6379 with whatever host your Redis server is on.
The default API port and P2P endpoint should already default to the correct values

Note: with vim, press ESC then ":wq" to save and exit.

### Node two

```bash
docker run -it -p 8001:8001 -p 6801:6801 aelf/node:latest /bin/bash
```
