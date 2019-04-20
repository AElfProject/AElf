# Setup

In order to develop your smart contract and dAPP to interact with it, AElf provides a framework that facilitates the process: AElf Boilerplate.

## Clone the repo

```bash
git clone https://github.com/AElfProject/aelf-boilerplate
```

### Build and run

In this tutorial we'll use visual studio code to illustrate the steps but of course, you can use another IDE.

First open the folder you have just cloned. If asked to add dependencies and restore, say yes to both.

<p align="center">
  <img src="dep-yes.png" width="300">
</p>

Open vscodes terminal and build the project with the following command:

```bash
cd boilerplate/chain/src/AElf.Boilerplate.Launcher/
dotnet build
```

then run:

```bash
dotnet run bin/Debug/netcoreapp2.2/AElf.Boilerplate.Launcher
```

<p align="center">
  <img src="term.png" width="400">
</p>

To actually run the node, use the follwing command.

```bash
dotnet run bin/Debug/netcoreapp2.2/AElf.Boilerplate.Launcher
```

At this point the smart contract has been deployed and is ready to use.

If you want to run the tests, simply navigate to the HelloWorldContract.Test folder. From here run:

```bash
dotnet test
```
The output should look somewhat like this:
```bash 
Total tests: 1. Passed: 1. Failed: 0. Skipped: 0.
```

## Run the JS SDK Demo

The following commands will demonstrate the capabilities of the js sdk, execute them in order:

```bash
cd Web/JSSDK
npm install
npm start
```

You should see the results in the terminal or in the browser dev tool.

## Run the browser extension Demo

To use the browser extension you must follow the following instructions:

[extension repo](https://github.com/hzz780/aelf-web-extension)

Next go into the extensions folder and run the app with the following commands:

```bash
cd Web/browserExtension
npm install
npm start
```

To see the plugin in action you can navigate to the following address in your browser: [http://localhost:3000](http://localhost:3000)