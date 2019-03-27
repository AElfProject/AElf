# QuickStart

# Docker quickstart


# Manual build & run the sources

This method is not as straightforward as the docker quickstart but is a lot more flexible. If your aim is to develop some dApps it's better you follow these more advanced ways of launching a node. This section will walk you through configuring, running and interacting with an AElf node.

### Generate the nodes account:
First, if you haven't already done it, clone our [repository](https://github.com/AElfProject/AElf) and stay on the `dev` branch

    ```bash
    git clone https://github.com/AElfProject/AElf.git aelf
    ```

Secondly navigate into the **aelf** directory to generate the nodes account (key pair) with AElfs command line tool. For this build and run the cli with the following command:

...

Note that a more detailed section about the cli can be found [here].

### Install Redis:
You will now need to install Redis as our node needs a key-value database to store the blockchain data.

...

### Node configuration:
Now that redis is setup, we have one last step before we can run the node, we have to set up some configuration...

...

We're now ready to launch the node

...


### Launch and test:
Now we build and run the node navigate into the **aelf** directory and build the solution with the following command:

    ```bash
    dotnet build --configuration Release
    ```

You can test the node 



