
## Pre-requisites 

Before you jump in to the guides and tutorial you'll need to install the following tools and frameworks.

# Pre-setup for Windows users

One convenient tool for Windows users is the Chocolatey for installing dependencies. Open this link and follow the installation instructions: [chock](https://chocolatey.org/install). Later, Chocolatey can be very useful for installing dependencies like Git and Protobuf.


# Git

If you want to run a node or use our custom smart contract environment, at some point you will have to clone (download the source code) from AElf's repository. For this you will have to use **Git** since we host our code on GitHub.

Click the following link to download Git for your platform:

[Getting Started - Installing Git](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)

# Node js

Next install nodejs by following the instructions here [nodejs](https://nodejs.org/en/download/).

# Development framework - dotnet sdk

Most of AElf is developed with dotnet core, so you will need to download and install the .NET Core SDK before you start:
[download page](https://dotnet.microsoft.com/download).

On this provided link find the download for your platform, be sure to download the SDK, it will look like this: 

<p align="center">
    <img src="dotnet-sdk-dl-link.png" height="50">
</p>

Wait for the download to finish and follow the instructions: for AElf all defaults provided in the installer should be correct.

To check the installation, you can open a terminal and run the ``dotnet`` command. If everything went fine it will show you dotnet options for the command line.

# Database

We currently support two key-value databases to store our nodes data: redis or ssdb. Both work well, it's your decision:
- [Redis](https://redis.io/)
- [SSDB](http://ssdb.io/?lang=en) 

# Protobuf

You also need to install protobuf compiler https://developers.google.com/protocol-buffers/.

