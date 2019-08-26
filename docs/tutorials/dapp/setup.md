# Setup

This article will get you started with AElf's Boilerplate project. You will learn the following items:
- setting up the basic environment for developing contracts and dApps.
- downloading **AElf Boilerplate**'s code and setting up Visual Studio Code.
- running a template contract and basic interactions from a dApp frontend with the Javascript SDK demo and the browser extension.

## Environment

#### Minimum requirement

To successfully complete this tutorial you first need to install the following dependencies, please refer to your platforms installation guide:
- Git for version control, follow [these instructions](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git).
- Dotnet core sdk for building and testing, you can [download it here](https://dotnet.microsoft.com/download). 

Mac users also need to [install Homebrew](https://brew.sh/).

#### IDE

Strictly speaking you don't need an IDE for this tutorial but it is highly recommended. If you don't already have one we recommend you try Visual Studio Code (vscode) with the C# extension:
- installation instructions [**here**](https://code.visualstudio.com/docs/setup/setup-overview).
- working with C# [**here**](https://code.visualstudio.com/docs/languages/csharp).

#### Clone the repository

Open a terminal in your system and clone the tutorial with the following command:

```bash
git clone https://github.com/AElfProject/aelf-boilerplate
```

This command will create a **aelf-boilerplate** folder with the code inside it.

## Run the contract

#### Open the project

If not already done open vscode and open the **aelf-boilerplate** folder.

If asked to add some "required assets" say **yes**. There may also be some dependencies to restore: for all of them choose **Restore**.

<p align="center">
  <img src="vscode-dep.png" width="200">
</p>

Open vscode's **Integrated Terminal** and build the project with the following command. Note: you can find out more about vscode's terminal [**here**](https://code.visualstudio.com/docs/editor/integrated-terminal).

#### Install script

If you don't already have protobuf installed, run the following script:

- On Mac or Linux: 
```bash
sh chain/scripts/install.sh
```

- On Windows:  
Windows is slightly more complex. You need to open a PowerShell console as administrator. Enter your clone directory so your PowerShell is currently in the root of aelf-boilerplate clone. Run the following:
```bash
chain/scripts/install_choco.ps1
```

Note: if you prefer or have problems, you can refer to the following guide to [**manually install**](https://github.com/protocolbuffers/protobuf/blob/master/src/README.md) protobuf on your system.

#### Build and run

```bash
cd chain/src/AElf.Boilerplate.Launcher/
dotnet build
```

<p align="center">
  <img src="term.png" width="400">
</p>

To actually run the node, use the following command.

```bash
dotnet run bin/Debug/netcoreapp2.2/AElf.Boilerplate.Launcher
```

At this point the smart contract has been deployed and is ready to use. You should see the node's logs. You can now stop the node by killing the process (usually **control-c** or **ctrl-c** in the terminal).

If you want to run the tests, simply navigate to the HelloWorldContract.Test folder. From here run:

```bash
cd ../../test/HelloWorldContract.Test/
dotnet test
```
The output should look somewhat like this:
```bash 
Total tests: 1. Passed: 1. Failed: 0. Skipped: 0.
```

## Next

You've just seen a simple example of a smart contract run with our Boilerplate tutorial. When launching (with dotnet run) the contract was automatically deployed and ready to interact with. You also discovered how to navigate to the test folder and run the tests.

Next you will see how AElf's js sdk is used to interact with the contract.

## Run the JS SDK Demo

To run this demo you'll need to install [Nodejs](https://nodejs.org/) first. ([Nodjs image in China](http://nodejs.cn/download/))

The following commands will navigate to the SDK's folder and demonstrate the capabilities of the js sdk, execute them in order:

```bash
cd ../../../web/JSSDK/
npm install
npm start
```

You should see the results in the terminal or in the browser dev tool.

### In the terminal

<p align="center">
  <img src="assets/jssdk_terminal.jpg" width="400">
</p>

### In the browser dev tool

<p align="center">
  <img src="assets/jssdk_browser.jpg" width="400">
</p>

## Run the browser extension Demo

To use the browser extension you must follow the following instructions:

1.Install the extension.

click to install [dev version](https://chrome.google.com/webstore/detail/aelf-explorer-extension-d/mlmlhipeonlflbcclinpbmcjdnpnmkpf)

[extension repo](https://github.com/hzz780/aelf-web-extension)

If you meet some problem when use the extension, please click the [extension repo](https://github.com/hzz780/aelf-web-extension/blob/master/aelf-web-extension-0.7.0.crx) and download the 0.7.0 version extension from the github.
And add the extension manually.

2.Open the extension, create your wallet, create your keypair.

3.Next go into the extensions folder and run the app with the following commands:

```bash
cd ../browserExtension/
npm install
npm start
```

To see the plugin in action you can navigate to the following address in your browser: [http://localhost:3000](http://localhost:3000)

Please open dev tool to see the results.

<p align="center">
  <img src="assets/extension.jpg" width="400">
</p>
