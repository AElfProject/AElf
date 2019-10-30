
## Prerequisits 

Before you jump in to the guides and tutorial you'll need to install the following tools and frameworks.

# Development framework

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

