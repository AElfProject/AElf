# Linux
## Configure Environment

You can install and set up the development environment on computers running 64-bit Linux. This will take 10-20 minutes.

### Before You Start

Before you install and set up the development environment on a Linux device, please make sure that your computer meets these basic requirements:

- Ubuntu 18.

- Broadband internet connection.

### Update Environment

Execute this command to update your environment:
```bash
  sudo apt-get update
```
The following output suggests successful update:
```bash
  Fetched 25.0 MB in 3s (8,574 kB/s)                           
  Reading package lists... Done
```

### Install Git

If you want to use our customized smart contract development environment or to run a node, you need to clone aelf's repo (download source code). As aelf's code is hosted on GitHub, you need to install **Git** first.

1. Open the terminal.
2. Execute this command to install Git:

  ```bash
      sudo apt-get install git -y
  ```

3. Execute this command to check if Git is installed:

  ```bash
      git --version
  ```
The following output suggests successful installation:

  ```bash
      git version 2.17.1
  ```

### Install .NET SDK

As aelf is mostly developed with .NET Core, you need to download and install .NET Core SDK.

1. Execute the following commands to install .NET 6.0.

  1. Execute this command to download .NET packages:

      ```bash
          wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
      ```

  2. Execute this command to unzip .NET packages: 
  
      ```bash
          sudo dpkg -i packages-microsoft-prod.deb
        
          rm packages-microsoft-prod.deb
      ```

  3. Execute this command to install .NET:
  
      ```bash
          sudo apt-get update && \
        
          sudo apt-get install -y dotnet-sdk-6.0
      ```

2. Execute this command to check if .NET 6.0 is installed:

    ```bash
      dotnet --version
    ```

  The following output suggests successful installation:

    ```
      6.0.403
    ```

### Install protoBuf

Before you start the installation, please check the directory you use and execute the following commands to install.

1. Execute the following commands to install protoBuf.

  1. Execute this command to download protoBuf packages:
  
      ```bash
          curl -OL https://github.com/google/protobuf/releases/download/v21.9/protoc-21.9-linux-x86_64.zip
      ```
  2. Execute this command to unzip protoBuf packages: 

      ```
          unzip protoc-21.9-linux-x86_64.zip -d protoc3
      ```

  3. Execute these commands to install protoBuf:

      ```bash
          sudo mv protoc3/bin/* /usr/local/bin/

          sudo mv protoc3/include/* /usr/local/include/

          sudo chown ${USER} /usr/local/bin/protoc

          sudo chown -R ${USER} /usr/local/include/google
      ```

      If it shows error ```Permission denied @ apply2files```, then there is a permission issue. You can solve it using the following command and then redo the installation with the above commands:

      ```bash
          sudo chown -R $(whoami) $(brew --prefix)/*
      ```

2. Execute this command to check if protoBuf is installed:

  ```bash
      protoc --version
  ```
The following output suggests successful installation:

  ```
      libprotoc 3.21.9
  ```

### Install Redis

1. Execute this command to install Redis:

  ```bash
      sudo apt-get install redis -y
  ```
2. Execute this command to start a Redis instance and check if Redis is installed:
  ```
      redis-server
  ```
  The following output suggests Redis is installed and a Redis instance is started:

  ```
      Server initialized
      Ready to accept connections
  ```

  You can open a new terminal and use redis-cli to start Redis command line. The command below can be used to clear Redis cache (be careful to use it):

  ```
      flushall
  ```

### Install Nodejs

1. Execute these commands to install Nodejs:

  ```bash
      curl -fsSL https://deb.nodesource.com/setup_14.x | sudo -E bash -
      
      sudo apt-get install -y nodejs
  ```

2. Execute this command to check if Nodejs is installed:

  ```bash
      npm --version
  ```

  The following output suggests successful installation:

  ```
      6.14.8
  ```

## What's Next

If you have installed the above tools and frameworks, you can proceed with what interests you here. Read the following to learn about contract deployment and node running:

[Smart contract development](https://docs.aelf.io/en/latest/getting-started/smart-contract-development/index.html) 

[Smart contract deployment](https://docs.aelf.io/en/latest/getting-started/smart-contract-development/index.html)

[Node](/Development Environment/node/node.md)



