
## Pre-requisites 

Before you jump in to the guides and tutorials you'll need to install the following tools and frameworks.

# Pre-setup for Windows users

A convenient tool for Windows users is **Chocolatey** for installing dependencies. Open this link and follow the installation instructions:

[**Chocolatey installation**](https://chocolatey.org/install)

Later, **Chocolatey** can be very useful for installing dependencies like **Git** and **Protobuf**.

Another dependency needed to build **AElf** from the command line under Windows is **Visual Studio Build Tools**. The easiest way is to use the **Visual Studio Installer**: 

If you already have an edition of **Visual Studio** installed, open the **Visual Studio Installer** and add the **Desktop development with C++** workload:

<p align="center">
    <img src="vs-cpp-dep.png" height="300">
</p>

If you don't have any of the Visual Studio editions installed:

- you can download it here [**Visual Studio Community Edition**](https://visualstudio.microsoft.com/fr/downloads/?rr=https%3A%2F%2Fwww.google.com%2F) for free and after the installation add the **Desktop development with C++** workload.

- or if you don't need or want a full blown installation of **Visual Studio**, you can download the build tools here: [**Download Page**](https://visualstudio.microsoft.com/downloads/#other). Scroll down and under the section *Tools for Visual Studio 2019* download the build tools for Visual Studio:

<p align="center">
    <img src="build-tools.png" height="100" width="600">
</p>

 After the installation open **Visual Studio Installer**, locate and install the *C++ build tools*.

 <p align="center">
    <img src="build-tools-2.png" height="50">
</p>

# Pre-setup for macOS users

It is highly recommended that you install **Homebrew (or simply Brew)** to quickly and easily setup dependencies:

[**Homebrew install page**](https://brew.sh/)

# Git

If you want to run a node or use our custom smart contract environment, at some point you will have to clone (download the source code) from AElf's repository. For this you will have to use **Git** since we host our code on GitHub.

Click the following link to download Git for your platform (the link provides Chocolatey and Brew packages to use):

[**Getting Started - Installing Git**](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)

# Node js

Next install nodejs by following the instructions here:

[**Nodejs**](https://nodejs.org/en/download/).

# Development framework - dotnet core sdk

Most of AElf is developed with dotnet core, so you will need to download and install the .NET Core SDK before you start:

[**Download .NET Core 2.2**](https://dotnet.microsoft.com/download/dotnet-core/2.2)

For now AElf depends on version 2.2 of the SDK, on the provided link find the download for your platform (for Windows and macOS the installer for x64 is the most convenient if your platform is compatible - most are these days), the page looks like this: 

<p align="center">
    <img src="dotnet-sdk-dl-link.png" height="200">
</p>

Wait for the download to finish and follow the instructions: for AElf all defaults provided in the installer should be correct.

To check the installation, you can open a terminal and run the ``dotnet`` command. If everything went fine it will show you dotnet options for the command line.

# Database

We currently support two key-value databases to store our nodes data: **Redis** or **SSDB**. Both work well, it's your decision:

## Redis (recommended fo the tutorials): 

[**Redis download page** (sources)](https://redis.io/)

- On Windows we recommend you use the Chocolatey package for Redis: [**Redis Chocolatey Package**](https://chocolatey.org/packages/redis-64#dependencies).
- On macOS, open a terminal and use Brew:  ```brew install redis```


To test the installation (all platforms) you can just open a terminal and type ```redis-server```. This will show you the servers welcome page as well as the port it's listening on:

<p align="center">
    <img src="redis.png" height="300">
</p>

## SSDB: 

[**SSDB**](http://ssdb.io/?lang=en) 

- On macOS you can use Brew : ```brew install ssdb```

# Protobuf

**Protobuf** is also a dependency, the next section will show you how to clone our repository and run the install scripts.

