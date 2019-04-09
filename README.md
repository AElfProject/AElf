# AElf - A Decentralized Cloud Computing Blockchain Network 

[![Build Status](https://travis-ci.org/AElfProject/AElf.svg?branch=dev)](https://travis-ci.org/AElfProject/AElf)
[![Build status](https://ci.appveyor.com/api/projects/status/wnehtmk2up4l5w5j/branch/dev?svg=true)](https://ci.appveyor.com/project/AElfProject/aelf/branch/dev)
[![GitHub closed issues](https://img.shields.io/github/issues-closed/aelfproject/aelf.svg)](https://app.gitkraken.com/glo/board/XKsOZJarBgAPseno)
[![codecov](https://codecov.io/gh/AElfProject/AElf/branch/dev/graph/badge.svg)](https://codecov.io/gh/AElfProject/AElf)
[![lisence](https://img.shields.io/github/license/AElfProject/AElf.svg)](https://github.com/AElfProject/AElf/blob/dev/LICENSE)
[![Nuget](https://img.shields.io/nuget/v/AElf.OS.svg)](https://www.nuget.org/packages?q=aelf)
[![MyGet (with prereleases)](https://img.shields.io/myget/aelf-project-dev/vpre/aelf.os.svg?label=myget)](https://www.myget.org/gallery/aelf-project-dev)

[![Twitter Follow](https://img.shields.io/twitter/follow/aelfblockchain.svg?label=%40aelfblockchain&style=social)](https://twitter.com/aelfblockchain)
[![Gitter](https://badges.gitter.im/aelfproject/community.svg)](https://gitter.im/aelfproject/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

Welcome to AElfs’ official GitHub repository. The code is still in its early stages and is under constant change to 
improve its quality and functionality.

You can find out more about AElf by reading the 
[White Paper](https://grid.hoopox.com/aelf_whitepaper_EN.pdf?v=1). 

Official website: [aelf.io](https://aelf.io)

## Overview

AElfs main objective is to permit scalability and extensibility through a multi-layer branching structure formed by a 
main chain and multiple levels of side-chains (a tree like structure). Each side-chain will be designed for one business 
use case. We also plan to include communication with external blockchains like Bitcoin or Ethereum.

AElf also aims to make it as easy as possible to extend/customize the system by providing easy to use tools and 
frameworks in order to customize the chains and write smart contracts. AElf will support various languages that will let 
developers choose the one they are the most comfortable with.

AElf will improve overall blockchain performance by executing transactions in parallel and isolating smart contracts in 
their own side-chains in order to segregate the systems resources.

## Development

In these early stages, we want to concentrate on developing the kernel of the system. This corresponds to the most basic 
building block of the system. Notably, structures like chain and storage will be implemented in it. The next step will 
be to develop the networking and consensus layer used to create the network. The final step will be to work on AElfs 
gouvernance system.

If you want to run the code you can clone the repository and open the project with any IDE that support C# and the 
.NET core runtime (we would recommend either [Visual Studio](https://www.visualstudio.com/) on Windows or 
[Rider](https://www.jetbrains.com/rider/) if you’re on a Mac). You will also need to have the 
[.NET Core SDK](https://www.microsoft.com/net/learn/get-started/macos) installed.

For now the solution includes the unit tests, you can run them and study them to get an idea as to how different parts 
of the current system work and how they’re used.

You will find some more in-depth technical documentation [here](/docs/README.md).

## How to Contribute

If you have a reasonable understanding of blockchain technology and at least some notions of C# you can of course 
contribute by using GitHub issues and Pull Requests. We also appreciate other types of contributions such as 
documentation improvements or even correcting typos in the code if you spot any.

The standard procedure is well documented on GitHub, for detailed explanation, especially if it’s the first time you’re 
doing this, you can follow the procedure on the following links:
[Working with forks](https://help.github.com/articles/working-with-forks/) and 
[Pull Requests](https://help.github.com/articles/proposing-changes-to-your-work-with-pull-requests/).
Basically, you fork the AElf repository, create a branch that clearly indicates the problem you’re solving. Later, when 
you are happy with your work, you create a Pull Request so we can review and discuss your implementation.

If the problem needs debating or you have questions on how to implement a feature, we would prefer you open a GitHub 
[issue](https://github.com/AElfProject/AElf/issues). If you spotted a typo or a code formatting issue, just directly 
opening a Pull Request is fine. 

## Supported Platforms

Any platform that supports .NET Core is compatible.
