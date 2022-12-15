# macOS

## Configure Environment

You can install and set up the development environment on macOS computers with either Intel or Apple M1 processors. This will take 10-20 minutes.

### Before You Start

Before you install and set up the development environment on a macOS device, please make sure that your computer meets these basic requirements:

- Operating system version is 10.7 Lion or higher.

- At least a 2Ghz processor, 3Ghz recommended.

- At least 8 GB RAM, 16 GB recommended.

- No less than 10 GB of available space.

- Broadband internet connection.

**Support for Apple M1**

If you use a macOS computer with an Apple M1 chip, you need to install Apple Rosetta. Open the Terminal on your computer and execute this command:

```powershell
	/usr/sbin/softwareupdate --install-rosetta --agree-to-license
```

### Install Homebrew

In most cases, you should use Homebrew to install and manage packages on macOS devices. If Homebrew is not installed on your local computer yet, you should download and install it before you continue.

To install Homebrew:

1. Open Terminal.

2. Execute this command to install Homebrew:

   ```bash
   	/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
   ```

3. Execute this command to check if Homebrew is installed:

   ```bash
   	brew --version
   ```

   The following output suggests successful installation:

   ```bash
   	Homebrew 3.3.1

   	Homebrew/homebrew-core (git revision c6c488fbc0f; last commit 2021-10-30)

   	Homebrew/homebrew-cask (git revision 66bab33b26; last commit 2021-10-30)
   ```

### Environment Update

Execute this command to update your environment:

```bash
		brew update
```

You will see output like this and you can select the next command based on your needs.

```bash
	You have xx outdated formula installed.
	You can upgrade it with brew upgrade
	or list it with brew outdated.
```

### Install Git

If you want to use our customized smart contract development environment or to run a node, you need to clone aelf's repo (download source code). As aelf's code is hosted on GitHub, you need to install **Git** first.

1. Execute this command in Terminal:

   ```bash
   	brew install git
   ```

2. Execute this command to check if Git is installed:

   ```bash
   	git --version
   ```

   The following output suggests successful installation:

   ```bash
   	git version xx.xx.xx
   ```

### Install .NET SDK

As aelf is mostly developed with .NET Core, you need to download and install .NET Core SDK (**Installers - x64** recommended for Windows and macOS if compatible).

1. Download and install .NET 6.0 which is currently used in aelf's repo.

2. Please reopen Terminal after the installation is done.

3. Execute this command to check if .NET is installed:

   ```bash
   	dotnet --version
   ```

   The following output suggests successful installation:

   ```
   	6.0.403
   ```

### Install protoBuf

1.  Execute this command to install protoBuf:

    ```bash
        brew install protobuf
    ```

    If it shows error `Permission denied @ apply2files`, then there is a permission issue. You can solve it using the following command and then redo the installation with the above command:

    ```bash
        sudo chown -R $(whoami) $(brew --prefix)/*
    ```

2.  Execute this command to check if protoBuf is installed:

    ```bash
    	protoc --version
    ```

    The following output suggests successful installation:

    ```bash
    	libprotoc 3.21.9
    ```

### Install Redis

1. Execute this command to install Redis:

   ```bash
   	brew install redis
   ```

2. Execute this command to start a Redis instance and check if Redis is installed:

   ```bash
   	redis-server
   ```

   The following output suggests Redis is installed and a Redis instance is started:

   ![image](mac_install_redis.png)

### Install Nodejs

1. Execute this command to install Nodejs:

   ```bash
   	brew install node
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

[Node](/docs-sphinx/getting-started/development-environment/node/node.md)
